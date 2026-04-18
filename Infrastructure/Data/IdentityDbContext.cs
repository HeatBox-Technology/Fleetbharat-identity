
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;

public class IdentityDbContext : DbContext
{
       private readonly ICurrentUserService? _currentUser;

       public IdentityDbContext(
              DbContextOptions<IdentityDbContext> opt,
              ICurrentUserService? currentUser = null) : base(opt)
       {
              _currentUser = currentUser;
       }

       private bool IsAuthenticatedUser => _currentUser?.IsAuthenticated == true;
       private bool IsSystemUser => _currentUser?.IsSystem == true;
       private int[] AccessibleAccountIds => _currentUser?.AccessibleAccountIds?.ToArray() ?? Array.Empty<int>();

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
       public DbSet<SolutionMaster> Solutions => Set<SolutionMaster>();
       public DbSet<PlanEntitlementModule> EntitlementModules => Set<PlanEntitlementModule>();

       /// <summary>
       /// VTS DBSET
       /// </summary>
       /// <param name="modelBuilder"></param>
       public DbSet<mst_device> Devices { get; set; }
       public DbSet<device_transfer> DeviceTransfers { get; set; }
       public DbSet<device_transfer_item> DeviceTransferItems { get; set; }
       public DbSet<mst_device_type> DeviceTypes { get; set; }
       public DbSet<mst_trip_type> TripTypes { get; set; }
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
       public DbSet<mst_Geofence> GeofenceZones { get; set; }
       public DbSet<map_vehicle_device_sync_log> map_vehicle_device_sync_logs { get; set; }
       public DbSet<map_geofence_sync_log> map_geofence_sync_logs { get; set; }

