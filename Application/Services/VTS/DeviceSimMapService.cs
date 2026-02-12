using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for mapping devices to SIMs (historical mapping).
    /// Handles CRUD operations for device-SIM associations.
    /// </summary>
    public class DeviceSimMapService : IDeviceSimMapService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceSimMapService"/> class.
        /// </summary>
        public DeviceSimMapService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all device-SIM mappings.
        /// </summary>
        /// <returns>List of <see cref="DeviceSimMapDto"/>.</returns>
        public async Task<IEnumerable<DeviceSimMapDto>> GetAllAsync()
        {
            var entities = await _context.Set<Domain.Entities.map_device_sim>().ToListAsync();
            return entities.Select(MapToDto);
        }

        /// <summary>
        /// Gets a device-SIM mapping by its unique identifier.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>The <see cref="DeviceSimMapDto"/> if found; otherwise, null.</returns>
        public async Task<DeviceSimMapDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.map_device_sim>().FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new device-SIM mapping.
        /// </summary>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The created <see cref="DeviceSimMapDto"/>.</returns>
        public async Task<DeviceSimMapDto> CreateAsync(DeviceSimMapDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Set<Domain.Entities.map_device_sim>().Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing device-SIM mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The updated <see cref="DeviceSimMapDto"/>.</returns>
        public async Task<DeviceSimMapDto> UpdateAsync(long id, DeviceSimMapDto dto)
        {
            var entity = await _context.Set<Domain.Entities.map_device_sim>().FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.DeviceId = dto.DeviceId;
            entity.SimId = dto.SimId;
            entity.FromTs = dto.FromTs;
            entity.ToTs = dto.ToTs;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a device-SIM mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.map_device_sim>().FindAsync(id);
            if (entity == null) return false;
            _context.Set<Domain.Entities.map_device_sim>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static DeviceSimMapDto MapToDto(Domain.Entities.map_device_sim entity)
        {
            return new DeviceSimMapDto
            {
                DeviceSimId = entity.DeviceSimId,
                DeviceId = entity.DeviceId,
                SimId = entity.SimId,
                FromTs = entity.FromTs,
                ToTs = entity.ToTs
            };
        }

        private static Domain.Entities.map_device_sim MapToEntity(DeviceSimMapDto dto, bool includeId)
        {
            var entity = new Domain.Entities.map_device_sim
            {
                DeviceId = dto.DeviceId,
                SimId = dto.SimId,
                FromTs = dto.FromTs == default ? System.DateTime.UtcNow : dto.FromTs,
                ToTs = dto.ToTs
            };
            if (includeId) entity.DeviceSimId = dto.DeviceSimId;
            return entity;
        }
    }
}
