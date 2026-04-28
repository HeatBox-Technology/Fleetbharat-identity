using System.Collections.Generic;
using System.Threading.Tasks;

public interface IFormConfigurationService
{
    Task<List<FormPageResponseDto>> GetFormPagesAsync();
    Task<List<FormFieldResponseDto>> GetFormFieldsAsync(string? pageKey);
    Task<FormFieldResponseDto> CreateFormFieldAsync(CreateFormFieldRequestDto request);
    Task<FormConfigurationResponseDto> GetFormConfigurationAsync(int accountId, string? pageKey);
    Task SaveFormConfigurationAsync(SaveFormConfigurationRequestDto request);
}
