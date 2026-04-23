using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class VehicleComplianceService : IVehicleComplianceService
{
    private const long MaxDocumentSizeBytes = 5 * 1024 * 1024;
    private readonly IdentityDbContext _db;
    private readonly IWebHostEnvironment _env;

    public VehicleComplianceService(IdentityDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<int> CreateAsync(CreateVehicleComplianceDto dto, IFormFile? document = null)
    {
        await ValidateReferences(dto.AccountId, dto.VehicleId);

        var documentInfo = document == null
            ? (DocumentPath: dto.DocumentPath, DocumentFileName: dto.DocumentFileName, ContentType: (string?)null)
            : await SaveDocumentAsync(dto.AccountId, dto.VehicleId, document);

        var entity = new vehicle_compliance
        {
            AccountId = dto.AccountId,
            VehicleId = dto.VehicleId,
            ComplianceType = NormalizeType(dto.ComplianceType),
            DocumentNumber = dto.DocumentNumber.Trim(),
            IssueDate = dto.IssueDate,
            ExpiryDate = dto.ExpiryDate,
            ReminderBeforeDays = dto.ReminderBeforeDays <= 0 ? 7 : dto.ReminderBeforeDays,
            DocumentPath = documentInfo.DocumentPath,
            DocumentFileName = documentInfo.DocumentFileName,
            DocumentContentType = documentInfo.ContentType,
            Remarks = dto.Remarks,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.VehicleCompliances.Add(entity);
        await _db.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<VehicleComplianceListUiResponseDto> GetDocuments(
        int page,
        int pageSize,
        int? accountId,
        int? vehicleId,
        string? complianceType,
        string? status,
        string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var today = DateTime.UtcNow.Date;
        var query = BuildQuery();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (vehicleId.HasValue)
            query = query.Where(x => x.VehicleId == vehicleId.Value);

        if (!string.IsNullOrWhiteSpace(complianceType))
        {
            var type = NormalizeType(complianceType);
            query = query.Where(x => x.ComplianceType == type);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.VehicleNumber.ToLower().Contains(s) ||
                x.AccountName.ToLower().Contains(s) ||
                x.ComplianceType.ToLower().Contains(s) ||
                x.DocumentNumber.ToLower().Contains(s) ||
                (x.Remarks ?? string.Empty).ToLower().Contains(s));
        }

        var records = await query.ToListAsync();

        var projected = records.Select(x => new VehicleComplianceDto
        {
            Id = x.Id,
            AccountId = x.AccountId,
            AccountName = x.AccountName,
            VehicleId = x.VehicleId,
            VehicleNumber = x.VehicleNumber,
            ComplianceType = x.ComplianceType,
            DocumentNumber = x.DocumentNumber,
            IssueDate = x.IssueDate,
            ExpiryDate = x.ExpiryDate,
            ReminderBeforeDays = x.ReminderBeforeDays,
            Status = GetStatus(x.ExpiryDate, x.ReminderBeforeDays, today),
            DocumentPath = x.DocumentPath,
            DocumentFileName = x.DocumentFileName,
            Remarks = x.Remarks,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        });

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = NormalizeStatus(status);
            projected = projected.Where(x => x.Status == normalizedStatus);
        }

        var filtered = projected.ToList();
        var total = filtered.Count;
        var healthy = filtered.Count(x => x.Status == "Healthy");
        var dueSoon = filtered.Count(x => x.Status == "DueSoon");
        var overdue = filtered.Count(x => x.Status == "Overdue");

        var items = filtered
            .OrderBy(x => x.ExpiryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new VehicleComplianceListUiResponseDto
        {
            Summary = new VehicleComplianceSummaryDto
            {
                TotalDocuments = total,
                Healthy = healthy,
                DueSoon = dueSoon,
                Overdue = overdue
            },
            Documents = new PagedResultDto<VehicleComplianceDto>
            {
                Items = items,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        };
    }

    public async Task<VehicleComplianceDto?> GetByIdAsync(int id)
    {
        var today = DateTime.UtcNow.Date;

        var record = await BuildQuery()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (record == null)
            return null;

        return new VehicleComplianceDto
        {
            Id = record.Id,
            AccountId = record.AccountId,
            AccountName = record.AccountName,
            VehicleId = record.VehicleId,
            VehicleNumber = record.VehicleNumber,
            ComplianceType = record.ComplianceType,
            DocumentNumber = record.DocumentNumber,
            IssueDate = record.IssueDate,
            ExpiryDate = record.ExpiryDate,
            ReminderBeforeDays = record.ReminderBeforeDays,
            Status = GetStatus(record.ExpiryDate, record.ReminderBeforeDays, today),
            DocumentPath = record.DocumentPath,
            DocumentFileName = record.DocumentFileName,
            Remarks = record.Remarks,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    public async Task<bool> UpdateAsync(int id, UpdateVehicleComplianceDto dto, IFormFile? document = null)
    {
        var entity = await _db.VehicleCompliances.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
            return false;

        await ValidateReferences(dto.AccountId, dto.VehicleId);

        if (document != null)
        {
            var documentInfo = await SaveDocumentAsync(dto.AccountId, dto.VehicleId, document);
            entity.DocumentPath = documentInfo.DocumentPath;
            entity.DocumentFileName = documentInfo.DocumentFileName;
            entity.DocumentContentType = documentInfo.ContentType;
        }
        else
        {
            entity.DocumentPath = dto.DocumentPath ?? entity.DocumentPath;
            entity.DocumentFileName = dto.DocumentFileName ?? entity.DocumentFileName;
        }

        entity.AccountId = dto.AccountId;
        entity.VehicleId = dto.VehicleId;
        entity.ComplianceType = NormalizeType(dto.ComplianceType);
        entity.DocumentNumber = dto.DocumentNumber.Trim();
        entity.IssueDate = dto.IssueDate;
        entity.ExpiryDate = dto.ExpiryDate;
        entity.ReminderBeforeDays = dto.ReminderBeforeDays <= 0 ? 7 : dto.ReminderBeforeDays;
        entity.Remarks = dto.Remarks;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.VehicleCompliances.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    private IQueryable<VehicleComplianceProjection> BuildQuery()
    {
        return
            from compliance in _db.VehicleCompliances.AsNoTracking()
            join vehicle in _db.Vehicles.AsNoTracking()
                on compliance.VehicleId equals vehicle.Id
            join account in _db.Accounts.AsNoTracking()
                on compliance.AccountId equals account.AccountId
            where !compliance.IsDeleted && !vehicle.IsDeleted && !account.IsDeleted
            select new VehicleComplianceProjection
            {
                Id = compliance.Id,
                AccountId = compliance.AccountId,
                AccountName = account.AccountName,
                VehicleId = compliance.VehicleId,
                VehicleNumber = vehicle.VehicleNumber,
                ComplianceType = compliance.ComplianceType,
                DocumentNumber = compliance.DocumentNumber,
                IssueDate = compliance.IssueDate,
                ExpiryDate = compliance.ExpiryDate,
                ReminderBeforeDays = compliance.ReminderBeforeDays,
                DocumentPath = compliance.DocumentPath,
                DocumentFileName = compliance.DocumentFileName,
                Remarks = compliance.Remarks,
                CreatedAt = compliance.CreatedAt,
                UpdatedAt = compliance.UpdatedAt
            };
    }

    private async Task ValidateReferences(int accountId, int vehicleId)
    {
        var vehicleExists = await _db.Vehicles
            .AnyAsync(x => x.Id == vehicleId && x.AccountId == accountId && !x.IsDeleted);

        if (!vehicleExists)
            throw new InvalidOperationException("Vehicle not found for selected organization.");
    }

    private async Task<(string DocumentPath, string DocumentFileName, string? ContentType)> SaveDocumentAsync(
        int accountId,
        int vehicleId,
        IFormFile document)
    {
        ValidateDocument(document);

        var extension = Path.GetExtension(document.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine("uploads", "vehicle-compliance", accountId.ToString(), vehicleId.ToString(), fileName);
        var physicalPath = Path.Combine(_env.ContentRootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        await using var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write);
        await document.CopyToAsync(fs);

        return ("/" + relativePath.Replace("\\", "/"), document.FileName, document.ContentType);
    }

    private static void ValidateDocument(IFormFile document)
    {
        if (document.Length == 0)
            throw new InvalidOperationException("Document is empty.");

        if (document.Length > MaxDocumentSizeBytes)
            throw new InvalidOperationException("Document size must be less than 5MB.");
    }

    private static string NormalizeType(string complianceType)
    {
        return complianceType.Trim().ToUpperInvariant();
    }

    private static string NormalizeStatus(string status)
    {
        var value = status.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
        return value.Equals("duesoon", StringComparison.OrdinalIgnoreCase)
            ? "DueSoon"
            : value.Equals("overdue", StringComparison.OrdinalIgnoreCase)
                ? "Overdue"
                : "Healthy";
    }

    private static string GetStatus(DateTime expiryDate, int reminderBeforeDays, DateTime today)
    {
        var expiry = expiryDate.Date;
        if (expiry < today)
            return "Overdue";

        return expiry <= today.AddDays(reminderBeforeDays <= 0 ? 7 : reminderBeforeDays)
            ? "DueSoon"
            : "Healthy";
    }

    private class VehicleComplianceProjection
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string ComplianceType { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int ReminderBeforeDays { get; set; }
        public string? DocumentPath { get; set; }
        public string? DocumentFileName { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
