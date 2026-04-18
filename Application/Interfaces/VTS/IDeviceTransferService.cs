using System;
using System.Threading.Tasks;

public interface IDeviceTransferService
{
    Task<DeviceTransferCreateResultDto> CreateAsync(CreateDeviceTransferRequest request);

    Task<DeviceTransferActionResultDto> ApproveAsync(int transferId, UpdateDeviceTransferStatusRequest? request);

    Task<DeviceTransferActionResultDto> CancelAsync(int transferId, UpdateDeviceTransferStatusRequest? request);

    Task<DeviceTransferDto?> GetByIdAsync(int transferId);

    Task<DeviceTransferListUiResponseDto> GetTransfersAsync(
        int page,
        int pageSize,
        int? accountId = null,
        int? fromAccountId = null,
        int? toAccountId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? search = null);
}
