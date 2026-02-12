using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for sensor type management.
    /// Handles CRUD operations for sensor types.
    /// </summary>
    public class SensorTypeService : ISensorTypeService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorTypeService"/> class.
        /// </summary>
        public SensorTypeService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all sensor types.
        /// </summary>
        /// <returns>List of <see cref="SensorTypeDto"/>.</returns>
        public async Task<IEnumerable<SensorTypeDto>> GetAllAsync()
        {
            var entities = await _context.Set<Domain.Entities.lkp_sensor_type>().ToListAsync();
            return entities.Select(MapToDto);
        }

        /// <summary>
        /// Gets a sensor type by its unique identifier.
        /// </summary>
        /// <param name="id">Sensor type ID.</param>
        /// <returns>The <see cref="SensorTypeDto"/> if found; otherwise, null.</returns>
        public async Task<SensorTypeDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.lkp_sensor_type>().FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new sensor type.
        /// </summary>
        /// <param name="dto">Sensor type DTO.</param>
        /// <returns>The created <see cref="SensorTypeDto"/>.</returns>
        public async Task<SensorTypeDto> CreateAsync(SensorTypeDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Set<Domain.Entities.lkp_sensor_type>().Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing sensor type.
        /// </summary>
        /// <param name="id">Sensor type ID.</param>
        /// <param name="dto">Sensor type DTO.</param>
        /// <returns>The updated <see cref="SensorTypeDto"/>.</returns>
        public async Task<SensorTypeDto> UpdateAsync(long id, SensorTypeDto dto)
        {
            var entity = await _context.Set<Domain.Entities.lkp_sensor_type>().FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.Code = dto.Code;
            entity.Name = dto.Name;
            entity.Unit = dto.Unit;
            entity.ValueKind = dto.ValueKind;
            entity.MinValue = dto.MinValue;
            entity.MaxValue = dto.MaxValue;
            entity.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a sensor type.
        /// </summary>
        /// <param name="id">Sensor type ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.lkp_sensor_type>().FindAsync(id);
            if (entity == null) return false;
            _context.Set<Domain.Entities.lkp_sensor_type>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static SensorTypeDto MapToDto(Domain.Entities.lkp_sensor_type entity)
        {
            return new SensorTypeDto
            {
                SensorTypeId = entity.SensorTypeId,
                Code = entity.Code,
                Name = entity.Name,
                Unit = entity.Unit,
                ValueKind = entity.ValueKind,
                MinValue = entity.MinValue,
                MaxValue = entity.MaxValue,
                IsActive = entity.IsActive
            };
        }

        private static Domain.Entities.lkp_sensor_type MapToEntity(SensorTypeDto dto, bool includeId)
        {
            var entity = new Domain.Entities.lkp_sensor_type
            {
                Code = dto.Code,
                Name = dto.Name,
                Unit = dto.Unit,
                ValueKind = dto.ValueKind,
                MinValue = dto.MinValue,
                MaxValue = dto.MaxValue,
                IsActive = dto.IsActive
            };
            if (includeId) entity.SensorTypeId = dto.SensorTypeId;
            return entity;
        }
    }
}
