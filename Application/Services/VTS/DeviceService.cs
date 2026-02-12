using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for device management.
    /// Handles CRUD operations for devices.
    /// </summary>
    public class DeviceService : IDeviceService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceService"/> class.
        /// </summary>
        public DeviceService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all devices.
        /// </summary>
        /// <returns>List of <see cref="DeviceDto"/>.</returns>
        public async Task<IEnumerable<DeviceDto>> GetAllAsync()
        {
            var entities = await _context.Devices.ToListAsync();
            return entities.Select(MapToDto);
        }

        /// <summary>
        /// Gets a device by its unique identifier.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <returns>The <see cref="DeviceDto"/> if found; otherwise, null.</returns>
        public async Task<DeviceDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Devices.FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new device.
        /// </summary>
        /// <param name="dto">Device DTO.</param>
        /// <returns>The created <see cref="DeviceDto"/>.</returns>
        public async Task<DeviceDto> CreateAsync(DeviceDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Devices.Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing device.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <param name="dto">Device DTO.</param>
        /// <returns>The updated <see cref="DeviceDto"/>.</returns>
        public async Task<DeviceDto> UpdateAsync(int id, DeviceDto dto)
        {
            var entity = await _context.Devices.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.AccountId = dto.AccountId;
            entity.DeviceImeiOrSerial = dto.DeviceImeiOrSerial;
            entity.DeviceTypeId = dto.DeviceTypeId;
            entity.FirmwareVersion = dto.FirmwareVersion;
            entity.SimMobile = dto.SimMobile;
            entity.SimIccid = dto.SimIccid;
            entity.NetworkProviderId = dto.NetworkProviderId;
            entity.DeviceStatus = dto.DeviceStatus;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a device.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Devices.FindAsync(id);
            if (entity == null) return false;
            _context.Devices.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static DeviceDto MapToDto(Domain.Entities.mst_device entity)
        {
            return new DeviceDto
            {
                Id = entity.Id,
                AccountId = entity.AccountId,
                DeviceImeiOrSerial = entity.DeviceImeiOrSerial,
                DeviceTypeId = entity.DeviceTypeId,
                FirmwareVersion = entity.FirmwareVersion,
                SimMobile = entity.SimMobile,
                SimIccid = entity.SimIccid,
                NetworkProviderId = entity.NetworkProviderId,
                DeviceStatus = entity.DeviceStatus
            };
        }

        private static Domain.Entities.mst_device MapToEntity(DeviceDto dto, bool includeId)
        {
            var entity = new Domain.Entities.mst_device
            {
                AccountId = dto.AccountId,
                DeviceImeiOrSerial = dto.DeviceImeiOrSerial,
                DeviceTypeId = dto.DeviceTypeId,
                FirmwareVersion = dto.FirmwareVersion,
                SimMobile = dto.SimMobile,
                SimIccid = dto.SimIccid,
                NetworkProviderId = dto.NetworkProviderId,
                DeviceStatus = dto.DeviceStatus
            };
            if (includeId) entity.Id = dto.Id;
            return entity;
        }
        /// <summary>
        /// Bulk create devices.
        /// </summary>
        /// <param name="devices">List of devices to create.</param>
        /// <returns>List of created devices.</returns>
        public async Task<IEnumerable<DeviceDto>> BulkCreateAsync(IEnumerable<DeviceDto> devices)
        {
            var entities = devices.Select(dto => MapToEntity(dto, false)).ToList();
            _context.Devices.AddRange(entities);
            await _context.SaveChangesAsync();
            return entities.Select(MapToDto).ToList();
        }

        public async Task<PagedResultDto<DeviceDto>> GetPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            var query = _context.Devices.AsQueryable();
            var totalCount = await query.CountAsync();
            var data = await query
                .OrderByDescending(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResultDto<DeviceDto>
            {
                Items = data.Select(MapToDto).ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