       public DbSet<bulk_job> bulk_jobs { get; set; }
       public DbSet<bulk_job_row> bulk_job_rows { get; set; }
       public DbSet<bulk_column_config> bulk_column_configs { get; set; }
       public DbSet<BulkUploadConfig> BulkUploadConfigs { get; set; }
       public DbSet<map_vehicle_geofence> VehicleGeofenceMaps { get; set; }
       public DbSet<map_vehicle_geofence_sync_log> map_vehicle_geofence_sync_logs { get; set; }
       public DbSet<ErrorLog> ErrorLogs { get; set; }
       public DbSet<AuditLog> AuditLogs { get; set; }
       public DbSet<external_sync_config> external_sync_configs { get; set; }
       public DbSet<external_sync_queue> external_sync_queues { get; set; }
       public DbSet<external_sync_dead_letter> external_sync_dead_letters { get; set; }
       public DbSet<ExternalApiLog> ExternalApiLogs { get; set; }
       public DbSet<BillingPlan> BillingPlans { get; set; }
       public DbSet<PlanFeature> PlanFeatures { get; set; }
       public DbSet<PlanSolution> PlanSolutions { get; set; }
       public DbSet<AccountSubscription> AccountSubscriptions { get; set; }
       public DbSet<UsageRecord> UsageRecords { get; set; }
       public DbSet<BillingInvoice> BillingInvoices { get; set; }





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
                     entity.HasIndex(x => x.HierarchyPath).HasDatabaseName("idx_account_hierarchy");
              });
              modelBuilder.Entity<mst_role>(entity =>
              {
                     entity.ToTable("mst_role");
                     entity.HasKey(x => x.RoleId);
                     entity.HasIndex(x => new { x.AccountId, x.RoleName }).IsUnique();
              });
              modelBuilder.Entity<mst_form>(entity =>
              {
                     entity.ToTable("mst_form");
                     entity.HasKey(x => x.FormId);

                     entity.Property(x => x.FilterConfigJson);

                     entity.HasOne(x => x.FormModule)
                           .WithMany(x => x.Forms)
                           .HasForeignKey(x => x.FormModuleId)
                           .OnDelete(DeleteBehavior.Restrict);

                     entity.HasIndex(x => x.FormModuleId);
              });
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

                     entity.Property(x => x.WhiteLabelId).HasColumnName("WhiteLabelId");
                     entity.Property(x => x.AccountId).HasColumnName("AccountId");

                     entity.Property(x => x.BrandName)
                     .HasColumnName("BrandName")
                     .HasMaxLength(200);

                     entity.Property(x => x.LogoName)
                     .HasColumnName("LogoName")
                     .HasMaxLength(255);

                     entity.Property(x => x.LogoPath)
                     .HasColumnName("LogoPath")
                     .HasMaxLength(500);

                     entity.Property(x => x.CustomEntryFqdn)
                     .HasColumnName("CustomEntryFqdn")
                     .HasMaxLength(200)
                     .IsRequired();

                     entity.Property(x => x.LogoUrl)
                     .HasColumnName("LogoUrl")
                     .HasMaxLength(500);

                     entity.Property(x => x.PrimaryLogoPath).HasColumnName("PrimaryLogoPath").HasMaxLength(500);
                     entity.Property(x => x.PrimaryLogoUrl).HasColumnName("PrimaryLogoUrl").HasMaxLength(500);
                     entity.Property(x => x.AppLogoPath).HasColumnName("AppLogoPath").HasMaxLength(500);
                     entity.Property(x => x.AppLogoUrl).HasColumnName("AppLogoUrl").HasMaxLength(500);
                     entity.Property(x => x.MobileLogoPath).HasColumnName("MobileLogoPath").HasMaxLength(500);
                     entity.Property(x => x.MobileLogoUrl).HasColumnName("MobileLogoUrl").HasMaxLength(500);
                     entity.Property(x => x.FaviconPath).HasColumnName("FaviconPath").HasMaxLength(500);
                     entity.Property(x => x.FaviconUrl).HasColumnName("FaviconUrl").HasMaxLength(500);
                     entity.Property(x => x.LogoDarkPath).HasColumnName("LogoDarkPath").HasMaxLength(500);
                     entity.Property(x => x.LogoDarkUrl).HasColumnName("LogoDarkUrl").HasMaxLength(500);
                     entity.Property(x => x.LogoLightPath).HasColumnName("LogoLightPath").HasMaxLength(500);
                     entity.Property(x => x.LogoLightUrl).HasColumnName("LogoLightUrl").HasMaxLength(500);

                     entity.Property(x => x.PrimaryColorHex)
                     .HasColumnName("PrimaryColorHex")
                     .HasMaxLength(10)
                     .HasDefaultValue("#4F46E5");

                     entity.Property(x => x.SecondaryColorHex).HasColumnName("SecondaryColorHex");
                     entity.Property(x => x.IsActive)
                     .HasColumnName("IsActive")
                     .HasDefaultValue(true);
                     entity.Property(x => x.CreatedOn).HasColumnName("CreatedOn");
                     entity.Property(x => x.UpdatedOn).HasColumnName("UpdatedOn");
                     entity.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
                     entity.Property(x => x.CreatedBy).HasColumnName("createdby");
                     entity.Property(x => x.UpdatedBy).HasColumnName("updatedby");
                     entity.Property(x => x.DeletedBy).HasColumnName("deletedby");
                     entity.Property(x => x.DeletedAt).HasColumnName("deletedat");

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
                     entity.HasIndex(x => new { x.AccountId, x.Email }).IsUnique();
              });
              modelBuilder.Entity<AuditLog>(entity =>
              {
                     entity.ToTable("AuditLogs");

                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.ServiceName).HasMaxLength(100).IsRequired();
                     entity.Property(x => x.Module).HasMaxLength(100);
                     entity.Property(x => x.Action).HasMaxLength(100);
                     entity.Property(x => x.Endpoint).HasMaxLength(500).IsRequired();
                     entity.Property(x => x.EventType).HasMaxLength(50).IsRequired();
                     entity.Property(x => x.IpAddress).HasMaxLength(64);
                     entity.Property(x => x.UserAgent).HasMaxLength(500);
                     entity.Property(x => x.CorrelationId).HasMaxLength(100);
                     entity.Property(x => x.Email).HasMaxLength(256);
                     entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                     entity.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_AuditLogs_CreatedAt");
                     entity.HasIndex(x => x.UserId).HasDatabaseName("IX_AuditLogs_UserId");
                     entity.HasIndex(x => x.AccountId).HasDatabaseName("IX_AuditLogs_AccountId");
                     entity.HasIndex(x => x.ServiceName).HasDatabaseName("IX_AuditLogs_ServiceName");
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
                     e.Property(x => x.SolutionId).HasColumnName("SolutionId");
                     e.Property(x => x.ModuleCode).HasColumnName("ModuleCode");
                     e.Property(x => x.ModuleName).HasColumnName("ModuleName");
                     e.Property(x => x.Description).HasColumnName("Description");
                     e.Property(x => x.IsActive).HasColumnName("IsActive");
                     e.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
                     e.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
                     e.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
                     e.Property(x => x.UpdatedBy).HasColumnName("UpdatedBy");

                     e.HasOne(x => x.Solution)
                      .WithMany(x => x.Modules)
                      .HasForeignKey(x => x.SolutionId)
                      .OnDelete(DeleteBehavior.Restrict);

                     e.HasIndex(x => x.SolutionId);
              });

              modelBuilder.Entity<mst_device>(entity =>
              {
                     entity.HasIndex(x => x.AccountId)
                           .HasDatabaseName("idx_mst_device_accountid");
              });

              modelBuilder.Entity<device_transfer>(entity =>
              {
                     entity.ToTable("device_transfer");
                     entity.HasKey(x => x.Id);

                     entity.Property(x => x.TransferCode)
                           .HasMaxLength(50)
                           .IsRequired();

                     entity.Property(x => x.Status)
                           .HasMaxLength(20)
                           .IsRequired();

                     entity.Property(x => x.Remarks)
                           .HasMaxLength(255);

                     entity.Property(x => x.CreatedAt)
                           .HasDefaultValueSql("CURRENT_TIMESTAMP");

                     entity.HasIndex(x => x.TransferCode)
                           .IsUnique()
                           .HasDatabaseName("ux_device_transfer_code");

                     entity.HasIndex(x => x.Status)
                           .HasDatabaseName("idx_device_transfer_status");

                     entity.HasIndex(x => x.FromAccountId)
                           .HasDatabaseName("idx_device_transfer_from_account");

                     entity.HasIndex(x => x.ToAccountId)
                           .HasDatabaseName("idx_device_transfer_to_account");
              });

              modelBuilder.Entity<device_transfer_item>(entity =>
              {
                     entity.ToTable("device_transfer_items");
                     entity.HasKey(x => x.Id);

                     entity.Property(x => x.CreatedAt)
                           .HasDefaultValueSql("CURRENT_TIMESTAMP");

                     entity.HasIndex(x => x.TransferId)
                           .HasDatabaseName("idx_transfer_items_transferid");

                     entity.HasIndex(x => new { x.TransferId, x.DeviceId })
                           .IsUnique()
                           .HasDatabaseName("ux_transfer_items_transfer_device");

                     entity.HasOne(x => x.Transfer)
                           .WithMany(x => x.Items)
                           .HasForeignKey(x => x.TransferId)
                           .OnDelete(DeleteBehavior.Cascade);

                     entity.HasOne(x => x.Device)
                           .WithMany()
                           .HasForeignKey(x => x.DeviceId)
                           .OnDelete(DeleteBehavior.Restrict);
              });

              modelBuilder.Entity<mst_trip_type>(entity =>
              {
                     entity.ToTable("mst_trip_type");

                     entity.HasKey(x => x.Id);

                     entity.Property(x => x.Id).HasColumnName("id");
                     entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
                     entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
                     entity.Property(x => x.Description).HasColumnName("description");
                     entity.Property(x => x.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
                     entity.Property(x => x.CreatedBy).HasColumnName("created_by");
                     entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                     entity.Property(x => x.UpdatedBy).HasColumnName("updated_by");
                     entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
                     entity.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
                     entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

                     entity.HasIndex(x => x.Code).IsUnique();
                     entity.HasIndex(x => x.Name).IsUnique();
              });

              modelBuilder.Entity<SolutionMaster>(e =>
              {
                     e.ToTable("mst_solution_master");

                     e.HasKey(x => x.Id);

                     e.Property(x => x.Id).HasColumnName("Id");
                     e.Property(x => x.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
                     e.Property(x => x.Description).HasColumnName("Description").HasMaxLength(500);
                     e.Property(x => x.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
                     e.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
              });
              modelBuilder.Entity<PlanEntitlementModule>()
           .ToTable("plan_entitlement_module", "public");

              modelBuilder.Entity<PlanEntitlementModule>()
                  .HasKey(x => new { x.PlanId, x.FormModuleId });

              modelBuilder.Entity<PlanEntitlementModule>()
                  .HasOne(x => x.MarketPlan)
                  .WithMany(x => x.EntitlementModules)
                  .HasForeignKey(x => x.PlanId);

              modelBuilder.Entity<BulkUploadConfig>(entity =>
              {
                     entity.ToTable("bulk_upload_config");
                     entity.HasKey(x => x.Id);

                     entity.Property(x => x.ModuleKey).HasMaxLength(100).IsRequired();
                     entity.Property(x => x.DtoName).HasMaxLength(200).IsRequired();
                     entity.Property(x => x.ServiceInterface).HasMaxLength(200).IsRequired();
                     entity.Property(x => x.ServiceMethod).HasMaxLength(100).IsRequired();
                     entity.Property(x => x.ColumnsJson).HasColumnType("jsonb");
                     entity.HasIndex(x => x.ModuleKey).IsUnique();
              });

              modelBuilder.Entity<mst_vehicle>()
                  .HasKey(x => x.Id);

              modelBuilder.Entity<map_geofence_sync_log>(entity =>
              {
                     entity.ToTable("map_geofence_sync_log");

                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.Id).HasColumnName("id");

                     entity.Property(x => x.GeofenceId)
                    .HasColumnName("geofence_id");

                     entity.Property(x => x.PayloadJson)
                    .HasColumnName("payload_json");

                     entity.Property(x => x.IsSynced)
                    .HasColumnName("is_synced");

                     entity.Property(x => x.ErrorMessage)
                    .HasColumnName("error_message");
                     entity.Property(x => x.RetryCount)
                    .HasColumnName("retry_count");
                     entity.Property(x => x.LastTriedAt)
                    .HasColumnName("last_tried_at");
              });

              modelBuilder.Entity<mst_Geofence>(entity =>
        {
               entity.ToTable("mst_geofence");

               entity.HasKey(x => x.Id);
               entity.Property(x => x.Id).HasColumnName("id");

               entity.Property(x => x.AccountId)
              .HasColumnName("account_id");

               entity.Property(x => x.UniqueCode)
              .HasColumnName("unique_code");

               entity.Property(x => x.DisplayName)
              .HasColumnName("display_name");

               entity.Property(x => x.Description)
              .HasColumnName("description");

               entity.Property(x => x.ClassificationCode)
              .HasColumnName("classification_code");

               entity.Property(x => x.ClassificationLabel)
              .HasColumnName("classification_label");

               entity.Property(x => x.ColorTheme)
              .HasColumnName("color_theme");

               entity.Property(x => x.Opacity)
              .HasColumnName("opacity");

               entity.Property(x => x.GeometryType)
              .HasColumnName("geometry_type");

               entity.Property(x => x.RadiusM)
              .HasColumnName("radius_m");

               entity.Property(x => x.Geom)
              .HasColumnName("geom")
              .HasColumnType("geography");

               entity.Property(x => x.Status)
              .HasColumnName("status");

               entity.Property(x => x.IsDeleted)
              .HasColumnName("is_deleted");

               entity.Property(x => x.MongoId)
              .HasColumnName("mongo_id");

               entity.Property(x => x.SyncStatus)
              .HasColumnName("sync_status");

               entity.Property(x => x.LastSyncedAt)
              .HasColumnName("last_synced_at");

               entity.Property(x => x.SyncError)
              .HasColumnName("sync_error");

               entity.Property(x => x.Version)
              .HasColumnName("version");

               entity.Property(x => x.CreatedBy)
              .HasColumnName("created_by");

               entity.Property(x => x.UpdatedBy)
              .HasColumnName("updated_by");

               entity.Property(x => x.CreatedAt)
              .HasColumnName("created_at");

               entity.Property(x => x.UpdatedAt)
              .HasColumnName("updated_at");

               entity.Property(x => x.CoordinatesJson)
       .HasColumnType("jsonb")
               .HasColumnName("coordinates_json")
               .HasMaxLength(5000);
        });
              modelBuilder.Entity<map_vehicle_geofence>(entity =>
        {
               entity.ToTable("map_vehicle_geofence");

               entity.HasKey(x => x.Id);
               entity.Property(x => x.Id).HasColumnName("id");

               entity.Property(x => x.AccountId).HasColumnName("account_id");
               entity.Property(x => x.VehicleId).HasColumnName("vehicle_id");
               entity.Property(x => x.GeofenceId).HasColumnName("geofence_id");
               entity.Property(x => x.Remarks).HasColumnName("remarks");

               entity.Property(x => x.IsActive).HasColumnName("is_active");
               entity.Property(x => x.IsDeleted).HasColumnName("is_deleted");

               entity.Property(x => x.CreatedBy).HasColumnName("created_by");
               entity.Property(x => x.CreatedAt).HasColumnName("created_at");

               entity.Property(x => x.UpdatedBy).HasColumnName("updated_by");
               entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

               entity.HasIndex(x => new { x.VehicleId, x.GeofenceId })
             .IsUnique();

               entity.HasOne(x => x.Vehicle)
             .WithMany()
             .HasForeignKey(x => x.VehicleId)
             .OnDelete(DeleteBehavior.Restrict);

               entity.HasOne(x => x.Geofence)
             .WithMany()
             .HasForeignKey(x => x.GeofenceId)
             .OnDelete(DeleteBehavior.Restrict);
               entity.Property(x => x.SyncStatus)
    .HasColumnName("sync_status");

               entity.Property(x => x.LastSyncedAt)
             .HasColumnName("last_synced_at");

               entity.Property(x => x.SyncError)
             .HasColumnName("sync_error");
        });
              modelBuilder.Entity<map_vehicle_geofence_sync_log>(entity =>
      {
             entity.ToTable("map_vehicle_geofence_sync_log");

             entity.HasKey(x => x.Id);
             entity.Property(x => x.Id).HasColumnName("id");

             entity.Property(x => x.MappingId)
             .HasColumnName("mapping_id");

             entity.Property(x => x.PayloadJson)
             .HasColumnName("payload_json");

             entity.Property(x => x.IsSynced)
             .HasColumnName("is_synced");

             entity.Property(x => x.ErrorMessage)
             .HasColumnName("error_message");

             entity.Property(x => x.RetryCount)
             .HasColumnName("retry_count");

             entity.Property(x => x.LastTriedAt)
             .HasColumnName("last_tried_at");
      });

              modelBuilder.Entity<external_sync_config>(entity =>
              {
                     entity.ToTable("external_sync_config");
                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.Id).HasColumnName("id");
                     entity.Property(x => x.ModuleName).HasColumnName("module_name").HasMaxLength(100).IsRequired();
                     entity.Property(x => x.ServiceInterface).HasColumnName("service_interface").HasMaxLength(200).IsRequired();
                     entity.Property(x => x.ServiceMethod).HasColumnName("service_method").HasMaxLength(100).IsRequired();
                     entity.Property(x => x.MaxRetryCount).HasColumnName("max_retry_count").HasDefaultValue(5);
                     entity.Property(x => x.RetryIntervalMinutes).HasColumnName("retry_interval_minutes").HasDefaultValue(5);
                     entity.Property(x => x.RetryEnabled).HasColumnName("retry_enabled").HasDefaultValue(true);
                     entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                     entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                     entity.HasIndex(x => x.ModuleName).IsUnique();
              });

              modelBuilder.Entity<external_sync_queue>(entity =>
              {
                     entity.ToTable("external_sync_queue");
                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.Id).HasColumnName("id");
                     entity.Property(x => x.ModuleName).HasColumnName("module_name").HasMaxLength(100).IsRequired();
                     entity.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(100).IsRequired();
                     entity.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
                     entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
                     entity.Property(x => x.RetryCount).HasColumnName("retry_count");
                     entity.Property(x => x.NextRetryTime).HasColumnName("next_retry_time");
                     entity.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
                     entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                     entity.Property(x => x.LastAttemptAt).HasColumnName("last_attempt_at");
                     entity.HasIndex(x => new { x.Status, x.NextRetryTime })
                           .HasDatabaseName("IX_external_sync_queue_status_retry");
                     entity.HasIndex(x => x.CreatedAt)
                           .HasDatabaseName("IX_external_sync_queue_created");
                     entity.HasIndex(x => x.ModuleName)
                           .HasDatabaseName("IX_external_sync_queue_module");
              });

              modelBuilder.Entity<external_sync_dead_letter>(entity =>
              {
                     entity.ToTable("external_sync_dead_letter");
                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.Id).HasColumnName("id");
                     entity.Property(x => x.ModuleName).HasColumnName("module_name").HasMaxLength(100).IsRequired();
                     entity.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(100).IsRequired();
                     entity.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
                     entity.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000).IsRequired();
                     entity.Property(x => x.RetryCount).HasColumnName("retry_count");
                     entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                     entity.Property(x => x.MovedToDLQAt).HasColumnName("moved_to_dlq_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                     entity.HasIndex(x => x.ModuleName);
              });

              modelBuilder.Entity<ExternalApiLog>(entity =>
              {
                     entity.ToTable("ExternalApiLogs");
                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.ServiceName).HasMaxLength(150).IsRequired();
                     entity.Property(x => x.Payload).IsRequired();
                     entity.Property(x => x.Response);
                     entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
                     entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
              });

              modelBuilder.Entity<BillingPlan>(entity =>
              {
                     entity.ToTable("billing_plan");
                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.PlanName).HasMaxLength(200).IsRequired();
                     entity.Property(x => x.Description).HasMaxLength(1000);
                     entity.Property(x => x.PricingModel).HasMaxLength(30).IsRequired();
                     entity.Property(x => x.PlanStatus).HasMaxLength(20).IsRequired();
                     entity.Property(x => x.BaseRate).HasColumnType("numeric(18,2)");
                     entity.Property(x => x.LicensePricePerUnit).HasColumnType("numeric(18,2)");
                     entity.Property(x => x.DiscountPercentage).HasColumnType("numeric(5,2)");
                     entity.Property(x => x.RecurringPlatformFee).HasColumnType("numeric(18,2)");
                     entity.Property(x => x.RecurringAmcFee).HasColumnType("numeric(18,2)");
                     entity.HasIndex(x => new { x.AccountId, x.PlanStatus });
                     entity.HasIndex(x => x.CreatedDate);
              });

              modelBuilder.Entity<PlanFeature>(entity =>
              {
                     entity.ToTable("billing_plan_feature");
                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.FeatureName).HasMaxLength(200).IsRequired();
                     entity.HasIndex(x => new { x.AccountId, x.PlanId });
                     entity.HasOne(x => x.Plan)
                           .WithMany(x => x.Features)
                           .HasForeignKey(x => x.PlanId)
                           .OnDelete(DeleteBehavior.Cascade);
              });

              modelBuilder.Entity<PlanSolution>(entity =>
              {
                     entity.ToTable("billing_plan_solution");
                     entity.HasKey(x => x.Id);
                     entity.HasIndex(x => new { x.PlanId, x.SolutionId }).IsUnique();
                     entity.HasIndex(x => new { x.AccountId, x.PlanId });

                     entity.HasOne(x => x.Plan)
                           .WithMany(x => x.Solutions)
                           .HasForeignKey(x => x.PlanId)
                           .OnDelete(DeleteBehavior.Cascade);

                     entity.HasOne(x => x.Solution)
                           .WithMany()
                           .HasForeignKey(x => x.SolutionId)
                           .OnDelete(DeleteBehavior.Restrict);
              });

              modelBuilder.Entity<AccountSubscription>(entity =>
              {
                     entity.ToTable("billing_account_subscription");
                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
                     entity.HasIndex(x => new { x.AccountId, x.Status });
                     entity.HasIndex(x => x.NextBillingDate);
                     entity.HasOne(x => x.Plan)
                           .WithMany()
                           .HasForeignKey(x => x.PlanId)
                           .OnDelete(DeleteBehavior.Restrict);
              });

              modelBuilder.Entity<UsageRecord>(entity =>
              {
                     entity.ToTable("billing_usage_record");
                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.UsageType).HasMaxLength(100).IsRequired();
                     entity.Property(x => x.UnitsConsumed).HasColumnType("numeric(18,4)");
                     entity.HasIndex(x => new { x.AccountId, x.UsageDate });
                     entity.HasIndex(x => x.SubscriptionId);
                     entity.HasOne(x => x.Subscription)
                           .WithMany()
                           .HasForeignKey(x => x.SubscriptionId)
                           .OnDelete(DeleteBehavior.Cascade);
              });

              modelBuilder.Entity<BillingInvoice>(entity =>
              {
                     entity.ToTable("billing_invoice");
                     entity.HasKey(x => x.Id);
                     entity.Property(x => x.InvoiceNumber).HasMaxLength(80).IsRequired();
                     entity.Property(x => x.Amount).HasColumnType("numeric(18,2)");
                     entity.Property(x => x.Currency).HasMaxLength(20).IsRequired();
                     entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
                     entity.HasIndex(x => x.InvoiceNumber).IsUnique();
                     entity.HasIndex(x => new { x.AccountId, x.Status });
                     entity.HasIndex(x => new { x.SubscriptionId, x.InvoiceDate });
                     entity.HasIndex(x => x.NextRetryDate);
                     entity.HasOne(x => x.Subscription)
                           .WithMany()
                           .HasForeignKey(x => x.SubscriptionId)
                           .OnDelete(DeleteBehavior.Restrict);
              });

              ApplyAccountHierarchyQueryFilters(modelBuilder);
       }

       private void ApplyAccountHierarchyQueryFilters(ModelBuilder modelBuilder)
       {
              var tenantEntityTypes = modelBuilder.Model.GetEntityTypes()
                     .Where(x => typeof(IAccountEntity).IsAssignableFrom(x.ClrType))
                     .Select(x => x.ClrType)
                     .ToList();

              foreach (var clrType in tenantEntityTypes)
              {
                     var method = typeof(IdentityDbContext)
                            .GetMethod(nameof(SetHierarchyFilter), BindingFlags.NonPublic | BindingFlags.Instance);

                     var genericMethod = method?.MakeGenericMethod(clrType);
                     genericMethod?.Invoke(this, new object[] { modelBuilder });
              }
       }

       private void SetHierarchyFilter<TEntity>(ModelBuilder modelBuilder)
              where TEntity : class, IAccountEntity
       {
              modelBuilder.Entity<TEntity>()
                     .HasQueryFilter(e =>
                            !IsAuthenticatedUser ||
                            IsSystemUser ||
                            (AccessibleAccountIds.Length > 0 &&
                             AccessibleAccountIds.Contains(e.AccountId)));
       }



}
