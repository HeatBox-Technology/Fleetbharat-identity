using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for mapping vehicles to sensors (mount points).
    /// Handles CRUD operations for vehicle-sensor associations.
    /// </summary>
    public class VehicleSensorMapService : IVehicleSensorMapService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleSensorMapService"/> class.
        /// </summary>
        public VehicleSensorMapService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all vehicle-sensor mappings.
        /// </summary>
        /// <returns>List of <see cref="VehicleSensorMapDto"/>.</returns>
        public async Task<IEnumerable<VehicleSensorMapDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Set<Domain.Entities.map_vehicle_sensor>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x =>
                    x.VehicleId.ToString().ToLower().Contains(s) ||
                    x.SensorId.ToString().ToLower().Contains(s) ||
                    (x.MountPoint ?? "").ToLower().Contains(s));
            }

            return await query
                .OrderByDescending(x => x.VehicleSensorId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new VehicleSensorMapDto
                {
                    VehicleSensorId = x.VehicleSensorId,
                    VehicleId = x.VehicleId,
                    SensorId = x.SensorId,
                    MountPoint = x.MountPoint,
                    FromTs = x.FromTs,
                    ToTs = x.ToTs
                })
                .ToListAsync();
        }

        /// <summary>
        /// Gets a vehicle-sensor mapping by its unique identifier.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>The <see cref="VehicleSensorMapDto"/> if found; otherwise, null.</returns>
        public async Task<VehicleSensorMapDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.map_vehicle_sensor>().FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new vehicle-sensor mapping.
        /// </summary>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The created <see cref="VehicleSensorMapDto"/>.</returns>
        public async Task<VehicleSensorMapDto> CreateAsync(VehicleSensorMapDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Set<Domain.Entities.map_vehicle_sensor>().Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing vehicle-sensor mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The updated <see cref="VehicleSensorMapDto"/>.</returns>
        public async Task<VehicleSensorMapDto> UpdateAsync(long id, VehicleSensorMapDto dto)
        {
            var entity = await _context.Set<Domain.Entities.map_vehicle_sensor>().FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.VehicleId = dto.VehicleId;
            entity.SensorId = dto.SensorId;
            entity.MountPoint = dto.MountPoint;
            entity.FromTs = dto.FromTs;
            entity.ToTs = dto.ToTs;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a vehicle-sensor mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.map_vehicle_sensor>().FindAsync(id);
            if (entity == null) return false;
            _context.Set<Domain.Entities.map_vehicle_sensor>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static VehicleSensorMapDto MapToDto(Domain.Entities.map_vehicle_sensor entity)
        {
            return new VehicleSensorMapDto
            {
                VehicleSensorId = entity.VehicleSensorId,
                VehicleId = entity.VehicleId,
                SensorId = entity.SensorId,
                MountPoint = entity.MountPoint,
                FromTs = entity.FromTs,
                ToTs = entity.ToTs
            };
        }

        private static Domain.Entities.map_vehicle_sensor MapToEntity(VehicleSensorMapDto dto, bool includeId)
        {
            var entity = new Domain.Entities.map_vehicle_sensor
            {
                VehicleId = dto.VehicleId,
                SensorId = dto.SensorId,
                MountPoint = dto.MountPoint,
                FromTs = dto.FromTs == default ? System.DateTime.UtcNow : dto.FromTs,
                ToTs = dto.ToTs
            };
            if (includeId) entity.VehicleSensorId = dto.VehicleSensorId;
            return entity;
        }
    }
}
