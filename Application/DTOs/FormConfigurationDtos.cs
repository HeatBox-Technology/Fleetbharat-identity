using System.Collections.Generic;

public class FormPageResponseDto
{
    public string PageKey { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
}

public class CreateFormFieldRequestDto
{
    public string? PageKey { get; set; }
    public string? FieldLabel { get; set; }
    public string? FieldKey { get; set; }
    public string? FieldType { get; set; }
}

public class FormFieldResponseDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class FormConfigurationFieldResponseDto
{
    public int FieldId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool Visible { get; set; }
    public bool Required { get; set; }
}

public class FormConfigurationResponseDto
{
    public int AccountId { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public List<FormConfigurationFieldResponseDto> Fields { get; set; } = new();
}

public class SaveFormConfigurationFieldRequestDto
{
    public int FieldId { get; set; }
    public bool Visible { get; set; }
    public bool Required { get; set; }
}

public class SaveFormConfigurationRequestDto
{
    public int AccountId { get; set; }
    public string? PageKey { get; set; }
    public List<SaveFormConfigurationFieldRequestDto>? Fields { get; set; }
}
