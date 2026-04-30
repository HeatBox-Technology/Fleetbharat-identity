using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IDeviceModelRepository
{
    Task<PagedResultDto<DeviceModelResponseDto>> GetAllAsync(
        DeviceModelGetAllRequestDto request,
        CancellationToken cancellationToken = default);

    Task<DeviceModelResponseDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> ManufacturerExistsAsync(
        int manufacturerId,
        CancellationToken cancellationToken = default);

    Task<bool> DeviceTypeExistsAsync(
        int deviceCategoryId,
        CancellationToken cancellationToken = default);

    Task<bool> DeviceNoExistsAsync(
        string deviceNo,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<int> AddAsync(
        DeviceModel entity,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        DeviceModel entity,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        int id,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DropdownDto>> GetManufacturerDropdownAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DropdownDto>> GetDeviceTypeDropdownAsync(
        CancellationToken cancellationToken = default);
}
