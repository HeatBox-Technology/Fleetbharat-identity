using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for device type management.
    /// Handles CRUD operations for device types.
    /// </summary>
    public class DeviceTypeService : IDeviceTypeService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceTypeService"/> class.
        /// </summary>
        public DeviceTypeService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all device types.
        /// </summary>
        /// <returns>List of <see cref="DeviceTypeDto"/>.</returns>
        public async Task<IEnumerable<DeviceTypeDto>> GetAllAsync()
        {
            var entities = await _context.DeviceTypes.ToListAsync();
            return entities.Select(MapToDto);
        }

        /// <summary>
        /// Gets a device type by its unique identifier.
        /// </summary>
        /// <param name="id">Device type ID.</param>
        /// <returns>The <see cref="DeviceTypeDto"/> if found; otherwise, null.</returns>
        public async Task<DeviceTypeDto?> GetByIdAsync(int id)
        {
            var entity = await _context.DeviceTypes.FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new device type.
        /// </summary>
        /// <param name="dto">Device type DTO.</param>
        /// <returns>The created <see cref="DeviceTypeDto"/>.</returns>
        public async Task<DeviceTypeDto> CreateAsync(DeviceTypeDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.DeviceTypes.Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing device type.
        /// </summary>
        /// <param name="id">Device type ID.</param>
        /// <param name="dto">Device type DTO.</param>
        /// <returns>The updated <see cref="DeviceTypeDto"/>.</returns>
        public async Task<DeviceTypeDto> UpdateAsync(int id, DeviceTypeDto dto)
        {
            var entity = await _context.DeviceTypes.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.DeviceType = dto.DeviceType;
            entity.OemManufacturerId = dto.OemManufacturerId;
            entity.DeviceCategoryId = dto.DeviceCategoryId;
            entity.InputCount = dto.InputCount;
            entity.OutputCount = dto.OutputCount;
            entity.SupportedIOs = dto.SupportedIOs != null ? string.Join(",", dto.SupportedIOs) : null;
            entity.SupportedFeatures = dto.SupportedFeatures != null ? string.Join(",", dto.SupportedFeatures) : null;
            entity.Status = dto.Status;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a device type.
        /// </summary>
        /// <param name="id">Device type ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.DeviceTypes.FindAsync(id);
            if (entity == null) return false;
            _context.DeviceTypes.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static DeviceTypeDto MapToDto(Domain.Entities.mst_device_type entity)
        {
            return new DeviceTypeDto
            {
                Id = entity.Id,
                DeviceType = entity.DeviceType,
                OemManufacturerId = entity.OemManufacturerId,
                DeviceCategoryId = entity.DeviceCategoryId,
                InputCount = entity.InputCount,
                OutputCount = entity.OutputCount,
                SupportedIOs = string.IsNullOrEmpty(entity.SupportedIOs) ? null : (entity.SupportedIOs.Split(',')?.ToList() ?? new List<string>()),
                SupportedFeatures = string.IsNullOrEmpty(entity.SupportedFeatures) ? null : (entity.SupportedFeatures.Split(',')?.ToList() ?? new List<string>()),
                Status = entity.Status
            };
        }

        private static Domain.Entities.mst_device_type MapToEntity(DeviceTypeDto dto, bool includeId)
        {
            var entity = new Domain.Entities.mst_device_type
            {
                DeviceType = dto.DeviceType,
                OemManufacturerId = dto.OemManufacturerId,
                DeviceCategoryId = dto.DeviceCategoryId,
                InputCount = dto.InputCount,
                OutputCount = dto.OutputCount,
                SupportedIOs = dto.SupportedIOs != null ? string.Join(",", dto.SupportedIOs) : null,
                SupportedFeatures = dto.SupportedFeatures != null ? string.Join(",", dto.SupportedFeatures) : null,
                Status = dto.Status
            };
            if (includeId) entity.Id = dto.Id;
            return entity;
        }
    }
}
