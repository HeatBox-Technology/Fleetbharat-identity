using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for sensor management.
    /// Handles CRUD operations for sensors.
    /// </summary>
    public class SensorService : ISensorService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorService"/> class.
        /// </summary>
        public SensorService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all sensors.
        /// </summary>
        /// <returns>List of <see cref="SensorDto"/>.</returns>
        public async Task<IEnumerable<SensorDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Set<Domain.Entities.mst_sensor>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.Name ?? "").ToLower().Contains(s) ||
                    (x.SerialNo ?? "").ToLower().Contains(s) ||
                    (x.MakeModel ?? "").ToLower().Contains(s));
            }

            return await query
                .OrderByDescending(x => x.SensorId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SensorDto
                {
                    SensorId = x.SensorId,
                    AccountId = x.AccountId,
                    SensorTypeId = x.SensorTypeId,
                    Name = x.Name,
                    MakeModel = x.MakeModel,
                    SerialNo = x.SerialNo,
                    StatusKey = x.StatusKey,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        /// <summary>
        /// Gets a sensor by its unique identifier.
        /// </summary>
        /// <param name="id">Sensor ID.</param>
        /// <returns>The <see cref="SensorDto"/> if found; otherwise, null.</returns>
        public async Task<SensorDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.mst_sensor>().FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new sensor.
        /// </summary>
        /// <param name="dto">Sensor DTO.</param>
        /// <returns>The created <see cref="SensorDto"/>.</returns>
        public async Task<SensorDto> CreateAsync(SensorDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Set<Domain.Entities.mst_sensor>().Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing sensor.
        /// </summary>
        /// <param name="id">Sensor ID.</param>
        /// <param name="dto">Sensor DTO.</param>
        /// <returns>The updated <see cref="SensorDto"/>.</returns>
        public async Task<SensorDto> UpdateAsync(long id, SensorDto dto)
        {
            var entity = await _context.Set<Domain.Entities.mst_sensor>().FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.AccountId = dto.AccountId;
            entity.SensorTypeId = dto.SensorTypeId;
            entity.Name = dto.Name;
            entity.MakeModel = dto.MakeModel;
            entity.SerialNo = dto.SerialNo;
            entity.StatusKey = dto.StatusKey;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a sensor.
        /// </summary>
        /// <param name="id">Sensor ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.mst_sensor>().FindAsync(id);
            if (entity == null) return false;
            _context.Set<Domain.Entities.mst_sensor>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static SensorDto MapToDto(Domain.Entities.mst_sensor entity)
        {
            return new SensorDto
            {
                SensorId = entity.SensorId,
                AccountId = entity.AccountId,
                SensorTypeId = entity.SensorTypeId,
                Name = entity.Name,
                MakeModel = entity.MakeModel,
                SerialNo = entity.SerialNo,
                StatusKey = entity.StatusKey,
                CreatedAt = entity.CreatedAt
            };
        }

        private static Domain.Entities.mst_sensor MapToEntity(SensorDto dto, bool includeId)
        {
            var entity = new Domain.Entities.mst_sensor
            {
                AccountId = dto.AccountId,
                SensorTypeId = dto.SensorTypeId,
                Name = dto.Name,
                MakeModel = dto.MakeModel,
                SerialNo = dto.SerialNo,
                StatusKey = dto.StatusKey,
                CreatedAt = dto.CreatedAt == default ? System.DateTime.UtcNow : dto.CreatedAt
            };
            if (includeId) entity.SensorId = dto.SensorId;
            return entity;
        }
    }
}
