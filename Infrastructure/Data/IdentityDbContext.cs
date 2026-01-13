
using Microsoft.EntityFrameworkCore;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> opt) : base(opt) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<mst_account> Accounts => Set<mst_account>();
    public DbSet<mst_role> Roles => Set<mst_role>();
    public DbSet<mst_form> Forms => Set<mst_form>();
    public DbSet<mst_country> Countries => Set<mst_country>();
    public DbSet<mst_state> States => Set<mst_state>();
    public DbSet<mst_city> Cities => Set<mst_city>();
    public DbSet<map_FormRole_right> FormRoleRights => Set<map_FormRole_right>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<mst_account>()
          .ToTable("mst_account")
          .HasKey(x => x.AccountId);
        modelBuilder.Entity<mst_role>()
          .ToTable("mst_role")
          .HasKey(x => x.RoleId);
        modelBuilder.Entity<mst_form>()
          .ToTable("mst_form")
          .HasKey(x => x.FormId);
        modelBuilder.Entity<mst_country>()
       .ToTable("mst_country")
       .HasKey(x => x.CountryId);
        modelBuilder.Entity<mst_state>()
            .ToTable("mst_state")
            .HasKey(x => x.StateId);
        modelBuilder.Entity<mst_state>()
            .HasIndex(x => new { x.CountryId, x.StateCode })
            .IsUnique();
        modelBuilder.Entity<mst_city>()
           .ToTable("mst_city")
           .HasKey(x => x.CityId);

        modelBuilder.Entity<mst_city>()
            .HasIndex(x => new { x.StateId, x.CityName })
            .IsUnique(); // optional but recommended
        modelBuilder.Entity<map_FormRole_right>()
        .ToTable("map_FormRole_right")
        .HasKey(x => x.RoleFormRightId);

        modelBuilder.Entity<map_FormRole_right>()
            .HasIndex(x => new { x.RoleId, x.FormId })
            .IsUnique(); // one role–form mapping
    }



}
