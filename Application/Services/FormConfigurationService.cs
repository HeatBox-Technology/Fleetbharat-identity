using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class FormConfigurationService : IFormConfigurationService
{
    private static readonly Regex KeyNormalizerRegex = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    private readonly IFormConfigurationRepository _repository;
    private readonly ILogger<FormConfigurationService> _logger;

    public FormConfigurationService(
        IFormConfigurationRepository repository,
        ILogger<FormConfigurationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<FormPageResponseDto>> GetFormPagesAsync()
    {
        var pages = await _repository.GetActiveFormPagesAsync();
        return pages.Select(MapFormPage).ToList();
    }

    public async Task<List<FormFieldResponseDto>> GetFormFieldsAsync(string? pageKey)
    {
        var normalizedPageKey = NormalizeKey(pageKey, "pageKey");
        await EnsurePageExistsAsync(normalizedPageKey);

        var fields = await _repository.GetActiveFormFieldsByPageKeyAsync(normalizedPageKey);
        return fields.Select(MapFormField).ToList();
    }

    public async Task<FormFieldResponseDto> CreateFormFieldAsync(CreateFormFieldRequestDto request)
    {
        if (request == null)
            throw new BadHttpRequestException("Request payload is required.");

        var pageKey = NormalizeKey(request.PageKey, "pageKey");
        var fieldKey = NormalizeKey(request.FieldKey, "fieldKey");
        var fieldLabel = NormalizeLabel(request.FieldLabel, "fieldLabel", 150);
        var fieldType = NormalizeFieldType(request.FieldType);

        await EnsurePageExistsAsync(pageKey);

        if (await _repository.FormFieldKeyExistsAsync(pageKey, fieldKey))
            throw new InvalidOperationException("Field key already exists for the selected page.");

        var utcNow = DateTime.UtcNow;
        var entity = new FormField
        {
            PageKey = pageKey,
            FieldKey = fieldKey,
            FieldLabel = fieldLabel,
            FieldType = fieldType,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await _repository.AddFormFieldAsync(entity);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "Created form field {FieldKey} for page {PageKey} with id {FieldId}.",
            entity.FieldKey,
            entity.PageKey,
            entity.Id);

        return MapFormField(entity);
    }

    public async Task<FormConfigurationResponseDto> GetFormConfigurationAsync(int accountId, string? pageKey)
    {
        ValidateAccountId(accountId);

        var normalizedPageKey = NormalizeKey(pageKey, "pageKey");

        await EnsureAccessibleAccountAsync(accountId);
        await EnsurePageExistsAsync(normalizedPageKey);

        var fields = await _repository.GetActiveFormFieldsByPageKeyAsync(normalizedPageKey);
        var configurations = await _repository.GetConfigurationsByAccountAndPageAsync(accountId, normalizedPageKey);
        var configurationMap = configurations.ToDictionary(x => x.FieldId, x => x);

        return new FormConfigurationResponseDto
        {
            AccountId = accountId,
            PageKey = normalizedPageKey,
            Fields = fields
                .Select(field =>
                {
                    configurationMap.TryGetValue(field.Id, out var configuration);

                    return new FormConfigurationFieldResponseDto
                    {
                        FieldId = field.Id,
                        FieldKey = field.FieldKey,
                        FieldLabel = field.FieldLabel,
                        FieldType = field.FieldType,
                        Visible = configuration?.Visible ?? false,
                        Required = configuration?.Required ?? false
                    };
                })
                .ToList()
        };
    }

    public async Task SaveFormConfigurationAsync(SaveFormConfigurationRequestDto request)
    {
        if (request == null)
            throw new BadHttpRequestException("Request payload is required.");

        ValidateAccountId(request.AccountId);

        var normalizedPageKey = NormalizeKey(request.PageKey, "pageKey");

        if (request.Fields == null || request.Fields.Count == 0)
            throw new BadHttpRequestException("fields are required.");

        await EnsureAccessibleAccountAsync(request.AccountId);
        await EnsurePageExistsAsync(normalizedPageKey);

        var requestedFieldIds = request.Fields.Select(x => x.FieldId).ToList();
        if (requestedFieldIds.Any(x => x <= 0))
            throw new BadHttpRequestException("fieldId is invalid.");

        if (requestedFieldIds.Count != requestedFieldIds.Distinct().Count())
            throw new BadHttpRequestException("Duplicate fieldId values are not allowed.");

        var pageFields = await _repository.GetActiveFormFieldsByIdsAsync(normalizedPageKey, requestedFieldIds);
        if (pageFields.Count != requestedFieldIds.Count)
            throw new BadHttpRequestException("One or more fieldId values do not belong to the selected page.");

        var existingConfigurations =
            await _repository.GetConfigurationsByAccountAndPageAsync(request.AccountId, normalizedPageKey);

        var existingByFieldId = existingConfigurations.ToDictionary(x => x.FieldId, x => x);
        var utcNow = DateTime.UtcNow;
        var newConfigurations = new List<AccountFormConfiguration>();

        foreach (var field in request.Fields)
        {
            var normalizedRequired = field.Visible && field.Required;

            if (existingByFieldId.TryGetValue(field.FieldId, out var existingConfiguration))
            {
                existingConfiguration.Visible = field.Visible;
                existingConfiguration.Required = normalizedRequired;
                existingConfiguration.UpdatedAt = utcNow;
                continue;
            }

            newConfigurations.Add(new AccountFormConfiguration
            {
                AccountId = request.AccountId,
                PageKey = normalizedPageKey,
                FieldId = field.FieldId,
                Visible = field.Visible,
                Required = normalizedRequired,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
        }

        if (newConfigurations.Count > 0)
            await _repository.AddAccountFormConfigurationsAsync(newConfigurations);

        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "Saved form configuration for account {AccountId} and page {PageKey}. Field count: {FieldCount}.",
            request.AccountId,
            normalizedPageKey,
            request.Fields.Count);
    }

    private async Task EnsureAccessibleAccountAsync(int accountId)
    {
        var account = await _repository.GetAccessibleAccountByIdAsync(accountId);
        if (account == null)
            throw new KeyNotFoundException("Account not found");
    }

    private async Task EnsurePageExistsAsync(string pageKey)
    {
        var page = await _repository.GetActiveFormPageByKeyAsync(pageKey);
        if (page == null)
            throw new KeyNotFoundException("Form page not found");
    }

    private static void ValidateAccountId(int accountId)
    {
        if (accountId <= 0)
            throw new BadHttpRequestException("accountId is required.");
    }

    private static string NormalizeKey(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BadHttpRequestException($"{fieldName} is required.");

        var normalized = KeyNormalizerRegex
            .Replace(value.Trim().ToLowerInvariant(), "_")
            .Trim('_');

        if (string.IsNullOrWhiteSpace(normalized))
            throw new BadHttpRequestException($"{fieldName} is invalid.");

        if (normalized.Length > 100)
            throw new BadHttpRequestException($"{fieldName} cannot exceed 100 characters.");

        return normalized;
    }

    private static string NormalizeLabel(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BadHttpRequestException($"{fieldName} is required.");

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new BadHttpRequestException($"{fieldName} cannot exceed {maxLength} characters.");

        return normalized;
    }

    private static string NormalizeFieldType(string? fieldType)
    {
        if (string.IsNullOrWhiteSpace(fieldType))
            throw new BadHttpRequestException("fieldType is required.");

        var normalized = fieldType.Trim().ToLowerInvariant();
        if (normalized.Length > 50)
            throw new BadHttpRequestException("fieldType cannot exceed 50 characters.");

        return normalized;
    }

    private static FormPageResponseDto MapFormPage(FormPage page)
    {
        return new FormPageResponseDto
        {
            PageKey = page.PageKey,
            PageName = page.PageName
        };
    }

    private static FormFieldResponseDto MapFormField(FormField field)
    {
        return new FormFieldResponseDto
        {
            Id = field.Id,
            PageKey = field.PageKey,
            FieldKey = field.FieldKey,
            FieldLabel = field.FieldLabel,
            FieldType = field.FieldType,
            IsActive = field.IsActive
        };
    }
}
