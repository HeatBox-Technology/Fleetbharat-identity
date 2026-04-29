using Infrastructure.Data;
using Infrastructure.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using NetTopologySuite.Geometries;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

bool IsHostedServiceEnabled(string key, bool productionDefault = true)
{
    var configured = builder.Configuration.GetValue<bool?>($"HostedServices:{key}");
    if (configured.HasValue)
    {
        return configured.Value;
    }

    return builder.Environment.IsDevelopment() ? false : productionDefault;
}

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

var defaultConnection = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is missing.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is missing.");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing.");

var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "storage", "keys");
Directory.CreateDirectory(dataProtectionPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("FleetBharat.IdentityService");

builder.Services.Configure<AuditLoggingOptions>(
    builder.Configuration.GetSection("AuditLogging"));
builder.Services.Configure<KafkaRealtimeOptions>(
    builder.Configuration.GetSection(KafkaRealtimeOptions.SectionName));

builder.Services.AddDbContext<IdentityDbContext>((sp, opt) =>
    opt.UseNpgsql(defaultConnection, x => x.UseNetTopologySuite())
       .EnableDetailedErrors()
       .EnableSensitiveDataLogging()
       .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));
builder.Services.AddHttpClient<IExternalMappingApiService, ExternalMappingApiService>(client =>
{
    //client.BaseAddress = new Uri("http://47.131.171.150:5000/api/v1/"); // external base url
    client.BaseAddress = new Uri("http://92.4.76.230:8083/api/v1/"); // local for testing
});
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();

builder.Services.AddVtsServices(builder.Configuration, defaultConnection);

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountProvisionService, AccountProvisionService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IStateService, StateService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFormRoleRightService, FormRoleRightService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IFeatureService, FeatureService>();
builder.Services.AddScoped<IAddonService, AddonService>();
builder.Services.AddScoped<IPlanEntitlementService, PlanEntitlementService>();
builder.Services.AddScoped<IPlanAddonService, PlanAddonService>();
builder.Services.AddScoped<ICustomerPlanService, CustomerPlanService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BackgroundCurrentUserContext>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddSingleton<AuditQueue>();
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IAuditLogStore, DatabaseAuditLogStore>();
builder.Services.AddHostedService<AuditWorker>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITaxTypeService, TaxTypeService>();
builder.Services.AddScoped<IAccountConfigurationService, AccountConfigurationService>();
builder.Services.AddScoped<IWhiteLabelService, WhiteLabelService>();
builder.Services.AddScoped<ICommonDropdownService, CommonDropdownService>();
builder.Services.AddScoped<IFormConfigurationRepository, FormConfigurationRepository>();
builder.Services.AddScoped<IFormConfigurationService, FormConfigurationService>();
builder.Services.AddScoped<IHierarchyRepository, HierarchyRepository>();
builder.Services.AddScoped<IHierarchyService, HierarchyService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IBulkUploadQueue, BulkUploadQueue>();
builder.Services.AddHostedService<BulkUploadWorker>();
builder.Services.AddScoped<IBulkUploadService, BulkUploadService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IExcelParser, ExcelParser>();
builder.Services.AddScoped<ICsvParser, CsvParser>();
builder.Services.AddScoped<IServiceResolver, ServiceResolver>();
builder.Services.AddScoped<ILookupResolverService, LookupResolverService>();
builder.Services.AddScoped<IUniqueFieldValidator, UniqueFieldValidator>();
builder.Services.AddScoped<IBulkLookupResolver, AccountBulkLookupResolver>();
builder.Services.AddScoped<IBulkLookupResolver, VehicleTypeBulkLookupResolver>();
builder.Services.AddScoped<IBulkLookupResolver, ManufacturerBulkLookupResolver>();
builder.Services.AddScoped<IBulkLookupResolver, DeviceTypeBulkLookupResolver>();
builder.Services.AddScoped<IBulkLookupResolver, GeofenceBulkLookupResolver>();
builder.Services.AddScoped<IBulkUniqueRule, VehicleBulkUniqueRule>();
builder.Services.AddScoped<IBulkUniqueRule, DeviceBulkUniqueRule>();
builder.Services.AddScoped<IBulkUniqueRule, DriverBulkUniqueRule>();
builder.Services.AddScoped<IBulkUniqueRule, GeofenceBulkUniqueRule>();
builder.Services.AddScoped<IBulkCustomValidator, VehicleBulkCustomValidator>();
builder.Services.AddScoped<IBulkCustomValidator, GeofenceBulkCustomValidator>();
builder.Services.AddScoped<IBulkProcessor, ConfigurableBulkProcessor>();
builder.Services.AddHttpClient<IExternalBulkSyncService, ExternalBulkSyncService>();
builder.Services.AddScoped<DbLogger>();
builder.Services.AddScoped<IBillingRepository, BillingRepository>();
builder.Services.AddScoped<IBillingPlanService, BillingPlanService>();
builder.Services.AddScoped<IBillingSubscriptionService, BillingSubscriptionService>();
builder.Services.AddScoped<IBillingUsageService, BillingUsageService>();
builder.Services.AddScoped<IBillingInvoiceService, BillingInvoiceService>();
builder.Services.AddScoped<IBillingCalculationService, BillingCalculationService>();
builder.Services.AddScoped<IBillingRetryService, BillingRetryService>();
builder.Services.AddScoped<IBillingAnalyticsService, BillingAnalyticsService>();
builder.Services.Configure<ExternalSyncWorkerOptions>(
builder.Configuration.GetSection(ExternalSyncWorkerOptions.SectionName));
builder.Services.AddScoped<IExternalSyncRepository, ExternalSyncRepository>();
builder.Services.AddScoped<IExternalApiLogRepository, ExternalApiLogRepository>();
builder.Services.AddScoped<IExternalSyncQueueService, ExternalSyncQueueService>();
builder.Services.AddScoped<IExternalSyncRetryPolicy, ExternalSyncRetryPolicyService>();
builder.Services.AddScoped<IExternalSyncInvoker, ExternalSyncInvoker>();
builder.Services.AddScoped<IExternalDeadLetterService, ExternalDeadLetterService>();
builder.Services.AddScoped<IExternalSyncDashboardService, ExternalSyncDashboardService>();
builder.Services.AddScoped<IExampleExternalSyncService, ExampleExternalSyncService>();
builder.Services.AddSingleton<IExternalSyncConcurrencyLimiter>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalSyncWorkerOptions>>().Value;
    return new ExternalSyncConcurrencyLimiter(options.MaxConcurrency);
});
// if (IsHostedServiceEnabled("ExternalSyncWorker"))
// {
//     builder.Services.AddHostedService<ExternalSyncWorker>();
// }

