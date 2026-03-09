using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IWhiteLabelService
{
    Task<int> CreateAsync(CreateWhiteLabelRequest req);
    Task<bool> UpdateAsync(int id, UpdateWhiteLabelRequest req);
    Task<bool> DeleteAsync(int id);

    Task<WhiteLabelResponseDto?> GetByIdAsync(int id);
    Task<WhiteLabelResponseDto?> GetByAccountIdAsync(int accountId);

    Task<PagedResultDto<WhiteLabelResponseDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive);

    Task<WhiteLabelLogoUploadResponseDto> UploadLogoAsync(int accountId, IFormFile file);
    Task<WhiteLabelLogoUploadResponseDto> UploadLogosAsync(WhiteLabelLogoUploadRequest req);
}
