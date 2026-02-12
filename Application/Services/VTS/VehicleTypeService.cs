using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for vehicle type management.
    /// Handles CRUD operations for vehicle types.
    /// </summary>
    public class VehicleTypeService : IVehicleTypeService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleTypeService"/> class.
        /// </summary>
        public VehicleTypeService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all vehicle types.
        /// </summary>
        /// <returns>List of <see cref="VehicleTypeDto"/>.</returns>
        public async Task<IEnumerable<VehicleTypeDto>> GetAllAsync()
        {
            var entities = await _context.Set<Domain.Entities.mst_vehicle_type>().ToListAsync();
            return entities.Select(MapToDto);
        }

        /// <summary>
        /// Gets a vehicle type by its unique identifier.
        /// </summary>
        /// <param name="id">Vehicle type ID.</param>
        /// <returns>The <see cref="VehicleTypeDto"/> if found; otherwise, null.</returns>
        public async Task<VehicleTypeDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Set<Domain.Entities.mst_vehicle_type>().FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new vehicle type.
        /// </summary>
        /// <param name="dto">Vehicle type DTO.</param>
        /// <returns>The created <see cref="VehicleTypeDto"/>.</returns>
        public async Task<VehicleTypeDto> CreateAsync(VehicleTypeDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Set<Domain.Entities.mst_vehicle_type>().Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing vehicle type.
        /// </summary>
        /// <param name="id">Vehicle type ID.</param>
        /// <param name="dto">Vehicle type DTO.</param>
        /// <returns>The updated <see cref="VehicleTypeDto"/>.</returns>
        public async Task<VehicleTypeDto> UpdateAsync(int id, VehicleTypeDto dto)
        {
            var entity = await _context.Set<Domain.Entities.mst_vehicle_type>().FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.VehicleTypeName = dto.VehicleTypeName;
            entity.Category = dto.Category;
            entity.DefaultVehicleIcon = dto.DefaultVehicleIcon;
            entity.DefaultAlarmIcon = dto.DefaultAlarmIcon;
            entity.DefaultIconColor = dto.DefaultIconColor;
            entity.SeatingCapacity = dto.SeatingCapacity;
            entity.WheelsCount = dto.WheelsCount;
            entity.FuelCategory = dto.FuelCategory;
            entity.TankCapacity = dto.TankCapacity;
            entity.DefaultSpeedLimit = dto.DefaultSpeedLimit;
            entity.DefaultIdleThreshold = dto.DefaultIdleThreshold;
            entity.Status = dto.Status;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a vehicle type.
        /// </summary>
        /// <param name="id">Vehicle type ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Set<Domain.Entities.mst_vehicle_type>().FindAsync(id);
            if (entity == null) return false;
            _context.Set<Domain.Entities.mst_vehicle_type>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static VehicleTypeDto MapToDto(Domain.Entities.mst_vehicle_type entity)
        {
            return new VehicleTypeDto
            {
                Id = entity.Id,
                VehicleTypeName = entity.VehicleTypeName,
                Category = entity.Category,
                DefaultVehicleIcon = entity.DefaultVehicleIcon,
                DefaultAlarmIcon = entity.DefaultAlarmIcon,
                DefaultIconColor = entity.DefaultIconColor,
                SeatingCapacity = entity.SeatingCapacity,
                WheelsCount = entity.WheelsCount,
                FuelCategory = entity.FuelCategory,
                TankCapacity = entity.TankCapacity,
                DefaultSpeedLimit = entity.DefaultSpeedLimit,
                DefaultIdleThreshold = entity.DefaultIdleThreshold,
                Status = entity.Status
            };
        }

        private static Domain.Entities.mst_vehicle_type MapToEntity(VehicleTypeDto dto)
        {
            return MapToEntity(dto, true);
        }

        private static Domain.Entities.mst_vehicle_type MapToEntity(VehicleTypeDto dto, bool includeId)
        {
            var entity = new Domain.Entities.mst_vehicle_type
            {
                VehicleTypeName = dto.VehicleTypeName,
                Category = dto.Category,
                DefaultVehicleIcon = dto.DefaultVehicleIcon,
                DefaultAlarmIcon = dto.DefaultAlarmIcon,
                DefaultIconColor = dto.DefaultIconColor,
                SeatingCapacity = dto.SeatingCapacity,
                WheelsCount = dto.WheelsCount,
                FuelCategory = dto.FuelCategory,
                TankCapacity = dto.TankCapacity,
                DefaultSpeedLimit = dto.DefaultSpeedLimit,
                DefaultIdleThreshold = dto.DefaultIdleThreshold,
                Status = dto.Status
            };
            if (includeId) entity.Id = dto.Id;
            return entity;
        }
    }
}
