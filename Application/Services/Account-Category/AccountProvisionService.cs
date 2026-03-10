using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

public class AccountProvisionService : IAccountProvisionService
{

    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public AccountProvisionService(
        IdentityDbContext db,
        ICurrentUserService currentUser,
        IEmailService emailService,
        IConfiguration config)
    {
        _db = db;
        _currentUser = currentUser;
        _emailService = emailService;
        _config = config;
    }
    public async Task<int> CreateAsync(CreateAccountRequest req)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            // ✅ validate tax type belongs to country
            var taxTypeId = req.TaxTypeId;

            var validTax = await _db.TaxTypes.AnyAsync(x =>
                x.TaxTypeId == req.TaxTypeId &&
                x.CountryId == req.CountryId &&
                x.IsActive);

            if (!validTax)
            {
                // fallback to default tax type
                taxTypeId = 1;
            }

            // ✅ account code
            var accountCode = string.IsNullOrWhiteSpace(req.AccountCode)
                ? $"ACC-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : req.AccountCode.Trim();

            var codeExists = await _db.Accounts
                .AnyAsync(x => x.AccountCode == accountCode && !x.IsDeleted);

            if (codeExists)
                throw new InvalidOperationException("AccountCode already exists");


            // =====================================================
            // CREATE ACCOUNT FIRST (WITHOUT HIERARCHY)
            // =====================================================

            var account = new mst_account
            {
                AccountCode = accountCode,
                AccountName = req.AccountName.Trim(),
                CategoryId = req.CategoryId,
                PrimaryDomain = req.PrimaryDomain.Trim(),

                CountryId = req.CountryId,
                StateId = req.StateId,
                CityId = req.CityId,
                Zipcode = req.Zipcode,

                ParentAccountId = req.ParentAccountId,
                RefferCode = req.RefferCode,

                TaxTypeId = req.TaxTypeId,

                fullname = req.fullname,
                email = req.email,
                phone = req.phone,
                Position = req.Position,

                address = req.address,

                BusinessPhone = req.BusinessPhone,
                BusinessEmail = req.BusinessEmail,
                BusinessAddress = req.BusinessAddress,
                BusinessHours = req.BusinessHours,
                BusinessTimeZone = req.BusinessTimeZone,

                UserName = req.UserName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),

                share = req.share,

