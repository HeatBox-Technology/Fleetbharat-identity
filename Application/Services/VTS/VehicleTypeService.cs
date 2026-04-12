using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
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
        private const long MaxIconFileSizeBytes = 2 * 1024 * 1024;
        private static readonly HashSet<string> AllowedIconContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpg",
            "image/jpeg"
        };

        private readonly IdentityDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IFileStorageService _fileStorage;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleTypeService"/> class.
        /// </summary>
        public VehicleTypeService(
            IdentityDbContext context,
            ICurrentUserService currentUser,
            IFileStorageService fileStorage)
        {
            _context = context;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// Gets all vehicle types.
        /// </summary>
        /// <returns>List of <see cref="VehicleTypeDto"/>.</returns>
        public async Task<IEnumerable<VehicleTypeDto>> GetAllAsync(int page = 1, int pageSize = 10, int? accountId = null, string? search = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Set<Domain.Entities.mst_vehicle_type>()
                .AsNoTracking()
                .ApplyAccountHierarchyFilter(_currentUser)
                .AsQueryable();

            if (accountId.HasValue)
                query = query.Where(x => x.AccountId == accountId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.VehicleTypeName ?? "").ToLower().Contains(s) ||
                    (x.Category ?? "").ToLower().Contains(s) ||
                    (x.FuelCategory ?? "").ToLower().Contains(s));
            }

            return await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new VehicleTypeDto
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    VehicleTypeName = x.VehicleTypeName,
                    Category = x.Category,
                    MovingIcon = x.MovingIcon,
                    StoppedIcon = x.StoppedIcon,
                    IdleIcon = x.IdleIcon,
                    ParkedIcon = x.ParkedIcon,
                    OfflineIcon = x.OfflineIcon,
                    BreakdownIcon = x.BreakdownIcon,
                    SeatingCapacity = x.SeatingCapacity,
                    WheelsCount = x.WheelsCount,
                    FuelCategory = x.FuelCategory,
                    TankCapacity = x.TankCapacity,
                    DefaultSpeedLimit = x.DefaultSpeedLimit,
                    DefaultIdleThreshold = x.DefaultIdleThreshold,
                    Status = x.Status
                })
                .ToListAsync();
        }

        /// <summary>
        /// Gets a vehicle type by its unique identifier.
        /// </summary>
        /// <param name="id">Vehicle type ID.</param>
        /// <returns>The <see cref="VehicleTypeDto"/> if found; otherwise, null.</returns>
        public async Task<VehicleTypeDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Set<Domain.Entities.mst_vehicle_type>()
                .AsNoTracking()
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.Id == id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new vehicle type.
        /// </summary>
        /// <param name="dto">Vehicle type DTO.</param>
        /// <returns>The created <see cref="VehicleTypeDto"/>.</returns>
        public async Task<VehicleTypeDto> CreateAsync(VehicleTypeDto dto)
        {
            var accountExists = await _context.Accounts
                .ApplyAccountHierarchyFilter(_currentUser)
                .AnyAsync(x => x.AccountId == dto.AccountId && !x.IsDeleted);

            if (!accountExists)
                throw new KeyNotFoundException("Account not found");

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
            var accountExists = await _context.Accounts
                .ApplyAccountHierarchyFilter(_currentUser)
                .AnyAsync(x => x.AccountId == dto.AccountId && !x.IsDeleted);

            if (!accountExists)
                throw new KeyNotFoundException("Account not found");

            var entity = await _context.Set<Domain.Entities.mst_vehicle_type>()
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) throw new KeyNotFoundException();
            entity.AccountId = dto.AccountId;
            entity.VehicleTypeName = dto.VehicleTypeName;
            entity.Category = dto.Category;
            entity.MovingIcon = dto.MovingIcon;
            entity.StoppedIcon = dto.StoppedIcon;
            entity.IdleIcon = dto.IdleIcon;
            entity.ParkedIcon = dto.ParkedIcon;
            entity.OfflineIcon = dto.OfflineIcon;
            entity.BreakdownIcon = dto.BreakdownIcon;
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

        public async Task<VehicleTypeIconUploadResponseDto> UploadIconsAsync(int accountId, int id, VehicleTypeIconUploadRequest req)
        {
            if (!HasAnyIcon(req))
                throw new InvalidOperationException("At least one icon file is required.");

            var accountExists = await _context.Accounts
                .ApplyAccountHierarchyFilter(_currentUser)
                .AnyAsync(x => x.AccountId == accountId && !x.IsDeleted);

            if (!accountExists)
                throw new KeyNotFoundException("Account not found");

            var entity = await _context.Set<Domain.Entities.mst_vehicle_type>().FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Vehicle type not found");

            if (entity.AccountId != accountId)
                throw new InvalidOperationException("Vehicle type does not belong to the specified account");

            if (req.MovingIcon != null && req.MovingIcon.Length > 0)
            {
                ValidateIconFile(req.MovingIcon, nameof(req.MovingIcon));
                entity.MovingIcon = await _fileStorage.SaveVehicleTypeIconAsync(accountId, id, "moving", req.MovingIcon);
            }

            if (req.StoppedIcon != null && req.StoppedIcon.Length > 0)
            {
                ValidateIconFile(req.StoppedIcon, nameof(req.StoppedIcon));
                entity.StoppedIcon = await _fileStorage.SaveVehicleTypeIconAsync(accountId, id, "stopped", req.StoppedIcon);
            }

            if (req.IdleIcon != null && req.IdleIcon.Length > 0)
            {
                ValidateIconFile(req.IdleIcon, nameof(req.IdleIcon));
                entity.IdleIcon = await _fileStorage.SaveVehicleTypeIconAsync(accountId, id, "idle", req.IdleIcon);
            }

            if (req.ParkedIcon != null && req.ParkedIcon.Length > 0)
            {
                ValidateIconFile(req.ParkedIcon, nameof(req.ParkedIcon));
                entity.ParkedIcon = await _fileStorage.SaveVehicleTypeIconAsync(accountId, id, "parked", req.ParkedIcon);
            }

            if (req.OfflineIcon != null && req.OfflineIcon.Length > 0)
            {
                ValidateIconFile(req.OfflineIcon, nameof(req.OfflineIcon));
                entity.OfflineIcon = await _fileStorage.SaveVehicleTypeIconAsync(accountId, id, "offline", req.OfflineIcon);
            }

            if (req.BreakdownIcon != null && req.BreakdownIcon.Length > 0)
            {
                ValidateIconFile(req.BreakdownIcon, nameof(req.BreakdownIcon));
                entity.BreakdownIcon = await _fileStorage.SaveVehicleTypeIconAsync(accountId, id, "breakdown", req.BreakdownIcon);
            }

            await _context.SaveChangesAsync();

            return new VehicleTypeIconUploadResponseDto
            {
                AccountId = accountId,
                VehicleTypeId = id,
                MovingIcon = entity.MovingIcon,
                StoppedIcon = entity.StoppedIcon,
                IdleIcon = entity.IdleIcon,
                ParkedIcon = entity.ParkedIcon,
                OfflineIcon = entity.OfflineIcon,
                BreakdownIcon = entity.BreakdownIcon
            };
        }

        /// <summary>
        /// Deletes a vehicle type.
        /// </summary>
        /// <param name="id">Vehicle type ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Set<Domain.Entities.mst_vehicle_type>()
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.Id == id);
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
                AccountId = entity.AccountId,
                VehicleTypeName = entity.VehicleTypeName,
                Category = entity.Category,
                MovingIcon = entity.MovingIcon,
                StoppedIcon = entity.StoppedIcon,
                IdleIcon = entity.IdleIcon,
                ParkedIcon = entity.ParkedIcon,
                OfflineIcon = entity.OfflineIcon,
                BreakdownIcon = entity.BreakdownIcon,
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
                AccountId = dto.AccountId,
                VehicleTypeName = dto.VehicleTypeName,
                Category = dto.Category,
                MovingIcon = dto.MovingIcon,
                StoppedIcon = dto.StoppedIcon,
                IdleIcon = dto.IdleIcon,
                ParkedIcon = dto.ParkedIcon,
                OfflineIcon = dto.OfflineIcon,
                BreakdownIcon = dto.BreakdownIcon,
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

        private static void ValidateIconFile(IFormFile file, string iconType)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException($"{iconType} file is required.");

            if (!AllowedIconContentTypes.Contains(file.ContentType))
                throw new InvalidOperationException($"{iconType} format is invalid. Allowed formats: PNG, JPG, JPEG.");

            if (file.Length > MaxIconFileSizeBytes)
                throw new InvalidOperationException($"{iconType} file size must be 2 MB or less.");
        }

        private static bool HasAnyIcon(VehicleTypeIconUploadRequest req)
        {
            return (req.MovingIcon?.Length ?? 0) > 0 ||
                   (req.StoppedIcon?.Length ?? 0) > 0 ||
                   (req.IdleIcon?.Length ?? 0) > 0 ||
                   (req.ParkedIcon?.Length ?? 0) > 0 ||
                   (req.OfflineIcon?.Length ?? 0) > 0 ||
                   (req.BreakdownIcon?.Length ?? 0) > 0;
        }
    }
}
