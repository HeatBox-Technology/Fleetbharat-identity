using Application.DTOs;
using Infrastructure.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Application.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IdentityDbContext _db;

        public VehicleService(IdentityDbContext db)
        {
            _db = db;
        }

        public async Task<int> CreateAsync(CreateVehicleDto dto)
        {
            var vehicleNumber = dto.VehicleNumber.Trim();

            var exists = await _db.Vehicles
                .AnyAsync(x => x.VehicleNumber == vehicleNumber && !x.IsDeleted);

            if (exists)
                throw new InvalidOperationException("Vehicle already exists");

            var entity = new mst_vehicle
            {
                AccountId = dto.AccountId,
                VehicleNumber = vehicleNumber,
                VinOrChassisNumber = dto.VinOrChassisNumber?.Trim(),
                VehicleTypeId = dto.VehicleTypeId,
                Status = "Active",
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.Vehicles.Add(entity);
            await _db.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<VehicleListUiResponseDto> GetVehicles(
            int page,
            int pageSize,
            string? search = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _db.Vehicles
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.VehicleNumber.ToLower().Contains(s));
            }

            var totalFleet = await query.CountAsync();
            var inService = await query.CountAsync(x => x.Status.ToLower() == "active");
            var outOfService = totalFleet - inService;

            var summary = new VehicleSummaryDto
            {
                TotalFleetSize = totalFleet,
                InService = inService,
                OutOfService = outOfService
            };

            var totalRecords = totalFleet;

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new VehicleDto
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    VehicleNumber = x.VehicleNumber,
                    VinOrChassisNumber = x.VinOrChassisNumber,
                    VehicleTypeId = x.VehicleTypeId,
                    Status = x.Status,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    UpdatedBy = x.UpdatedBy,
                    UpdatedAt = x.UpdatedAt,
                    IsDeleted = x.IsDeleted
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return new VehicleListUiResponseDto
            {
                Summary = summary,
                Vehicles = new PagedResultDto<VehicleDto>
                {
                    Items = items,
                    TotalRecords = totalRecords,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                }
            };
        }

        public async Task<VehicleDto?> GetByIdAsync(int id)
        {
            return await _db.Vehicles
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new VehicleDto
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    VehicleNumber = x.VehicleNumber,
                    VinOrChassisNumber = x.VinOrChassisNumber,
                    VehicleTypeId = x.VehicleTypeId,
                    Status = x.Status,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    UpdatedBy = x.UpdatedBy,
                    UpdatedAt = x.UpdatedAt,
                    IsDeleted = x.IsDeleted
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(int id, UpdateVehicleDto dto)
        {
            var entity = await _db.Vehicles
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return false;

            entity.VehicleNumber = dto.VehicleNumber.Trim();
            entity.VinOrChassisNumber = dto.VinOrChassisNumber?.Trim();
            entity.VehicleTypeId = dto.VehicleTypeId;
            entity.Status = dto.Status;
            entity.UpdatedBy = dto.updatedBy;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var entity = await _db.Vehicles
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return false;

            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _db.Vehicles
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResultDto<VehicleDto>> GetPagedAsync(
            int page,
            int pageSize,
            string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Vehicles
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.VehicleNumber.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new VehicleDto
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    VehicleNumber = x.VehicleNumber,
                    VinOrChassisNumber = x.VinOrChassisNumber,
                    VehicleTypeId = x.VehicleTypeId,
                    Status = x.Status,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return new PagedResultDto<VehicleDto>
            {
                Items = data,
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<VehicleDto>> BulkCreateAsync(List<CreateVehicleDto> vehicles)
        {
            var entities = vehicles.Select(dto => new mst_vehicle
            {
                AccountId = dto.AccountId,
                VehicleNumber = dto.VehicleNumber.Trim(),
                VinOrChassisNumber = dto.VinOrChassisNumber?.Trim(),
                VehicleTypeId = dto.VehicleTypeId,
                Status = "Active",
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _db.Vehicles.AddRange(entities);
            await _db.SaveChangesAsync();

            return entities.Select(x => new VehicleDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                VehicleNumber = x.VehicleNumber,
                VinOrChassisNumber = x.VinOrChassisNumber,
                VehicleTypeId = x.VehicleTypeId,
                Status = x.Status,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt
            }).ToList();
        }
    }
}