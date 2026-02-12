using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for vehicle management.
    /// Handles CRUD operations for vehicles.
    /// </summary>
    public class VehicleService : IVehicleService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleService"/> class.
        /// </summary>
        public VehicleService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all vehicles.
        /// </summary>
        /// <returns>List of <see cref="VehicleDto"/>.</returns>
        public async Task<IEnumerable<VehicleDto>> GetAllAsync()
        {
            var entities = await _context.Set<Domain.Entities.mst_vehicle>().ToListAsync();
            return entities.Select(MapToDto);
        }

        /// <summary>
        /// Gets a vehicle by its unique identifier.
        /// </summary>
        /// <param name="id">Vehicle ID.</param>
        /// <returns>The <see cref="VehicleDto"/> if found; otherwise, null.</returns>
        public async Task<VehicleDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Set<Domain.Entities.mst_vehicle>().FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new vehicle.
        /// </summary>
        /// <param name="dto">Vehicle DTO.</param>
        /// <returns>The created <see cref="VehicleDto"/>.</returns>
        public async Task<VehicleDto> CreateAsync(VehicleDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Set<Domain.Entities.mst_vehicle>().Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing vehicle.
        /// </summary>
        /// <param name="id">Vehicle ID.</param>
        /// <param name="dto">Vehicle DTO.</param>
        /// <returns>The updated <see cref="VehicleDto"/>.</returns>
        public async Task<VehicleDto> UpdateAsync(int id, VehicleDto dto)
        {
            var entity = await _context.Set<Domain.Entities.mst_vehicle>().FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.AccountId = dto.AccountId;
            entity.VehicleNumber = dto.VehicleNumber;
            entity.VinOrChassisNumber = dto.VinOrChassisNumber;
            entity.RegistrationDate = dto.RegistrationDate;
            entity.VehicleTypeId = dto.VehicleTypeId;
            entity.VehicleBrandOemId = dto.VehicleBrandOemId;
            entity.OwnershipType = dto.OwnershipType;
            entity.LeasedVendorId = dto.LeasedVendorId;
            entity.ImageFilePath = dto.ImageFilePath;
            entity.Status = dto.Status;
            entity.VehicleClass = dto.VehicleClass;
            entity.RtoPassing = dto.RtoPassing;
            entity.Warranty = dto.Warranty;
            entity.Insurer = dto.Insurer;
            entity.VehicleColor = dto.VehicleColor;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a vehicle.
        /// </summary>
        /// <param name="id">Vehicle ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Set<Domain.Entities.mst_vehicle>().FindAsync(id);
            if (entity == null) return false;
            _context.Set<Domain.Entities.mst_vehicle>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Gets a paged list of vehicles.
        /// </summary>
        /// <param name="page">Page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>A <see cref="PagedResultDto{VehicleDto}"/> containing the paged vehicles.</returns>
        public async Task<PagedResultDto<VehicleDto>> GetPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            var query = _context.Set<Domain.Entities.mst_vehicle>().AsQueryable();
            var totalCount = await query.CountAsync();
            var data = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResultDto<VehicleDto>
            {
                Items = data.Select(MapToDto).ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private static VehicleDto MapToDto(Domain.Entities.mst_vehicle entity)
        {
            return new VehicleDto
            {
                Id = entity.Id,
                AccountId = entity.AccountId,
                VehicleNumber = entity.VehicleNumber,
                VinOrChassisNumber = entity.VinOrChassisNumber,
                RegistrationDate = entity.RegistrationDate,
                VehicleTypeId = entity.VehicleTypeId,
                VehicleBrandOemId = entity.VehicleBrandOemId,
                OwnershipType = entity.OwnershipType,
                LeasedVendorId = entity.LeasedVendorId,
                ImageFilePath = entity.ImageFilePath,
                Status = entity.Status,
                VehicleClass = entity.VehicleClass,
                RtoPassing = entity.RtoPassing,
                Warranty = entity.Warranty,
                Insurer = entity.Insurer,
                VehicleColor = entity.VehicleColor
            };
        }

        private static Domain.Entities.mst_vehicle MapToEntity(VehicleDto dto)
        {
            return MapToEntity(dto, true);
        }

        private static Domain.Entities.mst_vehicle MapToEntity(VehicleDto dto, bool includeId)
        {
            var entity = new Domain.Entities.mst_vehicle
            {
                AccountId = dto.AccountId,
                VehicleNumber = dto.VehicleNumber,
                VinOrChassisNumber = dto.VinOrChassisNumber,
                RegistrationDate = dto.RegistrationDate,
                VehicleTypeId = dto.VehicleTypeId,
                VehicleBrandOemId = dto.VehicleBrandOemId,
                OwnershipType = dto.OwnershipType,
                LeasedVendorId = dto.LeasedVendorId,
                ImageFilePath = dto.ImageFilePath,
                Status = dto.Status,
                VehicleClass = dto.VehicleClass,
                RtoPassing = dto.RtoPassing,
                Warranty = dto.Warranty,
                Insurer = dto.Insurer,
                VehicleColor = dto.VehicleColor
            };
            if (includeId) entity.Id = dto.Id;
            return entity;
        }
        /// <summary>
        /// Bulk create vehicles.
        /// </summary>
        /// <param name="vehicles">List of vehicles to create.</param>
        /// <returns>List of created vehicles.</returns>
        public async Task<IEnumerable<VehicleDto>> BulkCreateAsync(IEnumerable<VehicleDto> vehicles)
        {
            var entities = vehicles.Select(dto => MapToEntity(dto, false)).ToList();
            _context.Set<Domain.Entities.mst_vehicle>().AddRange(entities);
            await _context.SaveChangesAsync();
            return entities.Select(MapToDto).ToList();
        }
    }
}
