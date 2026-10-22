using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Entities.Users;
using TaskFlow.Repo.Data;
using TaskFlow.Repo.Users.Implementations;
using TaskFlow.Service.Authentication.Implementations;
using TaskFlow.Service.Authentication.Options;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtSettings = jwtSection.Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JWT settings were not found.");

builder.Services.AddOptions<JwtSettings>()
    .Bind(jwtSection)
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.Issuer),
        "JWT issuer is required.")
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.Audience),
        "JWT audience is required.")
    .Validate(
        settings => settings.Secret.Length >= 32,
        "JWT secret must contain at least 32 characters.")
    .Validate(
        settings => settings.ExpirationMinutes > 0,
        "JWT expiration must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 1;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

    options.SignIn.RequireConfirmedEmail = false;
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.Scan(scan => scan
    .FromAssemblies(
        typeof(AuthenticationService).Assembly,
        typeof(UserRepository).Assembly)
    .AddClasses(classes => classes.Where(type =>
        type.Name.EndsWith("Service", StringComparison.Ordinal) ||
        type.Name.EndsWith("Repository", StringComparison.Ordinal)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "TaskFlow API is running.");

app.Run();
