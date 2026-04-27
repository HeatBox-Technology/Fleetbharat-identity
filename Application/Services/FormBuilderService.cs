using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class FormBuilderService : IFormBuilderService
{
    private readonly IdentityDbContext _db;

    public FormBuilderService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateFormBuilderRequest request)
    {
        ValidateCreate(request, out var createdByUser);

        var formCode = NormalizeFormCode(request.FormCode, request.FormTitle!);
        await EnsureUniqueFormCodeAsync(formCode, request.AccountId, request.FkFormId, null);

        var entity = new FormBuilder
        {
            AccountId = request.AccountId,
            FkFormId = request.FkFormId,
            FormTitle = request.FormTitle!.Trim(),
            FormCode = formCode,
            Description = request.Description?.Trim(),
            RawData = request.RawData!.Trim(),
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedByUser = createdByUser,
            CreatedDate = DateTime.UtcNow,
            ProjectName = request.ProjectName?.Trim(),
            AccountName = request.AccountName?.Trim(),
            FormName = request.FormName?.Trim()
        };

        _db.FormBuilders.Add(entity);
        await _db.SaveChangesAsync();
        return entity.pk_form_builder_id;
    }

    public async Task<bool> UpdateAsync(int id, UpdateFormBuilderRequest request)
    {
        var entity = await _db.FormBuilders
            .FirstOrDefaultAsync(x => x.pk_form_builder_id == id && !x.IsDeleted);

        if (entity == null)
            return false;

        ValidateUpdate(request, out var updatedByUser);

        var formCode = NormalizeFormCode(request.FormCode, request.FormTitle!);
        await EnsureUniqueFormCodeAsync(formCode, request.AccountId, request.FkFormId, id);

        entity.AccountId = request.AccountId;
        entity.FkFormId = request.FkFormId;
        entity.FormTitle = request.FormTitle!.Trim();
        entity.FormCode = formCode;
        entity.Description = request.Description?.Trim();
        entity.RawData = request.RawData!.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedByUser = updatedByUser;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.ProjectName = request.ProjectName?.Trim();
        entity.AccountName = request.AccountName?.Trim();
        entity.FormName = request.FormName?.Trim();

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<FormBuilderPagedResponseDto> GetAllAsync(
        int? accountId,
        int? fkFormId,
        string? search,
        int pageNumber,
        int pageSize)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.FormBuilders
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (fkFormId.HasValue)
            query = query.Where(x => x.FkFormId == fkFormId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.FormTitle != null && x.FormTitle.ToLower().Contains(s)) ||
                (x.FormCode != null && x.FormCode.ToLower().Contains(s)) ||
                (x.FormName != null && x.FormName.ToLower().Contains(s)) ||
                (x.AccountName != null && x.AccountName.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FormBuilderListItemDto
            {
                Id = x.pk_form_builder_id,
                AccountId = x.AccountId,
                FkFormId = x.FkFormId,
                FormTitle = x.FormTitle,
                FormCode = x.FormCode,
                Description = x.Description,
                IsActive = x.IsActive,
                ProjectName = x.ProjectName,
                AccountName = x.AccountName,
                FormName = x.FormName,
                CreatedDate = x.CreatedDate
            })
            .ToListAsync();

        return new FormBuilderPagedResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<FormBuilderResponseDto?> GetByIdAsync(int id)
    {
        var entity = await _db.FormBuilders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.pk_form_builder_id == id && !x.IsDeleted);

        return entity == null ? null : MapResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, DeleteFormBuilderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeletedByUser) ||
            !Guid.TryParse(request.DeletedByUser, out var deletedByUser))
        {
            throw new ArgumentException("deletedByUser is required and must be a valid GUID.");
        }

        var entity = await _db.FormBuilders
            .FirstOrDefaultAsync(x => x.pk_form_builder_id == id && !x.IsDeleted);

        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedByUser = deletedByUser;
        entity.DeletedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<FormBuilderResponseDto?> GetByAccountAndFormAsync(int accountId, int fkFormId)
    {
        var entity = await _db.FormBuilders
            .AsNoTracking()
            .Where(x =>
                x.AccountId == accountId &&
                x.FkFormId == fkFormId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
            .FirstOrDefaultAsync();

        return entity == null ? null : MapResponse(entity);
    }

    private static void ValidateCreate(CreateFormBuilderRequest request, out Guid createdByUser)
    {
        ValidateRequiredFields(request.FormTitle, request.RawData);

        if (string.IsNullOrWhiteSpace(request.CreatedByUser) ||
            !Guid.TryParse(request.CreatedByUser, out createdByUser))
        {
            throw new ArgumentException("createdByUser is required and must be a valid GUID.");
        }
    }

    private static void ValidateUpdate(UpdateFormBuilderRequest request, out Guid updatedByUser)
    {
        ValidateRequiredFields(request.FormTitle, request.RawData);

        if (string.IsNullOrWhiteSpace(request.UpdatedByUser) ||
            !Guid.TryParse(request.UpdatedByUser, out updatedByUser))
        {
            throw new ArgumentException("updatedByUser is required and must be a valid GUID.");
        }
    }

    private static void ValidateRequiredFields(string? formTitle, string? rawData)
    {
        if (string.IsNullOrWhiteSpace(formTitle))
            throw new ArgumentException("formTitle is required.");

        if (string.IsNullOrWhiteSpace(rawData))
            throw new ArgumentException("rawData is required.");
    }

    private async Task EnsureUniqueFormCodeAsync(
        string formCode,
        int? accountId,
        int? fkFormId,
        int? excludeId)
    {
        var exists = await _db.FormBuilders
            .AsNoTracking()
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.FormCode == formCode &&
                x.AccountId == accountId &&
                x.FkFormId == fkFormId &&
                (!excludeId.HasValue || x.pk_form_builder_id != excludeId.Value));

        if (exists)
            throw new InvalidOperationException("formCode already exists for this account and form.");
    }

    private static string NormalizeFormCode(string? formCode, string formTitle)
    {
        var source = string.IsNullOrWhiteSpace(formCode) ? formTitle : formCode;
        var normalized = Regex.Replace(source.Trim().ToLower(), @"[^a-z0-9]+", "_");
        normalized = Regex.Replace(normalized, "_+", "_").Trim('_');
        return string.IsNullOrWhiteSpace(normalized) ? $"form_{Guid.NewGuid():N}" : normalized;
    }

    private static FormBuilderResponseDto MapResponse(FormBuilder entity) =>
        new()
        {
            Id = entity.pk_form_builder_id,
            AccountId = entity.AccountId,
            FkFormId = entity.FkFormId,
            FormTitle = entity.FormTitle,
            FormCode = entity.FormCode,
            Description = entity.Description,
            RawData = entity.RawData,
            IsActive = entity.IsActive,
            ProjectName = entity.ProjectName,
            AccountName = entity.AccountName,
            FormName = entity.FormName,
            CreatedDate = entity.CreatedDate
        };
}
