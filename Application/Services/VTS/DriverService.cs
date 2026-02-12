using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for driver operations.
    /// </summary>
    public class DriverService : IDriverService
    {
        private readonly IdentityDbContext _context;
        public DriverService(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DriverDto>> GetAllAsync()
        {
            return await _context.Drivers
                .AsNoTracking()
                .Select(d => new DriverDto
                {
                    DriverId = d.DriverId,
                    AccountId = d.AccountId,
                    Name = d.Name,
                    Mobile = d.Mobile,
                    LicenseNumber = d.LicenseNumber,
                    LicenseExpiry = d.LicenseExpiry,
                    BloodGroup = d.BloodGroup ?? string.Empty,
                    EmergencyContact = d.EmergencyContact ?? string.Empty,
                    StatusKey = d.StatusKey,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<DriverDto> GetByIdAsync(long driverId)
        {
            var d = await _context.Drivers.FindAsync(driverId);
            if (d == null) return null!;
            return new DriverDto
            {
                DriverId = d.DriverId,
                AccountId = d.AccountId,
                Name = d.Name,
                Mobile = d.Mobile,
                LicenseNumber = d.LicenseNumber,
                LicenseExpiry = d.LicenseExpiry,
                BloodGroup = d.BloodGroup ?? string.Empty,
                EmergencyContact = d.EmergencyContact ?? string.Empty,
                StatusKey = d.StatusKey,
                CreatedAt = d.CreatedAt
            };
        }

        public async Task<DriverDto> CreateAsync(DriverDto dto)
        {
            var entity = new mst_driver
            {
                AccountId = dto.AccountId,
                Name = dto.Name,
                Mobile = dto.Mobile,
                LicenseNumber = dto.LicenseNumber,
                LicenseExpiry = dto.LicenseExpiry,
                BloodGroup = dto.BloodGroup,
                EmergencyContact = dto.EmergencyContact,
                StatusKey = dto.StatusKey,
                CreatedAt = dto.CreatedAt
            };
            _context.Drivers.Add(entity);
            await _context.SaveChangesAsync();
            dto.DriverId = entity.DriverId;
            return dto;
        }

        public async Task<DriverDto> UpdateAsync(DriverDto dto)
        {
            var entity = await _context.Drivers.FindAsync(dto.DriverId);
            if (entity == null) return null!;
            entity.AccountId = dto.AccountId;
            entity.Name = dto.Name;
            entity.Mobile = dto.Mobile;
            entity.LicenseNumber = dto.LicenseNumber;
            entity.LicenseExpiry = dto.LicenseExpiry;
            entity.BloodGroup = dto.BloodGroup;
            entity.EmergencyContact = dto.EmergencyContact;
            entity.StatusKey = dto.StatusKey;
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<bool> DeleteAsync(long driverId)
        {
            var entity = await _context.Drivers.FindAsync(driverId);
            if (entity == null) return false;
            _context.Drivers.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DriverDto>> BulkCreateAsync(IEnumerable<DriverDto> drivers)
        {
            var entities = drivers.Select(dto => new mst_driver
            {
                AccountId = dto.AccountId,
                Name = dto.Name,
                Mobile = dto.Mobile,
                LicenseNumber = dto.LicenseNumber,
                LicenseExpiry = dto.LicenseExpiry,
                BloodGroup = dto.BloodGroup,
                EmergencyContact = dto.EmergencyContact,
                StatusKey = dto.StatusKey,
                CreatedAt = dto.CreatedAt
            }).ToList();
            _context.Drivers.AddRange(entities);
            await _context.SaveChangesAsync();
            for (int i = 0; i < entities.Count; i++)
                drivers.ElementAt(i).DriverId = entities[i].DriverId;
            return drivers;
        }

        public async Task<PagedResultDto<DriverDto>> GetPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            var query = _context.Drivers.AsQueryable();
            var totalCount = await query.CountAsync();
            var data = await query
                .OrderByDescending(d => d.DriverId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResultDto<DriverDto>
            {
                Items = data.Select(d => new DriverDto
                {
                    DriverId = d.DriverId,
                    AccountId = d.AccountId,
                    Name = d.Name,
                    Mobile = d.Mobile,
                    LicenseNumber = d.LicenseNumber,
                    LicenseExpiry = d.LicenseExpiry,
                    BloodGroup = d.BloodGroup ?? string.Empty,
                    EmergencyContact = d.EmergencyContact ?? string.Empty,
                    StatusKey = d.StatusKey,
                    CreatedAt = d.CreatedAt
                }).ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