if (IsHostedServiceEnabled("BillingScheduler"))
{
    builder.Services.AddHostedService<BillingSchedulerService>();
}

if (IsHostedServiceEnabled("InvoiceWorker"))
{
    builder.Services.AddHostedService<InvoiceWorker>();
}


var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
            return;
        }

        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
            return;
        }

        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<IRealtimeNotificationBroadcaster, SignalRRealtimeNotificationBroadcaster>();
builder.Services.AddSingleton<IKafkaAlertPublisher, KafkaAlertPublisher>();

var redisConn = builder.Configuration["Redis:ConnectionString"];
if (string.IsNullOrWhiteSpace(redisConn))
    throw new InvalidOperationException("Redis:ConnectionString is missing.");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = ConfigurationOptions.Parse(redisConn);

    options.AbortOnConnectFail = false;
    options.ConnectRetry = 5;
    options.ConnectTimeout = 5000;
    options.SyncTimeout = 5000;
    options.KeepAlive = 15;

    return ConnectionMultiplexer.Connect(options);
});

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted)
                    return;

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var message = context.Request.Headers.ContainsKey("Authorization")
                    ? "Authorization token is invalid or expired."
                    : "Authorization token is required.";

                var response = ApiResponse<object>.Fail(message, StatusCodes.Status401Unauthorized);
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                    return;

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var response = ApiResponse<object>.Fail("Forbidden", StatusCodes.Status403Forbidden);
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        };
    });

// builder.Services.AddAuthorization(options =>
// {
//     options.FallbackPolicy = new AuthorizationPolicyBuilder()
//         .RequireAuthenticatedUser()
//         .Build();
// });
// ✅ AUTHORIZATION (ENVIRONMENT BASED)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true) // Allow all in dev
            .Build();
    });
}
else
{
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FleetBharat.IdentityService API",
        Version = "v1"
    });
    // 🔐 Bearer Token Configuration
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Token like: Bearer {your token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});



// ✅ Live tracking simulator for animation demo
// ✅ Redis subscriber -> SignalR broadcaster (must be resilient too)
if (IsHostedServiceEnabled("RedisGpsSubscriber"))
{
    builder.Services.AddHostedService<RedisGpsSubscriberHostedService>();
}

if (IsHostedServiceEnabled("KafkaRealtimeConsumer"))
{
    builder.Services.AddHostedService<KafkaRealtimeConsumerWorker>();
}
// builder.Services.AddHostedService<Infrastructure.LiveTracking.LiveTrackingSimulatorService>();


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var bootstrapper = new ExternalSyncBootstrapper(scope.ServiceProvider.GetRequiredService<IdentityDbContext>());
    await bootstrapper.InitializeAsync();
}

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
var storagePath = Path.Combine(app.Environment.ContentRootPath, "storage");
Directory.CreateDirectory(uploadsPath);
Directory.CreateDirectory(storagePath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storagePath),
    RequestPath = "/storage"
});

//
// ✅ FIXED: enable swagger in Docker (Staging) also
//
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AppCors");

app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();


// app.MapControllers().RequireAuthorization();
// app.MapHub<TrackingHub>("/hubs/tracking").RequireAuthorization();
// ✅ ENVIRONMENT BASED ENDPOINT SECURITY
if (app.Environment.IsDevelopment())
{
    app.MapControllers();
    app.MapHub<TrackingHub>("/hubs/tracking");
}
else
{
    app.MapControllers().RequireAuthorization();
    app.MapHub<TrackingHub>("/hubs/tracking").RequireAuthorization();
}


app.Run();

public partial class Program { }
