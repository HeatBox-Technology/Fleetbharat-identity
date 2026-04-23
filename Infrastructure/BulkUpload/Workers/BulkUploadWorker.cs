using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

public class BulkUploadWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBulkUploadQueue _queue;
    private readonly ILogger<BulkUploadWorker> _logger;

    public BulkUploadWorker(
        IServiceScopeFactory scopeFactory,
        IBulkUploadQueue queue,
        ILogger<BulkUploadWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in _queue.DequeueAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();

            try
            {
                var backgroundCurrentUser = scope.ServiceProvider.GetRequiredService<BackgroundCurrentUserContext>();
                backgroundCurrentUser.UserId = workItem.UserId;
                backgroundCurrentUser.AccountId = workItem.AccountId;
                backgroundCurrentUser.RoleId = workItem.RoleId;
                backgroundCurrentUser.Role = workItem.Role;
                backgroundCurrentUser.HierarchyPath = workItem.HierarchyPath;
                backgroundCurrentUser.IsSystemRole = workItem.IsSystemRole;
                backgroundCurrentUser.IsAuthenticated = workItem.IsAuthenticated;
                backgroundCurrentUser.AccessibleAccountIds = workItem.AccessibleAccountIds;

                var processor = scope.ServiceProvider.GetRequiredService<IBulkProcessor>();
                await processor.ProcessAsync(workItem, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk worker failed for JobId={JobId}, Module={Module}", workItem.JobId, workItem.ModuleKey);

                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                    var job = await db.bulk_jobs.FirstOrDefaultAsync(x => x.Id == workItem.JobId, stoppingToken);
                    if (job != null)
                    {
                        job.ErrorFilePath = await GenerateFatalErrorReportAsync(workItem, ex, stoppingToken);
                        job.ProcessedRows = workItem.Rows?.Count ?? 0;
                        job.FailedRows = workItem.Rows?.Count ?? 0;
                        job.SuccessRows = 0;
                        job.Status = "COMPLETED_WITH_ERRORS";
                        job.CompletedAt = DateTime.UtcNow;

                        if (workItem.Rows != null && workItem.Rows.Count > 0)
                        {
                            var rowErrors = workItem.Rows.Select((row, index) => new bulk_job_row
                            {
                                JobId = workItem.JobId,
                                ModuleName = workItem.ModuleKey,
                                RowNumber = index + 2,
                                PayloadJson = JsonSerializer.Serialize(row),
                                Status = "FAILED",
                                ErrorMessage = ex.Message,
                                RetryCount = 0
                            }).ToList();

                            db.bulk_job_rows.AddRange(rowErrors);
                        }

                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception statusEx)
                {
                    _logger.LogError(statusEx, "Failed to mark bulk job {JobId} as failed after worker exception", workItem.JobId);
                }
            }
        }
    }

    private static async Task<string> GenerateFatalErrorReportAsync(
        BulkUploadWorkItem workItem,
        Exception exception,
        CancellationToken ct)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "uploads", "bulk-errors");
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, $"bulk_errors_{workItem.JobId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Errors");

        sheet.Cell(1, 1).Value = "RowNumber";
        sheet.Cell(1, 2).Value = "ErrorMessage";
        sheet.Row(1).Style.Font.Bold = true;

        var totalRows = workItem.Rows?.Count ?? 0;
        if (totalRows == 0)
        {
            sheet.Cell(2, 1).Value = 0;
            sheet.Cell(2, 2).Value = exception.Message;
        }
        else
        {
            for (var i = 0; i < totalRows; i++)
            {
                ct.ThrowIfCancellationRequested();
                sheet.Cell(i + 2, 1).Value = i + 2;
                sheet.Cell(i + 2, 2).Value = exception.Message;
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        await File.WriteAllBytesAsync(filePath, stream.ToArray(), ct);
        return filePath;
    }
}
