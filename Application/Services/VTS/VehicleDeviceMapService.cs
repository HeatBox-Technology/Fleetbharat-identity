using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for mapping vehicles to devices (historical mapping).
    /// Handles CRUD operations for vehicle-device associations.
    /// </summary>
    public class VehicleDeviceMapService : IVehicleDeviceMapService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleDeviceMapService"/> class.
        /// </summary>
        public VehicleDeviceMapService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all vehicle-device mappings.
        /// </summary>
        /// <returns>List of <see cref="VehicleDeviceMapDto"/>.</returns>
        public async Task<IEnumerable<VehicleDeviceMapDto>> GetAllAsync()
        {
            var entities = await _context.Set<Domain.Entities.map_vehicle_device>().ToListAsync();
            return entities.Select(MapToDto);
        }

        /// <summary>
        /// Gets a vehicle-device mapping by its unique identifier.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>The <see cref="VehicleDeviceMapDto"/> if found; otherwise, null.</returns>
        public async Task<VehicleDeviceMapDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.map_vehicle_device>().FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new vehicle-device mapping.
        /// </summary>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The created <see cref="VehicleDeviceMapDto"/>.</returns>
        public async Task<VehicleDeviceMapDto> CreateAsync(VehicleDeviceMapDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Set<Domain.Entities.map_vehicle_device>().Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing vehicle-device mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The updated <see cref="VehicleDeviceMapDto"/>.</returns>
        public async Task<VehicleDeviceMapDto> UpdateAsync(long id, VehicleDeviceMapDto dto)
        {
            var entity = await _context.Set<Domain.Entities.map_vehicle_device>().FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.AccountId = dto.AccountId;
            entity.VehicleId = dto.VehicleId;
            entity.DeviceId = dto.DeviceId;
            entity.InstallPosition = dto.InstallPosition;
            entity.IsPrimary = dto.IsPrimary;
            entity.FromTs = dto.FromTs;
            entity.ToTs = dto.ToTs;
            entity.InstalledByUserId = dto.InstalledByUserId;
            entity.Remarks = dto.Remarks;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a vehicle-device mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.map_vehicle_device>().FindAsync(id);
            if (entity == null) return false;
            _context.Set<Domain.Entities.map_vehicle_device>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static VehicleDeviceMapDto MapToDto(Domain.Entities.map_vehicle_device entity)
        {
            return new VehicleDeviceMapDto
            {
                VehicleDeviceId = entity.VehicleDeviceId,
                AccountId = entity.AccountId,
                VehicleId = entity.VehicleId,
                DeviceId = entity.DeviceId,
                InstallPosition = entity.InstallPosition,
                IsPrimary = entity.IsPrimary,
                FromTs = entity.FromTs,
                ToTs = entity.ToTs,
                InstalledByUserId = entity.InstalledByUserId,
                Remarks = entity.Remarks
            };
        }

        private static Domain.Entities.map_vehicle_device MapToEntity(VehicleDeviceMapDto dto, bool includeId)
        {
            var entity = new Domain.Entities.map_vehicle_device
            {
                AccountId = dto.AccountId,
                VehicleId = dto.VehicleId,
                DeviceId = dto.DeviceId,
                InstallPosition = dto.InstallPosition,
                IsPrimary = dto.IsPrimary,
                FromTs = dto.FromTs == default ? System.DateTime.UtcNow : dto.FromTs,
                ToTs = dto.ToTs,
                InstalledByUserId = dto.InstalledByUserId,
                Remarks = dto.Remarks
            };
            if (includeId) entity.VehicleDeviceId = dto.VehicleDeviceId;
            return entity;
        }
    }
}
