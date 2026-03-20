using System.Collections.Generic;
using System.Threading.Tasks;

public interface ITaxTypeService
{
    Task<int> CreateAsync(CreateTaxTypeRequest req);
    Task<bool> UpdateAsync(int taxTypeId, UpdateTaxTypeRequest req);
    Task<bool> DeleteAsync(int taxTypeId);

    Task<List<TaxTypeResponseDto>> GetAllAsync(string? search, int? countryId, bool? isActive, int page = 1, int pageSize = 10);
    Task<List<TaxTypeResponseDto>> GetByCountryAsync(int countryId);
    Task<TaxTypeResponseDto?> GetByIdAsync(int taxTypeId);

    Task<bool> UpdateStatusAsync(int taxTypeId, bool isActive);
}
