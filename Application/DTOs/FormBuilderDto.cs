using System;
using System.Collections.Generic;

public class CreateFormBuilderRequest
{
    public int? AccountId { get; set; }
    public int? FkFormId { get; set; }
    public string? FormTitle { get; set; }
    public string? FormCode { get; set; }
    public string? Description { get; set; }
    public string? RawData { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedByUser { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? AccountName { get; set; }
    public string? FormName { get; set; }
}

public class UpdateFormBuilderRequest
{
    public int? AccountId { get; set; }
    public int? FkFormId { get; set; }
    public string? FormTitle { get; set; }
    public string? FormCode { get; set; }
    public string? Description { get; set; }
    public string? RawData { get; set; }
    public bool IsActive { get; set; }
    public string UpdatedByUser { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? AccountName { get; set; }
    public string? FormName { get; set; }
}

public class DeleteFormBuilderRequest
{
    public string DeletedByUser { get; set; } = string.Empty;
}

public class FormBuilderListItemDto
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public int? FkFormId { get; set; }
    public string? FormTitle { get; set; }
    public string? FormCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? ProjectName { get; set; }
    public string? AccountName { get; set; }
    public string? FormName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class FormBuilderResponseDto : FormBuilderListItemDto
{
    public string? RawData { get; set; }
}

public class FormBuilderPagedResponseDto
{
    public List<FormBuilderListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