                Fk_userid = req.userId,
                Status = req.Status,

                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                IsDeleted = false,
                CreatedBy = req.userId
            };

            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();   // ⭐ get AccountId


            // =====================================================
            // GENERATE HIERARCHY PATH
            // =====================================================

            string hierarchyPath;

            if (account.ParentAccountId.HasValue)
            {
                var parent = await _db.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.AccountId == account.ParentAccountId.Value &&
                        !x.IsDeleted);

                if (parent == null)
                    throw new Exception("Parent account not found");
                if (string.IsNullOrWhiteSpace(parent.HierarchyPath))
                    throw new Exception("Parent account hierarchy is invalid");

                hierarchyPath = $"{parent.HierarchyPath}{account.AccountId}/";
            }
            else
            {
                // Root account
                hierarchyPath = $"/{account.AccountId}/";
            }

            account.HierarchyPath = hierarchyPath;

            await _db.SaveChangesAsync();

            // =====================================================
            // CREATE DEFAULT ROLE FOR FIRST USER
            // System creator  -> SuperAdmin
            // Non-system      -> category-based default role
            // =====================================================
            var firstRoleName = "SuperAdmin";

            if (!_currentUser.IsSystem)
            {
                var categoryName = await _db.Categories
                    .Where(x => x.CategoryId == req.CategoryId && !x.IsDeleted)
                    .Select(x => x.LabelName)
                    .FirstOrDefaultAsync();

                firstRoleName = ResolveDefaultRoleByCategory(categoryName);
                if (string.IsNullOrWhiteSpace(firstRoleName))
                    throw new BadHttpRequestException("No default role mapping found for selected category");
            }

            var firstRole = await _db.Roles
                .FirstOrDefaultAsync(x =>
                    x.AccountId == account.AccountId &&
                    x.RoleName == firstRoleName &&
                    !x.IsDeleted);

            if (firstRole == null)
            {
                firstRole = new mst_role
                {
                    AccountId = account.AccountId,
                    RoleName = firstRoleName,
                    RoleCode = firstRoleName.ToUpperInvariant(),
                    IsSystemRole = false,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    CreatedBy = req.userId
                };

                _db.Roles.Add(firstRole);
                await _db.SaveChangesAsync();
            }

            // =====================================================
            // CREATE FIRST USER
            // =====================================================

            var email = req.email?.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var userExists = await _db.Users
                    .AnyAsync(x => x.Email == email && !x.IsDeleted);

                if (userExists)
                    throw new InvalidOperationException("User email already exists");
            }


            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = email ?? "",
                FirstName = req.fullname,
                LastName = "",

                Password_hash = BCrypt.Net.BCrypt.HashPassword(req.Password),

                AccountId = account.AccountId,
                roleId = firstRole.RoleId,

                MobileNo = req.phone,
                Status = true,

                TwoFactorEnabled = false,
                EmailVerified = false,
                MobileVerified = false,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = req.userId,
                IsDeleted = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            // =====================================================
            // SEND ONBOARDING EMAIL (post-commit)
            // =====================================================
            var token = Guid.NewGuid().ToString("N");
            user.PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword(token);
            user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(30);
            await _db.SaveChangesAsync();

            var resetBaseUrl = _config["Frontend:ResetPasswordUrl"];
            if (!string.IsNullOrWhiteSpace(resetBaseUrl))
            {
                var resetLink = $"{resetBaseUrl}?token={token}&email={user.Email}";

                var template = await LoadEmailTemplateAsync("fleetbharat-account-onboarding.html");
                var body = template
                    .Replace("{{ACCOUNT_NAME}}", account.AccountName ?? "")
                    .Replace("{{USER_NAME}}", account.UserName ?? "")
                    .Replace("{{RESET_LINK}}", resetLink);

                var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(req.email))
                    recipients.Add(req.email.Trim());

                if (!string.IsNullOrWhiteSpace(req.BusinessEmail))
                    recipients.Add(req.BusinessEmail.Trim());

                foreach (var recipient in recipients)
                {
                    await _emailService.SendAsync(
                        recipient,
                        "Welcome to Fleetbharat - Your Account is Ready",
                        body
                    );
                }
            }

            return account.AccountId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
    public async Task<AccountListWithCardDto> GetAllAsync(
     int page,
     int pageSize,
     string? search,
     bool? status)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        // -----------------------------
        // Base query only for cards
        // -----------------------------
        var baseAccountQuery = _db.Accounts
            .Where(x => !x.IsDeleted);

        var cardCounts = await baseAccountQuery
            .GroupBy(x => 1)
            .Select(g => new AccountCardCountDto
            {
                Total = g.Count(),
                Active = g.Count(x => x.Status == true),
                Inactive = g.Count(x => x.Status == false),
                Pending = g.Count(x => x.Status == null)
            })
            .FirstOrDefaultAsync()
            ?? new AccountCardCountDto();

        // -----------------------------
        // Main grid query
        // -----------------------------
        var query =
            from a in _db.Accounts
            join c in _db.Countries on a.CountryId equals c.CountryId

            join st0 in _db.States
                on a.StateId equals st0.StateId.ToString() into stGroup
            from st in stGroup.DefaultIfEmpty()

            join ct0 in _db.Cities
                on a.CityId equals ct0.CityId.ToString() into ctGroup
            from ct in ctGroup.DefaultIfEmpty()

            join cat in _db.Categories on a.CategoryId equals cat.CategoryId

            where !a.IsDeleted
            select new { a, c, cat, st, ct };

        // -----------------------------
        // Search filter
        // -----------------------------
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            query = query.Where(x =>
                (x.a.AccountName ?? "").ToLower().Contains(s) ||
                (x.a.AccountCode ?? "").ToLower().Contains(s) ||
                (x.a.PrimaryDomain ?? "").ToLower().Contains(s) ||
                ((x.st != null ? x.st.StateName : "")).ToLower().Contains(s) ||
                ((x.ct != null ? x.ct.CityName : "")).ToLower().Contains(s));
        }

        // -----------------------------
        // Status filter only for grid
        // -----------------------------
        if (status.HasValue)
        {
            query = query.Where(x => x.a.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.a.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AccountResponseDto
            {
                AccountId = x.a.AccountId,
                AccountCode = x.a.AccountCode,
                AccountName = x.a.AccountName,
                Reffer = x.a.RefferCode,

                ParentAccountId = x.a.ParentAccountId,
                HierarchyPath = x.a.HierarchyPath,

                Fk_userid = x.a.Fk_userid,

                CategoryId = x.cat.CategoryId,
                CategoryName = x.cat.LabelName,

                PrimaryDomain = x.a.PrimaryDomain,

                CountryId = x.c.CountryId,
                CountryName = x.c.CountryName,

                StateId = x.a.StateId,
                StateName = x.st != null ? x.st.StateName : "",

                CityId = x.a.CityId,
                CityName = x.ct != null ? x.ct.CityName : "",

                fullname = x.a.fullname,
                email = x.a.email,
                phone = x.a.phone,
                address = x.a.address,
                Position = x.a.Position,
                BusinessAddress = x.a.BusinessAddress,
                BusinessEmail = x.a.BusinessEmail,
                BusinessHours = x.a.BusinessHours,
                BusinessPhone = x.a.BusinessPhone,
                BusinessTimeZone = x.a.BusinessTimeZone,
                Zipcode = x.a.Zipcode,
                usernamesacc = x.a.UserName,
                password = x.a.PasswordHash,
                share = x.a.share,


                TaxTypeId = x.a.TaxTypeId,
                Status = x.a.Status,

                CreatedOn = x.a.CreatedOn
            })
            .AsNoTracking()
            .ToListAsync();

        return new AccountListWithCardDto
        {
            PageData = new PagedResultDto<AccountResponseDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalCount,
                Items = items
            },
            CardCounts = cardCounts
        };
    }

    public async Task<AccountResponseDto?> GetByIdAsync(int accountId)
    {
        var query =
            from a in _db.Accounts
            join c in _db.Countries on a.CountryId equals c.CountryId

            // ✅ LEFT JOIN mst_state (string -> int match)
            join st in _db.States
                on a.StateId equals st.StateId.ToString() into stj
            from st in stj.DefaultIfEmpty()

                // ✅ LEFT JOIN mst_city (string -> int match)
            join ct in _db.Cities
                on a.CityId equals ct.CityId.ToString() into ctj
            from ct in ctj.DefaultIfEmpty()

            join cat in _db.Categories on a.CategoryId equals cat.CategoryId

            where !a.IsDeleted && a.AccountId == accountId
            select new { a, c, cat, st, ct };

        return await query
            .Select(x => new AccountResponseDto
            {
                AccountId = x.a.AccountId,
                AccountCode = x.a.AccountCode,
                AccountName = x.a.AccountName,
                Reffer = x.a.RefferCode,

                ParentAccountId = x.a.ParentAccountId,
                HierarchyPath = x.a.HierarchyPath,

                Fk_userid = x.a.Fk_userid,

                CategoryId = x.cat.CategoryId,
                CategoryName = x.cat.LabelName,

                PrimaryDomain = x.a.PrimaryDomain,

                CountryId = x.c.CountryId,
                CountryName = x.c.CountryName,

                // ✅ state
                StateId = x.a.StateId,
                StateName = x.st != null ? x.st.StateName : "",

                // ✅ city
                CityId = x.a.CityId,
                CityName = x.ct != null ? x.ct.CityName : "",

                fullname = x.a.fullname,
                email = x.a.email,
                phone = x.a.phone,
                address = x.a.address,
                Position = x.a.Position,
                BusinessAddress = x.a.BusinessAddress,
                BusinessEmail = x.a.BusinessEmail,
                BusinessHours = x.a.BusinessHours,
                BusinessPhone = x.a.BusinessPhone,
                BusinessTimeZone = x.a.BusinessTimeZone,
                Zipcode = x.a.Zipcode,
                usernamesacc = x.a.UserName,
                password = x.a.PasswordHash,
                share = x.a.share,

                TaxTypeId = x.a.TaxTypeId,
                Status = x.a.Status,

                CreatedOn = x.a.CreatedOn
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<List<AccountHierarchyDto>> GetHierarchyAsync()
    {
        var accounts = await (
            from a in _db.Accounts.AsNoTracking()
            join cat in _db.Categories.AsNoTracking()
                on a.CategoryId equals cat.CategoryId into catGroup
            from cat in catGroup.DefaultIfEmpty()
            where !a.IsDeleted
            orderby a.HierarchyPath
            select new
            {
                a.AccountId,
                a.AccountName,
                a.AccountCode,
                a.Status,
                a.ParentAccountId,
                CategoryName = cat != null ? cat.LabelName : string.Empty
            })
            .ToListAsync();

        var nodeMap = accounts.ToDictionary(
            x => x.AccountId,
            x => new AccountHierarchyDto
            {
                AccountId = x.AccountId,
                AccountName = x.AccountName ?? string.Empty,
                AccountCode = x.AccountCode ?? string.Empty,
                CategoryName = x.CategoryName ?? string.Empty,
                Status = x.Status
            });

        var roots = new List<AccountHierarchyDto>();

        foreach (var account in accounts)
        {
            var node = nodeMap[account.AccountId];

            if (account.ParentAccountId.HasValue &&
                nodeMap.TryGetValue(account.ParentAccountId.Value, out var parentNode))
            {
                parentNode.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots;
    }

    public async Task<bool> UpdateAsync(int accountId, UpdateAccountRequest req)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted);

            if (account == null)
                return false;


            var validTax = await _db.TaxTypes.AnyAsync(x =>
                x.TaxTypeId == req.TaxTypeId &&
                x.CountryId == req.CountryId &&
                x.IsActive);

            if (!validTax)
                throw new BadHttpRequestException("Invalid TaxType for selected Country");


            // Account Code
            if (!string.IsNullOrWhiteSpace(req.AccountCode))
            {
                var code = req.AccountCode.Trim();

                var codeExists = await _db.Accounts.AnyAsync(x =>
                    x.AccountCode == code &&
                    x.AccountId != accountId &&
                    !x.IsDeleted);

                if (codeExists)
                    throw new InvalidOperationException("AccountCode already exists");

                account.AccountCode = code;
            }


            // ========================
            // MAIN FIELDS
            // ========================

            account.AccountName = req.AccountName.Trim();
            account.CategoryId = req.CategoryId;
            account.PrimaryDomain = req.PrimaryDomain.Trim();

            account.CountryId = req.CountryId;
            account.StateId = req.StateId;
            account.CityId = req.CityId;
            account.Zipcode = req.Zipcode;

            account.RefferCode = req.RefferCode;
            account.TaxTypeId = req.TaxTypeId;


            // ========================
            // CONTACT
            // ========================

            account.fullname = req.fullname;
            account.email = req.email;
            account.phone = req.phone;
            account.Position = req.Position;
            account.address = req.address;


            // ========================
            // BUSINESS
            // ========================

            account.BusinessPhone = req.BusinessPhone;
            account.BusinessEmail = req.BusinessEmail;
            account.BusinessAddress = req.BusinessAddress;
            account.BusinessHours = req.BusinessHours;
            account.BusinessTimeZone = req.BusinessTimeZone;


            // ========================
            // USER ACCESS
            // ========================

            account.UserName = req.UserName;
            account.share = req.share;

            if (!string.IsNullOrWhiteSpace(req.Password))
                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);


            // ========================
            // PARENT CHANGE → UPDATE HIERARCHY
            // ========================

            if (req.ParentAccountId == account.AccountId)
                throw new Exception("Account cannot be its own parent");

            if (account.ParentAccountId != req.ParentAccountId)
            {
                var oldPath = account.HierarchyPath;
                if (string.IsNullOrWhiteSpace(oldPath))
                    oldPath = $"/{account.AccountId}/";

                string newPath;

                if (req.ParentAccountId.HasValue)
                {
                    var parent = await _db.Accounts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.AccountId == req.ParentAccountId.Value &&
                            !x.IsDeleted);

                    if (parent == null)
                        throw new Exception("Parent account not found");
                    if (string.IsNullOrWhiteSpace(parent.HierarchyPath))
                        throw new Exception("Parent account hierarchy is invalid");

                    // Prevent assigning under own subtree.
                    if (parent.HierarchyPath.StartsWith(oldPath))
                        throw new Exception("Circular hierarchy is not allowed");

                    newPath = $"{parent.HierarchyPath}{account.AccountId}/";
                }
                else
                {
                    newPath = $"/{account.AccountId}/";
                }

                var descendants = await _db.Accounts
                    .Where(x => !x.IsDeleted && x.HierarchyPath.StartsWith(oldPath))
                    .ToListAsync();

                foreach (var child in descendants)
                {
                    child.HierarchyPath = newPath + child.HierarchyPath.Substring(oldPath.Length);
                    child.UpdatedOn = DateTime.UtcNow;
                    child.UpdatedBy = req.userId;
                }

                account.ParentAccountId = req.ParentAccountId;
            }


            account.Status = req.Status;
            account.UpdatedOn = DateTime.UtcNow;
            account.UpdatedBy = req.userId;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(int accountId, bool status)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (account == null) return false;

        account.Status = status;
        account.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int accountId)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (account == null) return false;

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();
        return true;
    }

    private static string ResolveDefaultRoleByCategory(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return string.Empty;

        var normalized = categoryName.Trim().ToLowerInvariant();

        if (normalized.Contains("distributor"))
            return "DistributorAdmin";

        if (normalized.Contains("reseller"))
            return "ResellerAdmin";

        if (normalized.Contains("dealer"))
            return "DealerAdmin";

        return string.Empty;
    }

    private static async Task<string> LoadEmailTemplateAsync(string templateName)
    {
        var relativePath = Path.Combine("docs", "email-templates", templateName);
        var contentRootPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        if (File.Exists(contentRootPath))
            return await File.ReadAllTextAsync(contentRootPath);

        var baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (File.Exists(baseDirectoryPath))
            return await File.ReadAllTextAsync(baseDirectoryPath);

        throw new FileNotFoundException($"Email template not found: {relativePath}");
    }
}
