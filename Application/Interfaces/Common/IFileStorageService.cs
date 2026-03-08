using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IFileStorageService
{
    Task<string> SaveProfileImageAsync(Guid userId, IFormFile file);
    Task<string> SaveWhiteLabelLogoAsync(int accountId, IFormFile file);
}

