
using Microsoft.EntityFrameworkCore;
using FleetRobo.IdentityService.Domain.Entities;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> opt) : base(opt) { }
    public DbSet<User> Users => Set<User>();
}
