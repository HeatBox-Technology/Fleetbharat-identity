
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IdentityDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IStateService, StateService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IFormRoleRightService, FormRoleRightService>();





//Token Service registeration
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication("Bearer")
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// Auto EF Core migration (Code-First)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
