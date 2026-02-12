using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for SIM management.
    /// Handles CRUD operations for SIM cards.
    /// </summary>
    public class SimService : ISimService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimService"/> class.
        /// </summary>
        public SimService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all SIMs.
        /// </summary>
        /// <returns>List of <see cref="SimDto"/>.</returns>
        public async Task<IEnumerable<SimDto>> GetAllAsync()
        {
            var entities = await _context.Sims.ToListAsync();
            return entities.Select(MapToDto);
        }

        /// <summary>
        /// Gets a SIM by its unique identifier.
        /// </summary>
        /// <param name="id">SIM ID.</param>
        /// <returns>The <see cref="SimDto"/> if found; otherwise, null.</returns>
        public async Task<SimDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Sims.FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new SIM.
        /// </summary>
        /// <param name="dto">SIM DTO.</param>
        /// <returns>The created <see cref="SimDto"/>.</returns>
        public async Task<SimDto> CreateAsync(SimDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Sims.Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing SIM.
        /// </summary>
        /// <param name="id">SIM ID.</param>
        /// <param name="dto">SIM DTO.</param>
        /// <returns>The updated <see cref="SimDto"/>.</returns>
        public async Task<SimDto> UpdateAsync(long id, SimDto dto)
        {
            var entity = await _context.Sims.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.AccountId = dto.AccountId;
            entity.Iccid = dto.Iccid;
            entity.Msisdn = dto.Msisdn;
            entity.Imsi = dto.Imsi;
            entity.NetworkProviderId = dto.NetworkProviderId;
            entity.StatusKey = dto.StatusKey;
            entity.ActivatedAt = dto.ActivatedAt;
            entity.ExpiryAt = dto.ExpiryAt;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a SIM.
        /// </summary>
        /// <param name="id">SIM ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.Sims.FindAsync(id);
            if (entity == null) return false;
            _context.Sims.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Gets a paged list of SIMs.
        /// </summary>
        /// <param name="page">Page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>A <see cref="PagedResultDto{SimDto}"/> containing the paged SIMs.</returns>
        public async Task<PagedResultDto<SimDto>> GetPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            var query = _context.Sims.AsQueryable();
            var totalCount = await query.CountAsync();
            var data = await query
                .OrderByDescending(s => s.SimId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResultDto<SimDto>
            {
                Items = data.Select(MapToDto).ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private static SimDto MapToDto(Domain.Entities.mst_sim entity)
        {
            return new SimDto
            {
                SimId = entity.SimId,
                AccountId = entity.AccountId,
                Iccid = entity.Iccid,
                Msisdn = entity.Msisdn,
                Imsi = entity.Imsi,
                NetworkProviderId = entity.NetworkProviderId,
                StatusKey = entity.StatusKey,
                ActivatedAt = entity.ActivatedAt,
                ExpiryAt = entity.ExpiryAt,
                CreatedAt = entity.CreatedAt
            };
        }

        private static Domain.Entities.mst_sim MapToEntity(SimDto dto, bool includeId)
        {
            var entity = new Domain.Entities.mst_sim
            {
                AccountId = dto.AccountId,
                Iccid = dto.Iccid,
                Msisdn = dto.Msisdn,
                Imsi = dto.Imsi,
                NetworkProviderId = dto.NetworkProviderId,
                StatusKey = dto.StatusKey,
                ActivatedAt = dto.ActivatedAt,
                ExpiryAt = dto.ExpiryAt,
                CreatedAt = dto.CreatedAt == default ? System.DateTime.UtcNow : dto.CreatedAt
            };
            if (includeId) entity.SimId = dto.SimId;
            return entity;
        }
        /// <summary>
        /// Bulk create SIMs.
        /// </summary>
        /// <param name="sims">List of SIMs to create.</param>
        /// <returns>List of created SIMs.</returns>
        public async Task<IEnumerable<SimDto>> BulkCreateAsync(IEnumerable<SimDto> sims)
        {
            var entities = sims.Select(dto => MapToEntity(dto, false)).ToList();
            _context.Sims.AddRange(entities);
            await _context.SaveChangesAsync();
            return entities.Select(MapToDto).ToList();
        }
    }
}
