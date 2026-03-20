using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

public class FileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveProfileImageAsync(Guid userId, IFormFile file)
    {
        var allowed = new[] { "image/jpeg", "image/jpg", "image/png" };
        ValidateFile(file, allowed);

        var extension = file.ContentType.Contains("png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
        var relativePath = Path.Combine("uploads", "profiles", $"{userId}.{extension}");
        var physicalPath = Path.Combine(_env.ContentRootPath, relativePath);

        var dir = Path.GetDirectoryName(physicalPath)!;
        Directory.CreateDirectory(dir);

        await using var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write);
        await file.CopyToAsync(fs);

        return "/" + relativePath.Replace("\\", "/");
    }

    public async Task<string> SaveWhiteLabelLogoAsync(int accountId, IFormFile file)
    {
        return await SavePrimaryLogoAsync(accountId, file);
    }

    public Task<string> SavePrimaryLogoAsync(int accountId, IFormFile file) =>
        SaveWhiteLabelVariantAsync(accountId, "primary", file);

    public Task<string> SaveAppLogoAsync(int accountId, IFormFile file) =>
        SaveWhiteLabelVariantAsync(accountId, "app", file);

    public Task<string> SaveMobileLogoAsync(int accountId, IFormFile file) =>
        SaveWhiteLabelVariantAsync(accountId, "mobile", file);

    public Task<string> SaveFaviconAsync(int accountId, IFormFile file) =>
        SaveWhiteLabelVariantAsync(accountId, "favicon", file);

    public Task<string> SaveDarkLogoAsync(int accountId, IFormFile file) =>
        SaveWhiteLabelVariantAsync(accountId, "dark", file);

    public Task<string> SaveLightLogoAsync(int accountId, IFormFile file) =>
        SaveWhiteLabelVariantAsync(accountId, "light", file);

    private async Task<string> SaveWhiteLabelVariantAsync(int accountId, string variant, IFormFile file)
    {
        var allowed = new[] { "image/jpeg", "image/jpg", "image/png" };
        ValidateFile(file, allowed);

        var extension = file.ContentType.Contains("png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
        var relativePath = Path.Combine("uploads", "whitelabel", accountId.ToString(), variant, $"{accountId}.{extension}");
        var physicalPath = Path.Combine(_env.ContentRootPath, relativePath);

        var dir = Path.GetDirectoryName(physicalPath)!;
        Directory.CreateDirectory(dir);

        await using var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write);
        await file.CopyToAsync(fs);

        return "/" + relativePath.Replace("\\", "/");
    }

    private static void ValidateFile(IFormFile file, string[] allowedContentTypes)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File is required");

        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file type");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File size must be less than 2MB");
    }
}
