
using Microsoft.EntityFrameworkCore;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> opt) : base(opt) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<mst_account> Accounts => Set<mst_account>();
    public DbSet<mst_tax_type> TaxTypes => Set<mst_tax_type>();
    public DbSet<mst_role> Roles => Set<mst_role>();
    public DbSet<mst_form> Forms => Set<mst_form>();
    public DbSet<mst_country> Countries => Set<mst_country>();
    public DbSet<mst_state> States => Set<mst_state>();
    public DbSet<mst_city> Cities => Set<mst_city>();
    public DbSet<map_FormRole_right> FormRoleRights => Set<map_FormRole_right>();

    public DbSet<MarketPlan> MarketPlans => Set<MarketPlan>();
    public DbSet<FeatureMaster> Features => Set<FeatureMaster>();
    public DbSet<AddonMaster> Addons => Set<AddonMaster>();
    public DbSet<PlanEntitlement> PlanEntitlements => Set<PlanEntitlement>();
    public DbSet<PlanAddon> PlanAddons => Set<PlanAddon>();
    public DbSet<CustomerPlanAssignment> CustomerPlanAssignments => Set<CustomerPlanAssignment>();
    public DbSet<mst_category> Categories => Set<mst_category>();
    public DbSet<mst_account_configuration> AccountConfigurations => Set<mst_account_configuration>();
    public DbSet<mst_white_label> WhiteLabels => Set<mst_white_label>();
    public DbSet<map_UserFormRight> UserFormRights => Set<map_UserFormRight>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<mst_tax_type>(entity =>
           {
               entity.ToTable("mst_tax_type");
               entity.HasKey(x => x.TaxTypeId);

               entity.Property(x => x.TaxTypeCode).HasMaxLength(50).IsRequired();
               entity.Property(x => x.TaxTypeName).HasMaxLength(100).IsRequired();
           });

        modelBuilder.Entity<mst_account>(entity =>
        {
            entity.ToTable("mst_account");
            entity.HasKey(x => x.AccountId);

            entity.Property(x => x.AccountCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AccountName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PrimaryDomain).HasMaxLength(200).IsRequired();
        });
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
        modelBuilder.Entity<MarketPlan>().HasKey(x => x.PlanId);
        modelBuilder.Entity<FeatureMaster>().HasKey(x => x.FeatureId);
        modelBuilder.Entity<AddonMaster>().HasKey(x => x.AddonId);

        modelBuilder.Entity<PlanEntitlement>().HasKey(x => x.PlanEntitlementId);
        modelBuilder.Entity<PlanAddon>().HasKey(x => x.PlanAddonId);
        modelBuilder.Entity<CustomerPlanAssignment>().HasKey(x => x.CustomerPlanAssignmentId);

        modelBuilder.Entity<PlanEntitlement>()
            .HasOne(x => x.Plan)
            .WithMany(x => x.Entitlements)
            .HasForeignKey(x => x.PlanId);

        modelBuilder.Entity<PlanAddon>()
            .HasOne(x => x.Plan)
            .WithMany(x => x.PlanAddons)
            .HasForeignKey(x => x.PlanId);
        modelBuilder.Entity<mst_category>(entity =>
         {
             entity.ToTable("mst_category");
             entity.HasKey(x => x.CategoryId); // ✅ PK
         });
        modelBuilder.Entity<mst_account_configuration>(entity =>
{
    entity.ToTable("mst_account_configuration");
    entity.HasKey(x => x.AccountConfigurationId);

    entity.Property(x => x.MapProvider).HasMaxLength(50).IsRequired();
    entity.Property(x => x.DateFormat).HasMaxLength(30).IsRequired();
    entity.Property(x => x.TimeFormat).HasMaxLength(10).IsRequired();

    entity.Property(x => x.DistanceUnit).HasMaxLength(10).IsRequired();
    entity.Property(x => x.SpeedUnit).HasMaxLength(10).IsRequired();
    entity.Property(x => x.FuelUnit).HasMaxLength(15).IsRequired();
    entity.Property(x => x.TemperatureUnit).HasMaxLength(15).IsRequired();
    entity.Property(x => x.AddressDisplay).HasMaxLength(10).IsRequired();

    entity.Property(x => x.DefaultLanguage).HasMaxLength(10).IsRequired();
    entity.Property(x => x.AllowedLanguagesCsv).HasMaxLength(200);
    entity.HasIndex(x => x.AccountId).IsUnique();
});
        modelBuilder.Entity<mst_white_label>(entity =>
        {
            entity.ToTable("mst_white_label");
            entity.HasKey(x => x.WhiteLabelId);

            entity.Property(x => x.CustomEntryFqdn)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(x => x.LogoUrl)
                  .HasMaxLength(500);

            entity.Property(x => x.PrimaryColorHex)
                  .HasMaxLength(10)
                  .HasDefaultValue("#4F46E5");

            entity.Property(x => x.IsActive)
                  .HasDefaultValue(true);

            entity.HasIndex(x => x.AccountId).IsUnique();
        });

        modelBuilder.Entity<map_UserFormRight>(entity =>
{
    entity.ToTable("map_user_form_right");

    entity.HasKey(x => x.UserFormRightId);

    entity.HasIndex(x => new { x.AccountId, x.UserId, x.FormId })
          .IsUnique(); // 🚀 Prevent duplicate overrides

    entity.Property(x => x.CanRead).HasDefaultValue(false);
    entity.Property(x => x.CanWrite).HasDefaultValue(false);
    entity.Property(x => x.CanUpdate).HasDefaultValue(false);
    entity.Property(x => x.CanDelete).HasDefaultValue(false);
    entity.Property(x => x.CanExport).HasDefaultValue(false);
    entity.Property(x => x.CanAll).HasDefaultValue(false);
});
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.ProfileImagePath)
                  .HasMaxLength(500);
        });

    }



}
