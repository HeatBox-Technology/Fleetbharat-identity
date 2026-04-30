using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class DeviceModelService : IDeviceModelService
{
    private const string DefaultProtocolType = "UNKNOWN";
    private readonly IDeviceModelRepository _repository;

    public DeviceModelService(IDeviceModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<DeviceModelResponseDto>> GetAllAsync(
        DeviceModelGetAllRequestDto request,
        CancellationToken cancellationToken = default)
    {
        request ??= new DeviceModelGetAllRequestDto();

        if (request.ManufacturerId.HasValue && request.ManufacturerId.Value <= 0)
            throw new BadHttpRequestException("ManufacturerId filter is invalid.");

        if (request.DeviceCategoryId.HasValue && request.DeviceCategoryId.Value <= 0)
            throw new BadHttpRequestException("DeviceCategoryId filter is invalid.");

        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 200);
        request.Search = NormalizeOptionalText(
            request.Search,
            150,
            "Search text cannot exceed 150 characters.");

        return await _repository.GetAllAsync(request, cancellationToken);
    }

    public async Task<DeviceModelResponseDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<int> AddAsync(
        CreateDeviceModelRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new BadHttpRequestException("Request payload is required.");

        var validated = await ValidateWriteRequestAsync(
            request.ManufacturerId,
            request.Name,
            request.DeviceCategoryId,
            request.UseIMEIAsPrimaryId,
            request.DeviceNo,
            request.IMEISerialNumber,
            null,
            cancellationToken);

        var entity = new DeviceModel
        {
            Code = await GenerateCodeAsync(validated.Name, cancellationToken),
            Name = validated.Name,
            ManufacturerId = validated.ManufacturerId,
            DeviceCategoryId = validated.DeviceCategoryId,
            ProtocolType = DefaultProtocolType,
            UseIMEIAsPrimaryId = validated.UseIMEIAsPrimaryId,
            DeviceNo = validated.DeviceNo,
            IMEISerialNumber = validated.IMEISerialNumber,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            IsDeleted = false
        };

        return await _repository.AddAsync(entity, cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        UpdateDeviceModelRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new BadHttpRequestException("Request payload is required.");

        ValidateId(request.Id);

        var validated = await ValidateWriteRequestAsync(
            request.ManufacturerId,
            request.Name,
            request.DeviceCategoryId,
            request.UseIMEIAsPrimaryId,
            request.DeviceNo,
            request.IMEISerialNumber,
            request.Id,
            cancellationToken);

        var entity = new DeviceModel
        {
            Id = request.Id,
            Name = validated.Name,
            ManufacturerId = validated.ManufacturerId,
            DeviceCategoryId = validated.DeviceCategoryId,
            UseIMEIAsPrimaryId = validated.UseIMEIAsPrimaryId,
            DeviceNo = validated.DeviceNo,
            IMEISerialNumber = validated.IMEISerialNumber,
            IsEnabled = request.IsEnabled,
            UpdatedAt = DateTime.UtcNow
        };

        return await _repository.UpdateAsync(entity, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        return await _repository.SoftDeleteAsync(id, DateTime.UtcNow, cancellationToken);
    }

    public Task<IReadOnlyList<DropdownDto>> GetManufacturerDropdownAsync(
        CancellationToken cancellationToken = default)
    {
        return _repository.GetManufacturerDropdownAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DropdownDto>> GetDeviceTypeDropdownAsync(
        CancellationToken cancellationToken = default)
    {
        return _repository.GetDeviceTypeDropdownAsync(cancellationToken);
    }

    private async Task<ValidatedDeviceModelValues> ValidateWriteRequestAsync(
        int manufacturerId,
        string? name,
        int deviceCategoryId,
        bool useImeiAsPrimaryId,
        string? deviceNo,
        string? imeiSerialNumber,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (manufacturerId <= 0)
            throw new BadHttpRequestException("ManufacturerId is required.");

        if (deviceCategoryId <= 0)
            throw new BadHttpRequestException("DeviceCategoryId is required.");

        var normalizedName = NormalizeRequiredText(
            name,
            150,
            "Device model is required.",
            "Device model cannot exceed 150 characters.");

        var normalizedDeviceNo = NormalizeOptionalText(
            deviceNo,
            100,
            "DeviceNo cannot exceed 100 characters.");

        var normalizedImeiSerialNumber = NormalizeOptionalText(
            imeiSerialNumber,
            100,
            "IMEISerialNumber cannot exceed 100 characters.");

        if (useImeiAsPrimaryId && string.IsNullOrWhiteSpace(normalizedImeiSerialNumber))
            throw new BadHttpRequestException("IMEISerialNumber is required when UseIMEIAsPrimaryId is true.");

        if (!await _repository.ManufacturerExistsAsync(manufacturerId, cancellationToken))
            throw new InvalidOperationException("Manufacturer not found or disabled.");

        if (!await _repository.DeviceTypeExistsAsync(deviceCategoryId, cancellationToken))
            throw new InvalidOperationException("Device type not found or disabled.");

        if (!string.IsNullOrWhiteSpace(normalizedDeviceNo) &&
            await _repository.DeviceNoExistsAsync(normalizedDeviceNo, excludeId, cancellationToken))
        {
            throw new InvalidOperationException("DeviceNo already exists.");
        }

        return new ValidatedDeviceModelValues(
            manufacturerId,
            normalizedName,
            deviceCategoryId,
            useImeiAsPrimaryId,
            normalizedDeviceNo,
            normalizedImeiSerialNumber);
    }

    private async Task<string> GenerateCodeAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedBase = Regex.Replace(
            name.Trim().ToUpperInvariant(),
            "[^A-Z0-9]+",
            "-").Trim('-');

        if (string.IsNullOrWhiteSpace(normalizedBase))
            normalizedBase = "MODEL";

        normalizedBase = normalizedBase.Length > 24
            ? normalizedBase[..24]
            : normalizedBase;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var candidate = $"DM-{normalizedBase}-{suffix}";

            if (candidate.Length > 50)
                candidate = candidate[..50];

            if (!await _repository.CodeExistsAsync(candidate, cancellationToken))
                return candidate;
        }

        throw new InvalidOperationException("Unable to generate a unique device model code.");
    }

    private static void ValidateId(int id)
    {
        if (id <= 0)
            throw new BadHttpRequestException("Id is invalid.");
    }

    private static string NormalizeRequiredText(
        string? value,
        int maxLength,
        string requiredMessage,
        string maxLengthMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BadHttpRequestException(requiredMessage);

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
            throw new BadHttpRequestException(maxLengthMessage);

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string maxLengthMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
            throw new BadHttpRequestException(maxLengthMessage);

        return normalized;
    }

    private sealed record ValidatedDeviceModelValues(
        int ManufacturerId,
        string Name,
        int DeviceCategoryId,
        bool UseIMEIAsPrimaryId,
        string? DeviceNo,
        string? IMEISerialNumber);
}
