using System.Threading.Tasks;

public interface INetworkProviderService
{
    Task<int> CreateAsync(NetworkProviderDto dto);

    Task<NetworkProviderListUiResponseDto> GetProviders(
        int page,
        int pageSize,
        string? search);

    Task<NetworkProviderDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(int id, NetworkProviderDto dto);

    Task<bool> UpdateStatusAsync(int id, bool isEnabled);

    Task<bool> DeleteAsync(int id);
}
