using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class MstDeviceTypeService : IMstDeviceTypeService
{
    private readonly IMstDeviceTypeRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public MstDeviceTypeService(
        IMstDeviceTypeRepository repository,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<int> CreateAsync(CreateMstDeviceTypeRequestDto request)
    {
        if (request == null)
            throw new BadHttpRequestException("Request payload is required.");

        var code = NormalizeCode(request.Code);
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);

        ValidateOemManufacturerId(request.OemManufacturerId);

        if (await _repository.CodeExistsAsync(code))
            throw new InvalidOperationException("Device type already exists.");

        if (!await _repository.OemManufacturerExistsAsync(request.OemManufacturerId))
            throw new InvalidOperationException("OEM manufacturer not found or disabled.");

        var entity = new mst_device_type
        {
            Code = code,
            Name = name,
            Description = description,
            IsEnabled = request.IsEnabled,
            IsActive = request.IsActive,
            CreatedBy = ResolveAuditUserId(request.CreatedBy, "CreatedBy"),
            CreatedAt = DateTime.UtcNow,
            oemmanufacturerid = request.OemManufacturerId,
            IsDeleted = false
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<List<MstDeviceTypeResponseDto>> GetAllAsync(string? search = null)
    {
        var entities = await _repository.GetAllActiveAsync(search);
        return entities.Select(MapToResponse).ToList();
    }

    public async Task<MstDeviceTypeResponseDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new BadHttpRequestException("Id is invalid.");

        var entity = await _repository.GetActiveByIdAsync(id);
        return entity == null ? null : MapToResponse(entity);
    }

    public async Task<bool> UpdateAsync(int id, UpdateMstDeviceTypeRequestDto request)
    {
        if (id <= 0)
            throw new BadHttpRequestException("Id is invalid.");

        if (request == null)
            throw new BadHttpRequestException("Request payload is required.");

        var entity = await _repository.GetByIdForWriteAsync(id);
        if (entity == null)
            return false;

        var code = NormalizeCode(request.Code);
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);

        ValidateOemManufacturerId(request.OemManufacturerId);

        if (await _repository.CodeExistsAsync(code, id))
            throw new InvalidOperationException("Device type already exists.");

        if (!await _repository.OemManufacturerExistsAsync(request.OemManufacturerId))
            throw new InvalidOperationException("OEM manufacturer not found or disabled.");

        entity.Code = code;
        entity.Name = name;
        entity.Description = description;
        entity.IsEnabled = request.IsEnabled;
        entity.IsActive = request.IsActive;
        entity.oemmanufacturerid = request.OemManufacturerId;
        entity.UpdatedBy = ResolveAuditUserId(request.UpdatedBy, "UpdatedBy");
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        if (id <= 0)
            throw new BadHttpRequestException("Id is invalid.");

        var entity = await _repository.GetByIdForWriteAsync(id);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : entity.UpdatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync();
        return true;
    }

    private int ResolveAuditUserId(int? requestUserId, string fieldName)
    {
        if (requestUserId.HasValue && requestUserId.Value > 0)
            return requestUserId.Value;

        if (_currentUser.AccountId > 0)
            return _currentUser.AccountId;

        throw new BadHttpRequestException($"{fieldName} is required.");
    }

    private static string NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BadHttpRequestException("Code is required.");

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 50)
            throw new BadHttpRequestException("Code cannot exceed 50 characters.");

        return normalized;
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BadHttpRequestException("Name is required.");

        var normalized = name.Trim();
        if (normalized.Length > 150)
            throw new BadHttpRequestException("Name cannot exceed 150 characters.");

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    private static void ValidateOemManufacturerId(int oemManufacturerId)
    {
        if (oemManufacturerId <= 0)
            throw new BadHttpRequestException("OemManufacturerId is required.");
    }

    private static MstDeviceTypeResponseDto MapToResponse(mst_device_type entity)
    {
        return new MstDeviceTypeResponseDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            IsEnabled = entity.IsEnabled,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            OemManufacturerId = entity.oemmanufacturerid,
            UpdatedBy = entity.UpdatedBy,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            IsActive = entity.IsActive
        };
    }
}
