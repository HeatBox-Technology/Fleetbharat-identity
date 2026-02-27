using Infrastructure.Data;
using Infrastructure.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System;
using System.Text;
using NetTopologySuite.Geometries;

var builder = WebApplication.CreateBuilder(args);

var defaultConnection = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is missing.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is missing.");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing.");

builder.Services.AddDbContext<IdentityDbContext>(opt =>
    opt.UseNpgsql(defaultConnection, x => x.UseNetTopologySuite())
       .EnableDetailedErrors()
       .EnableSensitiveDataLogging());
builder.Services.AddHttpClient<IExternalMappingApiService, ExternalMappingApiService>(client =>
{
    client.BaseAddress = new Uri("http://47.128.76.211:8083/api/v1/"); // external base url
});

builder.Services.AddVtsServices(builder.Configuration, defaultConnection);

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountProvisionService, AccountProvisionService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IStateService, StateService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IFormRoleRightService, FormRoleRightService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IFeatureService, FeatureService>();
builder.Services.AddScoped<IAddonService, AddonService>();
builder.Services.AddScoped<IPlanEntitlementService, PlanEntitlementService>();
builder.Services.AddScoped<IPlanAddonService, PlanAddonService>();
builder.Services.AddScoped<ICustomerPlanService, CustomerPlanService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITaxTypeService, TaxTypeService>();
builder.Services.AddScoped<IAccountConfigurationService, AccountConfigurationService>();
builder.Services.AddScoped<IWhiteLabelService, WhiteLabelService>();
builder.Services.AddScoped<ICommonDropdownService, CommonDropdownService>();
builder.Services.AddSingleton<BulkQueue>();
builder.Services.AddHostedService<BulkWorker>();
builder.Services.AddScoped<IBulkService, BulkService>();
builder.Services.AddScoped<BulkProcessorFactory>();
builder.Services.AddScoped<IBulkModuleProcessor, GeofenceBulkProcessor>();

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
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IOT.IdentityService API",
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
builder.Services.AddHostedService<Infrastructure.LiveTracking.LiveTrackingSimulatorService>();
// ✅ Redis subscriber -> SignalR broadcaster (must be resilient too)
builder.Services.AddHostedService<RedisGpsSubscriberHostedService>();


var app = builder.Build();

//
// ✅ FIXED: enable swagger in Docker (Staging) also
//
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AppCors");

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers().RequireAuthorization();
app.MapHub<TrackingHub>("/hubs/tracking").RequireAuthorization();


app.Run();

public partial class Program { }
