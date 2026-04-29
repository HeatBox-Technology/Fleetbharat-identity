using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class BulkService : IBulkService
{
    private readonly IdentityDbContext _db;
    private readonly BulkQueue _queue;
    private readonly BulkProcessorFactory _factory;

    public BulkService(
        IdentityDbContext db,
        BulkQueue queue,
        BulkProcessorFactory factory)
    {
        _db = db;
        _queue = queue;
        _factory = factory;
    }

    public async Task<int> CreateJobAsync(string module, IFormFile file)
    {
        var filePath = Path.Combine("uploads", $"{Guid.NewGuid()}_{file.FileName}");

        Directory.CreateDirectory("uploads");

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var job = new bulk_job
        {
            ModuleName = module,
            FileName = filePath,
            Status = "PENDING"
        };

        _db.bulk_jobs.Add(job);
        await _db.SaveChangesAsync();

        await SaveRows(job.Id, module, filePath);

        await _queue.EnqueueAsync(job.Id);

        return job.Id;
    }

    private async Task SaveRows(int jobId, string module, string filePath)
    {
        var rows = File.ReadAllLines(filePath).Skip(1).ToList();

        int rowNo = 1;

        foreach (var line in rows)
        {
            var row = new bulk_job_row
            {
                JobId = jobId,
                ModuleName = module,
                RowNumber = rowNo++,
                PayloadJson = line
            };

            _db.bulk_job_rows.Add(row);
        }

        var job = await _db.bulk_jobs.FindAsync(jobId);
        if (job == null)
        {
            throw new InvalidOperationException($"Bulk job {jobId} was not found after creation.");
        }

        job.TotalRows = rows.Count;

        await _db.SaveChangesAsync();
    }

    public async Task ProcessJobAsync(int jobId)
    {
        var job = await _db.bulk_jobs.FindAsync(jobId);
        if (job == null)
        {
            throw new InvalidOperationException($"Bulk job {jobId} was not found.");
        }

        job.Status = "PROCESSING";
        await _db.SaveChangesAsync();

        var rows = await _db.bulk_job_rows
            .Where(x => x.JobId == jobId && x.Status == "PENDING")
            .ToListAsync();

        var processor = _factory.Get(job.ModuleName);

        await Parallel.ForEachAsync(rows,
            new ParallelOptions { MaxDegreeOfParallelism = 10 },
            async (row, token) =>
            {
                try
                {
                    await processor.ProcessAsync(row.PayloadJson);

                    row.Status = "SUCCESS";
                    // Interlocked.Increment(ref job.SuccessRows);
                }
                catch (Exception ex)
                {
                    row.Status = "FAILED";
                    row.ErrorMessage = ex.Message;
                    row.RetryCount++;

                    // Interlocked.Increment(ref job.FailedRows);
                }

                //  Interlocked.Increment(ref job.ProcessedRows);
            });

        job.Status = "COMPLETED";
        job.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task RetryFailedAsync(int jobId)
    {
        var rows = await _db.bulk_job_rows
            .Where(x => x.JobId == jobId && x.Status == "FAILED")
            .ToListAsync();

        foreach (var row in rows)
            row.Status = "PENDING";

        await _db.SaveChangesAsync();

        await _queue.EnqueueAsync(jobId);
    }

    public async Task<object?> GetStatusAsync(int jobId)
    {
        return await _db.bulk_jobs
            .Where(x => x.Id == jobId)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.TotalRows,
                x.ProcessedRows,
                x.SuccessRows,
                x.FailedRows
            })
            .FirstOrDefaultAsync();
    }
}
