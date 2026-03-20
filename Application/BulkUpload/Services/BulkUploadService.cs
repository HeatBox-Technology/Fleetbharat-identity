using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class BulkUploadService : IBulkUploadService
{
    private readonly IdentityDbContext _db;
    private readonly IExcelParser _excelParser;
    private readonly ICsvParser _csvParser;
    private readonly IBulkUploadQueue _queue;
    private readonly ITemplateService _templateService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<BulkUploadService> _logger;

    public BulkUploadService(
        IdentityDbContext db,
        IExcelParser excelParser,
        ICsvParser csvParser,
        IBulkUploadQueue queue,
        ITemplateService templateService,
        ICurrentUserService currentUser,
        ILogger<BulkUploadService> logger)
    {
        _db = db;
        _excelParser = excelParser;
        _csvParser = csvParser;
        _queue = queue;
        _templateService = templateService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BulkUploadStartResultDto> EnqueueUploadAsync(string moduleKey, IFormFile file, CancellationToken ct = default)
    {
        var config = await _db.BulkUploadConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ModuleKey == moduleKey && x.IsActive, ct);

        if (config == null)
            throw new KeyNotFoundException($"Bulk upload config not found for module '{moduleKey}'.");

        if (file == null || file.Length == 0)
            throw new BadHttpRequestException("Upload file is required.");

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv")
            throw new BadHttpRequestException("Only .xlsx and .csv are supported.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;

        var rows = ext == ".xlsx"
            ? await _excelParser.ParseAsync(ms, ct)
            : await _csvParser.ParseAsync(ms, ct);

        var job = new bulk_job
        {
            ModuleName = moduleKey,
            FileName = file.FileName,
            TotalRows = rows.Count,
            ProcessedRows = 0,
            SuccessRows = 0,
            FailedRows = 0,
            Status = "PENDING",
            CreatedBy = null
        };

        _db.bulk_jobs.Add(job);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Bulk upload job created. JobId={JobId}, Module={Module}, Rows={Rows}", job.Id, moduleKey, rows.Count);

        await _queue.EnqueueAsync(new BulkUploadWorkItem
        {
            JobId = job.Id,
            ModuleKey = moduleKey,
            Rows = rows,
            CreatedBy = job.CreatedBy
        }, ct);

        return new BulkUploadStartResultDto
        {
            JobId = job.Id,
            ModuleKey = moduleKey,
            TotalRows = rows.Count,
            Status = "PENDING"
        };
    }

    public async Task<BulkUploadStatusDto?> GetStatusAsync(int jobId, CancellationToken ct = default)
    {
        return await _db.bulk_jobs
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => new BulkUploadStatusDto
            {
                JobId = x.Id,
                ModuleKey = x.ModuleName,
                Status = x.Status,
                TotalRows = x.TotalRows,
                ProcessedRows = x.ProcessedRows,
                SuccessRows = x.SuccessRows,
                FailedRows = x.FailedRows,
                ErrorFilePath = x.ErrorFilePath,
                CompletedAt = x.CompletedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<(byte[] Content, string ContentType, string FileName)> GetTemplateAsync(string moduleKey, string format, CancellationToken ct = default)
    {
        return _templateService.GenerateTemplateAsync(moduleKey, format, ct);
    }

    public async Task<(byte[] Content, string FileName)?> GetErrorReportAsync(int jobId, CancellationToken ct = default)
    {
        var path = await _db.bulk_jobs
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => x.ErrorFilePath)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        return (await File.ReadAllBytesAsync(path, ct), Path.GetFileName(path));
    }
}
