using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IFileStorageService
{
    Task<string> SaveProfileImageAsync(Guid userId, IFormFile file);
    Task<string> SaveWhiteLabelLogoAsync(int accountId, IFormFile file);
    Task<string> SavePrimaryLogoAsync(int accountId, IFormFile file);
    Task<string> SaveAppLogoAsync(int accountId, IFormFile file);
    Task<string> SaveMobileLogoAsync(int accountId, IFormFile file);
    Task<string> SaveFaviconAsync(int accountId, IFormFile file);
    Task<string> SaveDarkLogoAsync(int accountId, IFormFile file);
    Task<string> SaveLightLogoAsync(int accountId, IFormFile file);
}
