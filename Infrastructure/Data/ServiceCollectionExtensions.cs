
using Application.Services;
using Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Infrastructure.Data
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVtsServices(
            this IServiceCollection services,
            IConfiguration configuration,
            string connectionString)
        {
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IDeviceService, DeviceService>();
            services.AddScoped<IDeviceTransferService, DeviceTransferService>();
            services.AddScoped<IDeviceTypeService, DeviceTypeService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IVehicleComplianceService, VehicleComplianceService>();
            services.AddScoped<IVehicleTypeService, VehicleTypeService>();
            services.AddScoped<ISimService, SimService>();
            services.AddScoped<ISensorTypeService, SensorTypeService>();
            services.AddScoped<ISensorService, SensorService>();
            services.AddScoped<IUserVehicleMapService, UserVehicleMapService>();
            services.AddScoped<IVehicleDeviceMapService, VehicleDeviceMapService>();
            services.AddScoped<IDeviceSimMapService, DeviceSimMapService>();
            services.AddScoped<IVehicleSensorMapService, VehicleSensorMapService>();
            services.AddScoped<IOemManufacturerService, OemManufacturerService>();
            services.AddScoped<INetworkProviderService, NetworkProviderService>();
            services.AddScoped<IServiceVendorService, ServiceVendorService>();
            services.AddScoped<IDeviceModelService, DeviceModelService>();
            services.AddScoped<IVehicleBrandService, VehicleBrandService>();
            services.AddScoped<IDriverService, DriverService>();
            services.AddScoped<IGeofenceService, GeofenceService>();
            services.AddScoped<IVehicleGeofenceMapService, VehicleGeofenceMapService>();
            services.AddScoped<IVtsExternalApiEnqueueService, VtsExternalApiEnqueueService>();
            services.AddScoped<IVtsExternalApiSyncDispatcher, VtsExternalApiSyncDispatcher>();


            // ✅ Redis
            var redisConn = configuration.GetSection("Redis")["ConnectionString"];
            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                services.AddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect(redisConn));

                services.AddScoped<IRedisCacheService, RedisCacheService>();
            }

            return services;
        }
    }
}
