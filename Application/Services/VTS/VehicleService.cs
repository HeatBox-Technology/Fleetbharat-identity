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

        public async Task<int> CreateAsync(VehicleDto dto)
        {
            var vehicleNumber = dto.VehicleNumber.Trim();

            var exists = await _db.Vehicles
                .AnyAsync(x => x.VehicleNumber == vehicleNumber);

            if (exists)
                throw new InvalidOperationException("Vehicle already exists");

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var entity = new mst_vehicle
                {
                    AccountId = dto.AccountId,
                    VehicleNumber = vehicleNumber,
                    VinOrChassisNumber = dto.VinOrChassisNumber?.Trim(),
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
                    VehicleColor = dto.VehicleColor,
                    //CreatedAt = DateTime.UtcNow,
                    //UpdatedAt = DateTime.UtcNow
                };

                _db.Vehicles.Add(entity);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();

                return entity.Id;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                Console.WriteLine("ERROR ======");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("============");
                throw;
            }
        }

        public async Task<List<VehicleDto>> GetAllAsync(string? search = null)
        {
            var query = _db.Vehicles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.VehicleNumber.ToLower().Contains(s));
            }

            return await query
                .OrderByDescending(x => x.Id)
                .Select(x => new VehicleDto
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    VehicleNumber = x.VehicleNumber,
                    VinOrChassisNumber = x.VinOrChassisNumber,
                    RegistrationDate = x.RegistrationDate,
                    VehicleTypeId = x.VehicleTypeId,
                    VehicleBrandOemId = x.VehicleBrandOemId,
                    OwnershipType = x.OwnershipType,
                    LeasedVendorId = x.LeasedVendorId,
                    ImageFilePath = x.ImageFilePath,
                    Status = x.Status,
                    VehicleClass = x.VehicleClass,
                    RtoPassing = x.RtoPassing,
                    Warranty = x.Warranty,
                    Insurer = x.Insurer,
                    VehicleColor = x.VehicleColor
                })
                .ToListAsync();
        }

        public async Task<VehicleDto?> GetByIdAsync(int id)
        {
            return await _db.Vehicles
                .Where(x => x.Id == id)
                .Select(x => new VehicleDto
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    VehicleNumber = x.VehicleNumber,
                    VinOrChassisNumber = x.VinOrChassisNumber,
                    RegistrationDate = x.RegistrationDate,
                    VehicleTypeId = x.VehicleTypeId,
                    VehicleBrandOemId = x.VehicleBrandOemId,
                    OwnershipType = x.OwnershipType,
                    LeasedVendorId = x.LeasedVendorId,
                    ImageFilePath = x.ImageFilePath,
                    Status = x.Status,
                    VehicleClass = x.VehicleClass,
                    RtoPassing = x.RtoPassing,
                    Warranty = x.Warranty,
                    Insurer = x.Insurer,
                    VehicleColor = x.VehicleColor
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(int id, VehicleDto dto)
        {
            var entity = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return false;

            entity.AccountId = dto.AccountId;
            entity.VehicleNumber = dto.VehicleNumber.Trim();
            entity.VinOrChassisNumber = dto.VinOrChassisNumber?.Trim();
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
            //entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var entity = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return false;

            entity.Status = status;
            //entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return false;

            _db.Vehicles.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResultDto<VehicleDto>> GetPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Vehicles.AsQueryable();

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
                    RegistrationDate = x.RegistrationDate,
                    VehicleTypeId = x.VehicleTypeId,
                    VehicleBrandOemId = x.VehicleBrandOemId,
                    OwnershipType = x.OwnershipType,
                    LeasedVendorId = x.LeasedVendorId,
                    ImageFilePath = x.ImageFilePath,
                    Status = x.Status,
                    VehicleClass = x.VehicleClass,
                    RtoPassing = x.RtoPassing,
                    Warranty = x.Warranty,
                    Insurer = x.Insurer,
                    VehicleColor = x.VehicleColor
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

        public async Task<List<VehicleDto>> BulkCreateAsync(List<VehicleDto> vehicles)
        {
            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var entities = vehicles.Select(dto => new mst_vehicle
                {
                    AccountId = dto.AccountId,
                    VehicleNumber = dto.VehicleNumber.Trim(),
                    VinOrChassisNumber = dto.VinOrChassisNumber?.Trim(),
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
                }).ToList();

                _db.Vehicles.AddRange(entities);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();

                return entities.Select(x => new VehicleDto
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    VehicleNumber = x.VehicleNumber,
                    VinOrChassisNumber = x.VinOrChassisNumber,
                    RegistrationDate = x.RegistrationDate,
                    VehicleTypeId = x.VehicleTypeId,
                    VehicleBrandOemId = x.VehicleBrandOemId,
                    OwnershipType = x.OwnershipType,
                    LeasedVendorId = x.LeasedVendorId,
                    ImageFilePath = x.ImageFilePath,
                    Status = x.Status,
                    VehicleClass = x.VehicleClass,
                    RtoPassing = x.RtoPassing,
                    Warranty = x.Warranty,
                    Insurer = x.Insurer,
                    VehicleColor = x.VehicleColor
                }).ToList();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


    }


}
