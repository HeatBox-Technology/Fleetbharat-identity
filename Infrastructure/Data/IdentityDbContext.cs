
using Domain.Entities;
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
    public DbSet<map_user_form_right> UserFormRights => Set<map_user_form_right>();
    public DbSet<PlanUnitLicense> PlanUnitLicenses => Set<PlanUnitLicense>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<FormModule> FormModules => Set<FormModule>();
    public DbSet<PlanEntitlementModule> EntitlementModules => Set<PlanEntitlementModule>();

    /// <summary>
    /// VTS DBSET
    /// </summary>
    /// <param name="modelBuilder"></param>
    public DbSet<mst_device> Devices { get; set; }
    public DbSet<mst_device_type> DeviceTypes { get; set; }
    public DbSet<mst_vehicle> Vehicles { get; set; }
    public DbSet<mst_vehicle_type> VehicleTypes { get; set; }
    public DbSet<mst_sim> Sims { get; set; }
    public DbSet<lkp_sensor_type> SensorTypes { get; set; }
    public DbSet<mst_sensor> Sensors { get; set; }
    public DbSet<map_user_vehicle> UserVehicleMaps { get; set; }
    public DbSet<map_vehicle_device> VehicleDeviceMaps { get; set; }
    public DbSet<map_device_sim> DeviceSimMaps { get; set; }
    public DbSet<map_vehicle_sensor> VehicleSensorMaps { get; set; }
    public DbSet<mst_driver> Drivers { get; set; }

    public DbSet<OemManufacturer> OemManufacturers { get; set; }
    public DbSet<DeviceCategory> DeviceCategories { get; set; }
    public DbSet<NetworkProvider> NetworkProviders { get; set; }
    public DbSet<VehicleBrandOem> VehicleBrandOems { get; set; }
    public DbSet<LeasedVendor> LeasedVendors { get; set; }





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
        modelBuilder.Entity<map_FormRole_right>(entity =>
 {
     entity.ToTable("map_FormRole_right");

     entity.HasKey(x => x.RoleFormRightId);

     entity.Property(x => x.RoleFormRightId)
           .ValueGeneratedOnAdd();   // <-- important

     entity.HasIndex(x => new { x.RoleId, x.FormId })
           .IsUnique();
 });
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

        modelBuilder.Entity<map_user_form_right>(entity =>
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
        modelBuilder.Entity<Currency>(entity =>
    {
        entity.ToTable("mst_currency");

        entity.HasKey(e => e.CurrencyId);

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyId");
        entity.Property(e => e.Code).HasColumnName("Code");
        entity.Property(e => e.Name).HasColumnName("Name");
        entity.Property(e => e.Symbol).HasColumnName("Symbol");
        entity.Property(e => e.Country).HasColumnName("Country");
        entity.Property(e => e.IsActive).HasColumnName("IsActive");
        entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
    });
        modelBuilder.Entity<FormModule>(e =>
        {
            e.ToTable("mst_form_module");

            e.HasKey(x => x.FormModuleId);

            e.Property(x => x.FormModuleId).HasColumnName("FormModuleId");
            e.Property(x => x.ModuleCode).HasColumnName("ModuleCode");
            e.Property(x => x.ModuleName).HasColumnName("ModuleName");
            e.Property(x => x.Description).HasColumnName("Description");
            e.Property(x => x.IsActive).HasColumnName("IsActive");
            e.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            e.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
            e.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
            e.Property(x => x.UpdatedBy).HasColumnName("UpdatedBy");
        });
        modelBuilder.Entity<PlanEntitlementModule>()
     .ToTable("plan_entitlement_module", "public");

        modelBuilder.Entity<PlanEntitlementModule>()
            .HasKey(x => new { x.PlanId, x.FormModuleId });

        modelBuilder.Entity<PlanEntitlementModule>()
            .HasOne(x => x.MarketPlan)
            .WithMany(x => x.EntitlementModules)
            .HasForeignKey(x => x.PlanId);



    }



}
