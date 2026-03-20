using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Infrastructure.Data;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for mapping users to vehicles (vehicle-wise login/filtering).
    /// Handles CRUD operations for user-vehicle access mapping.
    /// </summary>
    public class UserVehicleMapService : IUserVehicleMapService
    {
        private readonly IdentityDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserVehicleMapService"/> class.
        /// </summary>
        public UserVehicleMapService(IdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all user-vehicle mappings.
        /// </summary>
        /// <returns>List of <see cref="UserVehicleMapDto"/>.</returns>
        public async Task<IEnumerable<UserVehicleMapDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Set<Domain.Entities.map_user_vehicle>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x =>
                    x.UserId.ToString().ToLower().Contains(s) ||
                    x.VehicleId.ToString().ToLower().Contains(s));
            }

            return await query
                .OrderByDescending(x => x.UserVehicleId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserVehicleMapDto
                {
                    UserVehicleId = x.UserVehicleId,
                    UserId = x.UserId,
                    VehicleId = x.VehicleId,
                    FromTs = x.FromTs,
                    ToTs = x.ToTs
                })
                .ToListAsync();
        }

        /// <summary>
        /// Gets a user-vehicle mapping by its unique identifier.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>The <see cref="UserVehicleMapDto"/> if found; otherwise, null.</returns>
        public async Task<UserVehicleMapDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.map_user_vehicle>().FindAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        /// <summary>
        /// Creates a new user-vehicle mapping.
        /// </summary>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The created <see cref="UserVehicleMapDto"/>.</returns>
        public async Task<UserVehicleMapDto> CreateAsync(UserVehicleMapDto dto)
        {
            var entity = MapToEntity(dto, false);
            _context.Set<Domain.Entities.map_user_vehicle>().Add(entity);
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Updates an existing user-vehicle mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <param name="dto">Mapping DTO.</param>
        /// <returns>The updated <see cref="UserVehicleMapDto"/>.</returns>
        public async Task<UserVehicleMapDto> UpdateAsync(long id, UserVehicleMapDto dto)
        {
            var entity = await _context.Set<Domain.Entities.map_user_vehicle>().FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            entity.UserId = dto.UserId;
            entity.VehicleId = dto.VehicleId;
            entity.FromTs = dto.FromTs;
            entity.ToTs = dto.ToTs;
            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        /// <summary>
        /// Deletes a user-vehicle mapping.
        /// </summary>
        /// <param name="id">Mapping ID.</param>
        /// <returns>True if deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.Set<Domain.Entities.map_user_vehicle>().FindAsync(id);
            if (entity == null) return false;
            _context.Set<Domain.Entities.map_user_vehicle>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static UserVehicleMapDto MapToDto(Domain.Entities.map_user_vehicle entity)
        {
            return new UserVehicleMapDto
            {
                UserVehicleId = entity.UserVehicleId,
                UserId = entity.UserId,
                VehicleId = entity.VehicleId,
                FromTs = entity.FromTs,
                ToTs = entity.ToTs
            };
        }

        private static Domain.Entities.map_user_vehicle MapToEntity(UserVehicleMapDto dto, bool includeId)
        {
            var entity = new Domain.Entities.map_user_vehicle
            {
                UserId = dto.UserId,
                VehicleId = dto.VehicleId,
                FromTs = dto.FromTs == default ? System.DateTime.UtcNow : dto.FromTs,
                ToTs = dto.ToTs
            };
            if (includeId) entity.UserVehicleId = dto.UserVehicleId;
            return entity;
        }
    }
}
