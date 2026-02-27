using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IBulkService
{
    Task<int> CreateJobAsync(string module, IFormFile file);
    Task ProcessJobAsync(int jobId);
    Task RetryFailedAsync(int jobId);
    Task<object> GetStatusAsync(int jobId);
}