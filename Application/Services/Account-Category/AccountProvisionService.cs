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
            // FIX: normalize email


            if (codeExists)
                throw new InvalidOperationException("AccountCode already exists");

            await ValidateUniqueAccountUserFieldsAsync(
                businessEmail: req.BusinessEmail,
                phone: req.phone,
                userName: req.UserName);


            // =====================================================
            // CREATE ACCOUNT FIRST (WITHOUT HIERARCHY)
            // =====================================================
            var defaultpass = "123456";
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
                email = req.email?.Trim().ToLowerInvariant(),
                phone = req.phone,
                Position = req.Position,

                address = req.address,

                BusinessPhone = req.BusinessPhone,
                BusinessEmail = req.BusinessEmail,
                BusinessAddress = req.BusinessAddress,
                BusinessHours = req.BusinessHours,
                BusinessTimeZone = req.BusinessTimeZone,

                UserName = req.UserName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultpass),

                share = req.share,

                Fk_userid = req.userId,
                Status = req.Status,

                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                IsDeleted = false,
                CreatedBy = _currentUser.AccountId
            };

            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();   // ⭐ get AccountId


            // =====================================================
            // GENERATE HIERARCHY PATH
            // =====================================================

            string hierarchyPath;

            // if (account.ParentAccountId.HasValue)
            // {
            //     var parent = await _db.Accounts
            //         .AsNoTracking()
            //         .FirstOrDefaultAsync(x =>
            //             x.AccountId == account.ParentAccountId.Value &&
            //             !x.IsDeleted);

            //     if (parent == null)
            //         throw new Exception("Parent account not found");
            //     if (string.IsNullOrWhiteSpace(parent.HierarchyPath))
            //         throw new Exception("Parent account hierarchy is invalid");

            //     hierarchyPath = $"{parent.HierarchyPath}{account.AccountId}/";
            // }
            // FIX: parent must be inside hierarchy
            if (account.ParentAccountId.HasValue)
            {
                var parent = await _db.Accounts
                    .ApplyAccountHierarchyFilter(_currentUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.AccountId == account.ParentAccountId.Value &&
                        !x.IsDeleted);

                if (parent == null)
                    throw new Exception("Invalid parent account access");

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
                    CreatedBy = _currentUser.AccountId,
                };

                _db.Roles.Add(firstRole);
                await _db.SaveChangesAsync();
            }

            // =====================================================
            // CREATE FIRST USER
            // =====================================================

            // FIX: normalize email
            var email = req.email?.Trim().ToLowerInvariant();

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
                User_name = req.UserName,
                Password_hash = BCrypt.Net.BCrypt.HashPassword(req.Password),

                AccountId = account.AccountId,
                roleId = firstRole.RoleId,

                MobileNo = req.phone,
                Status = true,

                TwoFactorEnabled = false,
                EmailVerified = false,
                MobileVerified = false,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.AccountId,

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

                var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(req.email))
                    recipients.Add(req.email.Trim());

                if (!string.IsNullOrWhiteSpace(req.BusinessEmail))
                    recipients.Add(req.BusinessEmail.Trim());

                foreach (var recipient in recipients)
                {
                    await SendBrandedEmailAsync(
                        accountId: account.AccountId,
                        toEmail: recipient,
                        templateName: "account-onboarding.html",
                        subjectFactory: brandName => $"Welcome to {brandName} - Your Account is Ready",
                        placeholders: new Dictionary<string, string>
                        {
                            ["{{ACCOUNT_NAME}}"] = account.AccountName ?? string.Empty,
                            ["{{USER_NAME}}"] = account.UserName ?? string.Empty,
                            ["{{RESET_LINK}}"] = resetLink,
                            ["{{CODE}}"] = string.Empty
                        });
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
    // public async Task<AccountListWithCardDto> GetAllAsync(
    //  int page,
    //  int pageSize,
    //  string? search,
    //  bool? status)
    // {
    //     if (page <= 0) page = 1;
    //     if (pageSize <= 0) pageSize = 10;

    //     // -----------------------------
    //     // Base query only for cards
    //     // -----------------------------
    //     // var baseAccountQuery = _db.Accounts
    //     //     .Where(x => !x.IsDeleted);
    //     var baseAccountQuery = _db.Accounts
    // .ApplyAccountHierarchyFilter(_currentUser)
    // .Where(x => !x.IsDeleted);

    //     var cardCounts = await baseAccountQuery
    //         .GroupBy(x => 1)
    //         .Select(g => new AccountCardCountDto
    //         {
    //             Total = g.Count(),
    //             Active = g.Count(x => x.Status == true),
    //             Inactive = g.Count(x => x.Status == false),
    //             Pending = g.Count(x => x.Status == null)
    //         })
    //         .FirstOrDefaultAsync()
    //         ?? new AccountCardCountDto();

    //     // -----------------------------
    //     // Main grid query
    //     // -----------------------------
    //     var query =
    //         //from a in _db.Accounts
    //         from a in _db.Accounts.ApplyAccountHierarchyFilter(_currentUser)
    //         join c in _db.Countries on a.CountryId equals c.CountryId

    //         join st0 in _db.States
    //             on a.StateId equals st0.StateId.ToString() into stGroup
    //         from st in stGroup.DefaultIfEmpty()

    //         join ct0 in _db.Cities
    //             on a.CityId equals ct0.CityId.ToString() into ctGroup
    //         from ct in ctGroup.DefaultIfEmpty()

    //         join cat in _db.Categories on a.CategoryId equals cat.CategoryId

    //         where !a.IsDeleted
    //         select new { a, c, cat, st, ct };

    //     // -----------------------------
    //     // Search filter
    //     // -----------------------------
    //     if (!string.IsNullOrWhiteSpace(search))
    //     {
    //         var s = search.Trim().ToLower();

    //         query = query.Where(x =>
    //             (x.a.AccountName ?? "").ToLower().Contains(s) ||
    //             (x.a.AccountCode ?? "").ToLower().Contains(s) ||
    //             (x.a.PrimaryDomain ?? "").ToLower().Contains(s) ||
    //             ((x.st != null ? x.st.StateName : "")).ToLower().Contains(s) ||
    //             ((x.ct != null ? x.ct.CityName : "")).ToLower().Contains(s));
    //     }

    //     // -----------------------------
    //     // Status filter only for grid
    //     // -----------------------------
    //     if (status.HasValue)
    //     {
    //         query = query.Where(x => x.a.Status == status.Value);
    //     }

    //     var totalCount = await query.CountAsync();

    //     var items = await query
    //         .OrderByDescending(x => x.a.CreatedOn)
    //         .Skip((page - 1) * pageSize)
    //         .Take(pageSize)
    //         .Select(x => new AccountResponseDto
    //         {
    //             AccountId = x.a.AccountId,
    //             AccountCode = x.a.AccountCode,
    //             AccountName = x.a.AccountName,
    //             Reffer = x.a.RefferCode,

    //             ParentAccountId = x.a.ParentAccountId,
    //             HierarchyPath = x.a.HierarchyPath,

    //             Fk_userid = x.a.Fk_userid,

    //             CategoryId = x.cat.CategoryId,
    //             CategoryName = x.cat.LabelName,

    //             PrimaryDomain = x.a.PrimaryDomain,

    //             CountryId = x.c.CountryId,
    //             CountryName = x.c.CountryName,

    //             StateId = x.a.StateId,
    //             StateName = x.st != null ? x.st.StateName : "",

    //             CityId = x.a.CityId,
    //             CityName = x.ct != null ? x.ct.CityName : "",

    //             fullname = x.a.fullname,
    //             email = x.a.email,
    //             phone = x.a.phone,
    //             address = x.a.address,
    //             Position = x.a.Position,
    //             BusinessAddress = x.a.BusinessAddress,
    //             BusinessEmail = x.a.BusinessEmail,
    //             BusinessHours = x.a.BusinessHours,
    //             BusinessPhone = x.a.BusinessPhone,
    //             BusinessTimeZone = x.a.BusinessTimeZone,
    //             Zipcode = x.a.Zipcode,
    //             usernamesacc = x.a.UserName,
    //             //password = x.a.PasswordHash,
    //             share = x.a.share,


    //             TaxTypeId = x.a.TaxTypeId,
    //             Status = x.a.Status,

    //             CreatedOn = x.a.CreatedOn
    //         })
    //         .AsNoTracking()
    //         .ToListAsync();

    //     return new AccountListWithCardDto
    //     {
    //         PageData = new PagedResultDto<AccountResponseDto>
    //         {
    //             Page = page,
    //             PageSize = pageSize,
    //             TotalRecords = totalCount,
    //             Items = items
    //         },
    //         CardCounts = cardCounts
    //     };
    // }
    public async Task<AccountListWithCardDto> GetAllAsync(
    int page,
    int pageSize,
    string? search,
    bool? status)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        // System account can see all accounts
        var accountBaseQuery = _db.Accounts.AsQueryable();

        if (_currentUser.AccountId != 1)
        {
            accountBaseQuery = accountBaseQuery.ApplyAccountHierarchyFilter(_currentUser);
        }

        accountBaseQuery = accountBaseQuery.Where(x => !x.IsDeleted);

        var cardCounts = await accountBaseQuery
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

        var query =
            from a in accountBaseQuery

            join c0 in _db.Countries
                on a.CountryId equals c0.CountryId into countryGroup
            from c in countryGroup.DefaultIfEmpty()

            join st0 in _db.States
                on a.StateId equals st0.StateId.ToString() into stGroup
            from st in stGroup.DefaultIfEmpty()

            join ct0 in _db.Cities
                on a.CityId equals ct0.CityId.ToString() into ctGroup
            from ct in ctGroup.DefaultIfEmpty()

            join cat0 in _db.Categories
                on a.CategoryId equals cat0.CategoryId into catGroup
            from cat in catGroup.DefaultIfEmpty()

            select new { a, c, cat, st, ct };

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

                CategoryId = x.cat != null ? x.cat.CategoryId : 0,
                CategoryName = x.cat != null ? x.cat.LabelName : "",

                PrimaryDomain = x.a.PrimaryDomain,

                CountryId = x.c != null ? x.c.CountryId : 0,
                CountryName = x.c != null ? x.c.CountryName : "",

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

    // public async Task<AccountResponseDto?> GetByIdAsync(int accountId)
    // {
    //     var query =
    //         from a in _db.Accounts
    //         join c in _db.Countries on a.CountryId equals c.CountryId

    //         // ✅ LEFT JOIN mst_state (string -> int match)
    //         join st in _db.States
    //             on a.StateId equals st.StateId.ToString() into stj
    //         from st in stj.DefaultIfEmpty()

    //             // ✅ LEFT JOIN mst_city (string -> int match)
    //         join ct in _db.Cities
    //             on a.CityId equals ct.CityId.ToString() into ctj
    //         from ct in ctj.DefaultIfEmpty()

    //         join cat0 in _db.Categories
    //     on a.CategoryId equals cat0.CategoryId into catGroup
    //         from cat in catGroup.DefaultIfEmpty()

    //             // where !a.IsDeleted && a.AccountId == accountId
    //             //             where !a.IsDeleted
    //             //  && a.AccountId == accountId
    //             //  && a.HierarchyPath.StartsWith(_currentUser.HierarchyPath)
    //             //             select new { a, c, cat, st, ct };
    //         where !a.IsDeleted
    //             && a.AccountId == accountId
    //             && (
    //                 _currentUser.AccountId == 1
    //                 || a.HierarchyPath.StartsWith(_currentUser.HierarchyPath)
    //             )
    //         select new { a, c, cat, st, ct };

    //     return await query
    //         .Select(x => new AccountResponseDto
    //         {
    //             AccountId = x.a.AccountId,
    //             AccountCode = x.a.AccountCode,
    //             AccountName = x.a.AccountName,
    //             Reffer = x.a.RefferCode,

    //             ParentAccountId = x.a.ParentAccountId,
    //             HierarchyPath = x.a.HierarchyPath,

    //             Fk_userid = x.a.Fk_userid,

    //             CategoryId = x.cat.CategoryId,
    //             CategoryName = x.cat.LabelName,

    //             PrimaryDomain = x.a.PrimaryDomain,

    //             CountryId = x.c.CountryId,
    //             CountryName = x.c.CountryName,

    //             // ✅ state
    //             StateId = x.a.StateId,
    //             StateName = x.st != null ? x.st.StateName : "",

    //             // ✅ city
    //             CityId = x.a.CityId,
    //             CityName = x.ct != null ? x.ct.CityName : "",

    //             fullname = x.a.fullname,
    //             email = x.a.email,
    //             phone = x.a.phone,
    //             address = x.a.address,
    //             Position = x.a.Position,
    //             BusinessAddress = x.a.BusinessAddress,
    //             BusinessEmail = x.a.BusinessEmail,
    //             BusinessHours = x.a.BusinessHours,
    //             BusinessPhone = x.a.BusinessPhone,
    //             BusinessTimeZone = x.a.BusinessTimeZone,
    //             Zipcode = x.a.Zipcode,
    //             usernamesacc = x.a.UserName,
    //             // password = x.a.PasswordHash,
    //             share = x.a.share,

    //             TaxTypeId = x.a.TaxTypeId,
    //             Status = x.a.Status,

    //             CreatedOn = x.a.CreatedOn
    //         })
    //         .AsNoTracking()
    //         .FirstOrDefaultAsync();
    // }


    public async Task<AccountResponseDto?> GetByIdAsync(int accountId)
    {
        var query =
            from a in _db.Accounts

            join c0 in _db.Countries
                on a.CountryId equals c0.CountryId into countryGroup
            from c in countryGroup.DefaultIfEmpty()

            join st0 in _db.States
                on a.StateId equals st0.StateId.ToString() into stj
            from st in stj.DefaultIfEmpty()

            join ct0 in _db.Cities
                on a.CityId equals ct0.CityId.ToString() into ctj
            from ct in ctj.DefaultIfEmpty()

            join cat0 in _db.Categories
                on a.CategoryId equals cat0.CategoryId into catGroup
            from cat in catGroup.DefaultIfEmpty()

            where !a.IsDeleted
                && a.AccountId == accountId
                && (
                    _currentUser.AccountId == 1
                    || (
                        a.HierarchyPath != null &&
                        a.HierarchyPath.StartsWith(_currentUser.HierarchyPath)
                    )
                )

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

                CategoryId = x.cat != null ? x.cat.CategoryId : 0,
                CategoryName = x.cat != null ? x.cat.LabelName : "",

                PrimaryDomain = x.a.PrimaryDomain,

                CountryId = x.c != null ? x.c.CountryId : 0,
                CountryName = x.c != null ? x.c.CountryName : "",

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
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted);

            if (account == null)
                return false;

            var validTax = await _db.TaxTypes.AnyAsync(x =>
                x.TaxTypeId == req.TaxTypeId &&
                x.CountryId == req.CountryId &&
                x.IsActive);

            if (!validTax)
                throw new BadHttpRequestException("Invalid TaxType");

            await ValidateUniqueAccountUserFieldsAsync(
                businessEmail: req.BusinessEmail,
                phone: req.phone,
                userName: req.UserName,
                excludeAccountId: accountId);

            // Account Code
            if (!string.IsNullOrWhiteSpace(req.AccountCode))
            {
                var code = req.AccountCode.Trim();

                var exists = await _db.Accounts.AnyAsync(x =>
                    x.AccountCode == code &&
                    x.AccountId != accountId &&
                    !x.IsDeleted);

                if (exists)
                    throw new Exception("AccountCode exists");

                account.AccountCode = code;
            }

            // MAIN
            account.AccountName = req.AccountName.Trim();
            account.CategoryId = req.CategoryId;
            account.PrimaryDomain = req.PrimaryDomain.Trim();

            account.CountryId = req.CountryId;
            account.StateId = req.StateId;
            account.CityId = req.CityId;
            account.Zipcode = req.Zipcode;

            account.RefferCode = req.RefferCode;
            account.TaxTypeId = req.TaxTypeId;

            // CONTACT
            account.fullname = req.fullname;
            account.email = req.email?.Trim().ToLowerInvariant();
            account.phone = req.phone;
            account.Position = req.Position;
            account.address = req.address;

            // BUSINESS
            account.BusinessPhone = req.BusinessPhone;
            account.BusinessEmail = req.BusinessEmail;
            account.BusinessAddress = req.BusinessAddress;
            account.BusinessHours = req.BusinessHours;
            account.BusinessTimeZone = req.BusinessTimeZone;

            // USER ACCESS
            account.UserName = req.UserName;
            account.share = req.share;


            var hashedPassword = string.IsNullOrWhiteSpace(req.Password)
                ? null
                : BCrypt.Net.BCrypt.HashPassword(req.Password);

            if (hashedPassword != null)
                account.PasswordHash = hashedPassword;

            // ========================
            // HIERARCHY UPDATE
            // ========================
            if (req.ParentAccountId == account.AccountId)
                throw new Exception("Self parent not allowed");

            if (account.ParentAccountId != req.ParentAccountId)
            {
                var oldPath = account.HierarchyPath ?? $"/{account.AccountId}/";
                string newPath;

                if (req.ParentAccountId.HasValue)
                {
                    var parent = await _db.Accounts
                        .ApplyAccountHierarchyFilter(_currentUser)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.AccountId == req.ParentAccountId.Value &&
                            !x.IsDeleted);

                    if (parent == null)
                        throw new Exception("Invalid parent");

                    if (parent.HierarchyPath.StartsWith(oldPath))
                        throw new Exception("Circular hierarchy");

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
                account.HierarchyPath = newPath;
            }

            // ========================
            // 🔥 USER SYNC (YOUR LOGIC INCLUDED)
            // ========================
            var users = await _db.Users
                .Where(x => x.AccountId == accountId && !x.IsDeleted)
                .ToListAsync();

            foreach (var user in users)
            {
                if (!string.IsNullOrWhiteSpace(req.email))
                    user.Email = req.email.Trim().ToLowerInvariant();

                user.User_name = req.UserName;
                user.MobileNo = req.phone;

                if (hashedPassword != null)
                    user.Password_hash = hashedPassword;

                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = req.userId;
                user.UpdatedBy = _currentUser.AccountId;
                user.UpdatedAt = DateTime.UtcNow;
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
        var account = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountId == accountId);

        if (account == null) return false;

        account.Status = status;
        account.UpdatedOn = DateTime.UtcNow;

        var users = await _db.Users
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .ToListAsync();

        foreach (var user in users)
        {
            user.Status = status;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteAsync(int accountId)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var utcNow = DateTime.UtcNow;
            var actorAccountId = _currentUser.AccountId;

            var account = await _db.Accounts
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted);

            if (account == null) return false;

            var hasChildren = await _db.Accounts
                .AnyAsync(x => x.ParentAccountId == accountId && !x.IsDeleted);

            if (hasChildren)
                throw new Exception("Cannot delete account with children");

            account.IsDeleted = true;
            account.Status = false;
            account.UpdatedOn = utcNow;
            account.UpdatedBy = actorAccountId;
            account.DeletedBy = actorAccountId;
            account.DeletedOn = utcNow;

            var users = await _db.Users
                .Where(x => x.AccountId == accountId && !x.IsDeleted)
                .ToListAsync();

            var userIds = users.Select(x => x.UserId).ToList();

            foreach (var user in users)
            {
                user.IsDeleted = true;
                user.Status = false;
                user.UpdatedAt = utcNow;
                user.UpdatedBy = actorAccountId;
                user.DeletedBy = actorAccountId;
                user.DeletedAt = utcNow;
            }

            var roles = await _db.Roles
                .Where(x => x.AccountId == accountId && !x.IsDeleted)
                .ToListAsync();

            var roleIds = roles.Select(x => x.RoleId).ToList();

            foreach (var role in roles)
            {
                role.IsDeleted = true;
                role.IsActive = false;
                role.UpdatedOn = utcNow;
                role.UpdatedBy = actorAccountId;
                role.DeletedBy = actorAccountId;
                role.DeletedOn = utcNow;
            }

            // These mapping tables do not support soft delete, so remove them in bulk.
            if (userIds.Count > 0)
            {
                var userFormRights = await _db.UserFormRights
                    .Where(x => userIds.Contains(x.UserId))
                    .ToListAsync();

                if (userFormRights.Count > 0)
                    _db.UserFormRights.RemoveRange(userFormRights);
            }

            if (roleIds.Count > 0)
            {
                var formRoleRights = await _db.FormRoleRights
                    .Where(x => roleIds.Contains(x.RoleId))
                    .ToListAsync();

                if (formRoleRights.Count > 0)
                    _db.FormRoleRights.RemoveRange(formRoleRights);
            }

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

    private async Task ValidateUniqueAccountUserFieldsAsync(
        string? businessEmail,
        string? phone,
        string? userName,
        int? excludeAccountId = null)
    {
        var normalizedBusinessEmail = businessEmail?.Trim().ToLowerInvariant();
        var normalizedPhone = phone?.Trim();
        var normalizedUserName = userName?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedBusinessEmail))
        {
            var businessEmailExistsInAccounts = await _db.Accounts.AnyAsync(x =>
                !x.IsDeleted &&
                (!excludeAccountId.HasValue || x.AccountId != excludeAccountId.Value) &&
                x.BusinessEmail.ToLower() == normalizedBusinessEmail);

            if (businessEmailExistsInAccounts)
                throw new InvalidOperationException("Business email already exists");

            var businessEmailExistsInUsers = await _db.Users.AnyAsync(x =>
                !x.IsDeleted &&
                (!excludeAccountId.HasValue || x.AccountId != excludeAccountId.Value) &&
                x.Email.ToLower() == normalizedBusinessEmail);

            if (businessEmailExistsInUsers)
                throw new InvalidOperationException("Business email already exists in user table");
        }

        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            var phoneExistsInAccounts = await _db.Accounts.AnyAsync(x =>
                !x.IsDeleted &&
                (!excludeAccountId.HasValue || x.AccountId != excludeAccountId.Value) &&
                (x.phone == normalizedPhone || x.BusinessPhone == normalizedPhone));

            if (phoneExistsInAccounts)
                throw new InvalidOperationException("Phone number already exists");

            var phoneExistsInUsers = await _db.Users.AnyAsync(x =>
                !x.IsDeleted &&
                (!excludeAccountId.HasValue || x.AccountId != excludeAccountId.Value) &&
                x.MobileNo == normalizedPhone);

            if (phoneExistsInUsers)
                throw new InvalidOperationException("Phone number already exists in user table");
        }

        if (!string.IsNullOrWhiteSpace(normalizedUserName))
        {
            var userNameExistsInAccounts = await _db.Accounts.AnyAsync(x =>
                !x.IsDeleted &&
                (!excludeAccountId.HasValue || x.AccountId != excludeAccountId.Value) &&
                x.UserName.ToLower() == normalizedUserName.ToLower());

            if (userNameExistsInAccounts)
                throw new InvalidOperationException("Username already exists");

            var userNameExistsInUsers = await _db.Users.AnyAsync(x =>
                !x.IsDeleted &&
                (!excludeAccountId.HasValue || x.AccountId != excludeAccountId.Value) &&
                x.User_name != null &&
                x.User_name.ToLower() == normalizedUserName.ToLower());

            if (userNameExistsInUsers)
                throw new InvalidOperationException("Username already exists in user table");
        }
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

    private async Task SendBrandedEmailAsync(
        int accountId,
        string toEmail,
        string templateName,
        Func<string, string> subjectFactory,
        IDictionary<string, string>? placeholders = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return;

        var branding = await GetBrandingAsync(accountId);
        var template = await LoadEmailTemplateAsync(templateName);
        var body = ApplyBrandingPlaceholders(template, branding.BrandName, branding.LogoUrl, placeholders);
        var subject = subjectFactory(branding.BrandName);

        await _emailService.SendAsync(toEmail, subject, body);
    }

    private async Task<(string BrandName, string LogoUrl)> GetBrandingAsync(int accountId)
    {
        const string defaultBrandName = "Fleetbharat";

        var branding = await _db.WhiteLabels
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AccountId == accountId &&
                x.IsActive &&
                !x.IsDeleted);

        var brandName = string.IsNullOrWhiteSpace(branding?.BrandName)
            ? defaultBrandName
            : branding.BrandName.Trim();

        var logoUrl = branding?.LogoUrl?.Trim() ?? string.Empty;

        return (brandName, logoUrl);
    }

    private static string ApplyBrandingPlaceholders(
        string template,
        string brandName,
        string logoUrl,
        IDictionary<string, string>? placeholders)
    {
        var body = template
            .Replace("{{BRAND_NAME}}", brandName, StringComparison.Ordinal)
            .Replace("{{LOGO_URL}}", logoUrl, StringComparison.Ordinal);

        if (placeholders == null)
            return body;

        foreach (var placeholder in placeholders)
        {
            body = body.Replace(placeholder.Key, placeholder.Value ?? string.Empty, StringComparison.Ordinal);
        }

        return body;
    }
}
