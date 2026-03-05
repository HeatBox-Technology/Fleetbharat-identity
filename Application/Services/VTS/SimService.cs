using Application.DTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class SimService : ISimService
{
    private readonly IdentityDbContext _db;

    public SimService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateSimDto dto)
    {
        // Account validation
        var accountExists = await _db.Accounts
            .AnyAsync(x => x.AccountId == dto.AccountId);

        if (!accountExists)
            throw new Exception("Invalid AccountId");

        // ICCID uniqueness
        var exists = await _db.Sims
            .AnyAsync(x => x.Iccid == dto.Iccid && !x.IsDeleted);

        if (exists)
            throw new Exception("ICCID already exists");

        var entity = new mst_sim
        {
            AccountId = dto.AccountId,
            Iccid = dto.Iccid,
            Msisdn = dto.Msisdn,
            Imsi = dto.Imsi,
            NetworkProviderId = dto.NetworkProviderId,
            StatusKey = dto.StatusKey,
            ActivatedAt = dto.ActivatedAt,
            ExpiryAt = dto.ExpiryAt,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        _db.Sims.Add(entity);
        await _db.SaveChangesAsync();

        return entity.SimId;
    }

    public async Task<bool> UpdateAsync(int id, UpdateSimDto dto)
    {
        var entity = await _db.Sims
            .FirstOrDefaultAsync(x => x.SimId == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("SIM not found");

        entity.Iccid = dto.Iccid;
        entity.Msisdn = dto.Msisdn;
        entity.Imsi = dto.Imsi;
        entity.NetworkProviderId = dto.NetworkProviderId;
        entity.StatusKey = dto.StatusKey;
        entity.ActivatedAt = dto.ActivatedAt;
        entity.ExpiryAt = dto.ExpiryAt;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isActive)
    {
        var entity = await _db.Sims
            .FirstOrDefaultAsync(x => x.SimId == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("SIM not found");

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Sims
            .FirstOrDefaultAsync(x => x.SimId == id && !x.IsDeleted);

        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<SimDto?> GetByIdAsync(int id)
    {
        return await _db.Sims
            .Where(x => x.SimId == id && !x.IsDeleted)
            .Select(x => MapToDto(x))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<SimDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.Sims
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Iccid ?? "").ToLower().Contains(s) ||
                (x.Msisdn ?? "").ToLower().Contains(s) ||
                (x.Imsi ?? "").ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(x => x.SimId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync();
    }

    public async Task<PagedResultDto<SimDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.Sims
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Iccid ?? "").ToLower().Contains(s) ||
                (x.Msisdn ?? "").ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.SimId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync();

        return new PagedResultDto<SimDto>
        {
            Items = items,
            TotalRecords = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SimListUiResponseDto> GetSims(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        var query = _db.Sims.Where(x => !x.IsDeleted);

        var total = await query.CountAsync();
        var active = await query.CountAsync(x => x.IsActive);

        var summary = new SimSummaryDto
        {
            TotalSims = total,
            Active = active,
            Inactive = total - active,
            Expired = await query.CountAsync(x =>
                x.ExpiryAt != null && x.ExpiryAt < DateTime.UtcNow)
        };

        var paged = await GetPagedAsync(page, pageSize, accountId, search);

        return new SimListUiResponseDto
        {
            Summary = summary,
            Sims = paged
        };
    }

    public async Task<List<SimDto>> BulkCreateAsync(List<CreateSimDto> sims)
    {
        var entities = sims.Select(dto => new mst_sim
        {
            AccountId = dto.AccountId,
            Iccid = dto.Iccid,
            Msisdn = dto.Msisdn,
            Imsi = dto.Imsi,
            NetworkProviderId = dto.NetworkProviderId,
            StatusKey = dto.StatusKey,
            ActivatedAt = dto.ActivatedAt,
            ExpiryAt = dto.ExpiryAt,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        }).ToList();

        _db.Sims.AddRange(entities);
        await _db.SaveChangesAsync();

        return entities.Select(MapToDto).ToList();
    }

    private static SimDto MapToDto(mst_sim x)
    {
        return new SimDto
        {
            SimId = x.SimId,
            AccountId = x.AccountId,
            Iccid = x.Iccid,
            Msisdn = x.Msisdn,
            Imsi = x.Imsi,
            NetworkProviderId = x.NetworkProviderId,
            StatusKey = x.StatusKey,
            ActivatedAt = x.ActivatedAt,
            ExpiryAt = x.ExpiryAt,
            CreatedAt = x.CreatedAt,
            CreatedBy = x.CreatedBy,
            UpdatedAt = x.UpdatedAt,
            UpdatedBy = x.UpdatedBy,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted
        };
    }
}
