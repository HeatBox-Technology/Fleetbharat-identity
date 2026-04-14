using Application.DTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class SimService : ISimService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SimService(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> CreateAsync(CreateSimDto dto)
    {
        // Account validation
        var accountExists = await _db.Accounts
            .AnyAsync(x => x.AccountId == dto.AccountId);

        if (!accountExists)
            throw new Exception("Invalid AccountId");

        // ICCID uniqueness
        var exists = await _db.Sims
            .AnyAsync(x => x.Iccid == dto.Iccid && !x.IsDeleted);

        if (exists)
            throw new Exception("ICCID already exists");

        var normalizedNetworkProviderId = await NormalizeAndValidateNetworkProviderIdAsync(dto.NetworkProviderId);

        var entity = new mst_sim
        {
            AccountId = dto.AccountId,
            Iccid = dto.Iccid,
            Msisdn = dto.Msisdn,
            Imsi = dto.Imsi,
            NetworkProviderId = normalizedNetworkProviderId,
            StatusKey = dto.StatusKey,
            ActivatedAt = dto.ActivatedAt,
            ExpiryAt = dto.ExpiryAt,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        _db.Sims.Add(entity);
        await _db.SaveChangesAsync();

        return entity.SimId;
    }

    public async Task<bool> UpdateAsync(int id, UpdateSimDto dto)
    {
        var entity = await _db.Sims
            .FirstOrDefaultAsync(x => x.SimId == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("SIM not found");

        var normalizedNetworkProviderId = await NormalizeAndValidateNetworkProviderIdAsync(dto.NetworkProviderId);

        entity.Iccid = dto.Iccid;
        entity.Msisdn = dto.Msisdn;
        entity.Imsi = dto.Imsi;
        entity.NetworkProviderId = normalizedNetworkProviderId;
        entity.StatusKey = dto.StatusKey;
        entity.ActivatedAt = dto.ActivatedAt;
        entity.ExpiryAt = dto.ExpiryAt;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isActive)
    {
        var entity = await _db.Sims
            .FirstOrDefaultAsync(x => x.SimId == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("SIM not found");

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Sims
            .FirstOrDefaultAsync(x => x.SimId == id && !x.IsDeleted);

        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<SimDto?> GetByIdAsync(int id)
    {
        return await _db.Sims
            .Where(x => x.SimId == id && !x.IsDeleted)
            .Select(x => MapToDto(x))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<SimDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.Sims
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Iccid != null && x.Iccid.ToLower().Contains(s)) ||
                (x.Msisdn != null && x.Msisdn.ToLower().Contains(s)) ||
                (x.Imsi != null && x.Imsi.ToLower().Contains(s)));
        }

        return await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync();
    }

    public async Task<PagedResultDto<SimDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.Sims
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Iccid != null && x.Iccid.ToLower().Contains(s)) ||
                (x.Msisdn != null && x.Msisdn.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync();

        return new PagedResultDto<SimDto>
        {
            Items = items,
            TotalRecords = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SimListUiResponseDto> GetSims(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.Sims
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Iccid != null && x.Iccid.ToLower().Contains(s)) ||
                (x.Msisdn != null && x.Msisdn.ToLower().Contains(s)) ||
                (x.Imsi != null && x.Imsi.ToLower().Contains(s)));
        }

        var summaryData = await query
            .GroupBy(x => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(x => x.IsActive),
                Expired = g.Count(x => x.ExpiryAt != null && x.ExpiryAt < DateTime.UtcNow)
            })
            .FirstOrDefaultAsync();

        var total = summaryData?.Total ?? 0;
        var active = summaryData?.Active ?? 0;
        var expired = summaryData?.Expired ?? 0;

        var summary = new SimSummaryDto
        {
            TotalSims = total,
            Active = active,
            Inactive = total - active,
            Expired = expired
        };

        var paged = await GetPagedAsync(page, pageSize, accountId, search);

        return new SimListUiResponseDto
        {
            Summary = summary,
            Sims = paged
        };
    }

    public async Task<List<SimDto>> BulkCreateAsync(List<CreateSimDto> sims)
    {
        var requestedProviderIds = sims
            .Select(x => x.NetworkProviderId)
            .Where(x => x.HasValue && x.Value > 0)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (requestedProviderIds.Count > 0)
        {
            var existingProviderIds = await _db.NetworkProviders
                .AsNoTracking()
                .Where(x => requestedProviderIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            var missingProviderIds = requestedProviderIds
                .Except(existingProviderIds)
                .OrderBy(x => x)
                .ToList();

            if (missingProviderIds.Count > 0)
                throw new Exception($"Invalid NetworkProviderId(s): {string.Join(", ", missingProviderIds)}");
        }

        var entities = sims.Select(dto => new mst_sim
        {
            AccountId = dto.AccountId,
            Iccid = dto.Iccid,
            Msisdn = dto.Msisdn,
            Imsi = dto.Imsi,
            NetworkProviderId = dto.NetworkProviderId.HasValue && dto.NetworkProviderId.Value > 0
                ? dto.NetworkProviderId
                : null,
            StatusKey = dto.StatusKey,
            ActivatedAt = dto.ActivatedAt,
            ExpiryAt = dto.ExpiryAt,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        }).ToList();

        _db.Sims.AddRange(entities);
        await _db.SaveChangesAsync();

        return entities.Select(MapToDto).ToList();
    }

    public async Task<byte[]> ExportSimsCsvAsync(int? accountId = null, string? search = null)
    {
        var query = _db.Sims
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Iccid != null && x.Iccid.ToLower().Contains(s)) ||
                (x.Msisdn != null && x.Msisdn.ToLower().Contains(s)) ||
                (x.Imsi != null && x.Imsi.ToLower().Contains(s)));
        }

        var data = await (
            from sim in query
            join account in _db.Accounts.AsNoTracking() on sim.AccountId equals account.AccountId into accountJoin
            from account in accountJoin.DefaultIfEmpty()
            join provider in _db.NetworkProviders.AsNoTracking() on sim.NetworkProviderId equals provider.Id into providerJoin
            from provider in providerJoin.DefaultIfEmpty()
            orderby sim.UpdatedAt ?? sim.CreatedAt descending
            select new
            {
                AccountCode = account != null ? account.AccountCode : string.Empty,
                AccountName = account != null ? account.AccountName : string.Empty,
                sim.Iccid,
                sim.Msisdn,
                sim.Imsi,
                NetworkProvider = provider != null ? provider.Name : string.Empty,
                sim.StatusKey,
                sim.IsActive,
                sim.ActivatedAt,
                sim.ExpiryAt,
                sim.CreatedAt,
                sim.UpdatedAt
            })
            .ToListAsync();

        static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        static string EscapeExcelText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var trimmed = value.Trim();

            // CSV has no type metadata, so Excel guesses these long identifiers as numbers.
            // Emit them as a text formula so Excel preserves all digits on open.
            return Escape($"=\"{trimmed.Replace("\"", "\"\"")}\"");
        }

        static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Account Code,Account Name,ICCID,Mobile Number,IMSI,Network Provider,Status Key,Active Status,Activated On,Expiry On,Created On,Updated On");

        foreach (var item in data)
        {
            sb.AppendLine(
                $"{Escape(item.AccountCode)}," +
                $"{Escape(item.AccountName)}," +
                $"{EscapeExcelText(item.Iccid)}," +
                $"{EscapeExcelText(item.Msisdn)}," +
                $"{EscapeExcelText(item.Imsi)}," +
                $"{Escape(item.NetworkProvider)}," +
                $"{Escape(item.StatusKey)}," +
                $"{Escape(item.IsActive ? "Active" : "Inactive")}," +
                $"{Escape(FormatDate(item.ActivatedAt))}," +
                $"{Escape(FormatDate(item.ExpiryAt))}," +
                $"{Escape(FormatDate(item.CreatedAt))}," +
                $"{Escape(FormatDate(item.UpdatedAt))}"
            );
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static SimDto MapToDto(mst_sim x)
    {
        return new SimDto
        {
            SimId = x.SimId,
            AccountId = x.AccountId,
            Iccid = x.Iccid,
            Msisdn = x.Msisdn,
            Imsi = x.Imsi,
            NetworkProviderId = x.NetworkProviderId,
            StatusKey = x.StatusKey,
            ActivatedAt = x.ActivatedAt,
            ExpiryAt = x.ExpiryAt,
            CreatedAt = x.CreatedAt,
            CreatedBy = x.CreatedBy,
            UpdatedAt = x.UpdatedAt,
            UpdatedBy = x.UpdatedBy,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted
        };
    }

    public async Task<byte[]> ExportSimsXlsxAsync(int? accountId = null, string? search = null)
    {
        var query = _db.Sims
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Iccid != null && x.Iccid.ToLower().Contains(s)) ||
                (x.Msisdn != null && x.Msisdn.ToLower().Contains(s)) ||
                (x.Imsi != null && x.Imsi.ToLower().Contains(s)));
        }

        var data = await (
            from sim in query
            join account in _db.Accounts.AsNoTracking() on sim.AccountId equals account.AccountId into accountJoin
            from account in accountJoin.DefaultIfEmpty()
            join provider in _db.NetworkProviders.AsNoTracking() on sim.NetworkProviderId equals provider.Id into providerJoin
            from provider in providerJoin.DefaultIfEmpty()
            orderby sim.UpdatedAt ?? sim.CreatedAt descending
            select new
            {
                AccountCode = account != null ? account.AccountCode : string.Empty,
                AccountName = account != null ? account.AccountName : string.Empty,
                sim.Iccid,
                sim.Msisdn,
                sim.Imsi,
                NetworkProvider = provider != null ? provider.Name : string.Empty,
                sim.StatusKey,
                sim.IsActive,
                sim.ActivatedAt,
                sim.ExpiryAt,
                sim.CreatedAt,
                sim.UpdatedAt
            })
            .ToListAsync();

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("SIMs");

            // Add headers
            worksheet.Cell(1, 1).Value = "Account Code";
            worksheet.Cell(1, 2).Value = "Account Name";
            worksheet.Cell(1, 3).Value = "ICCID";
            worksheet.Cell(1, 4).Value = "Mobile Number";
            worksheet.Cell(1, 5).Value = "IMSI";
            worksheet.Cell(1, 6).Value = "Network Provider";
            worksheet.Cell(1, 7).Value = "Status Key";
            worksheet.Cell(1, 8).Value = "Active Status";
            worksheet.Cell(1, 9).Value = "Activated On";
            worksheet.Cell(1, 10).Value = "Expiry On";
            worksheet.Cell(1, 11).Value = "Created On";
            worksheet.Cell(1, 12).Value = "Updated On";

            // Style header row
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            // Keep long SIM identifiers as text in Excel.
            worksheet.Column(3).Style.NumberFormat.Format = "@";
            worksheet.Column(4).Style.NumberFormat.Format = "@";
            worksheet.Column(5).Style.NumberFormat.Format = "@";

            // Add data
            int rowNumber = 2;
            foreach (var item in data)
            {
                worksheet.Cell(rowNumber, 1).Value = item.AccountCode;
                worksheet.Cell(rowNumber, 2).Value = item.AccountName;
                worksheet.Cell(rowNumber, 3).Value = item.Iccid;
                worksheet.Cell(rowNumber, 4).Value = item.Msisdn;
                worksheet.Cell(rowNumber, 5).Value = item.Imsi;
                worksheet.Cell(rowNumber, 6).Value = item.NetworkProvider;
                worksheet.Cell(rowNumber, 7).Value = item.StatusKey;
                worksheet.Cell(rowNumber, 8).Value = item.IsActive ? "Active" : "Inactive";
                worksheet.Cell(rowNumber, 9).Value = item.ActivatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                worksheet.Cell(rowNumber, 10).Value = item.ExpiryAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                worksheet.Cell(rowNumber, 11).Value = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(rowNumber, 12).Value = item.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                rowNumber++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Return as bytes
            using (var stream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(stream);
                stream.Flush();
                return stream.ToArray();
            }
        }
    }

    private async Task<int?> NormalizeAndValidateNetworkProviderIdAsync(int? networkProviderId)
    {
        if (!networkProviderId.HasValue || networkProviderId.Value <= 0)
            return null;

        var exists = await _db.NetworkProviders
            .AsNoTracking()
            .AnyAsync(x => x.Id == networkProviderId.Value);

        if (!exists)
            throw new Exception($"Invalid NetworkProviderId: {networkProviderId.Value}");

        return networkProviderId.Value;
    }
}
