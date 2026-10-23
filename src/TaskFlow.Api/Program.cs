using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Entities.Users;
using TaskFlow.Repo.Data;
using TaskFlow.Repo.UserRepo;
using TaskFlow.Service.Authentication;
using TaskFlow.Service.Authentication.Options;
using TaskFlow.Service.Email.Options;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Service.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(
    optional: true,
    reloadOnChange: true);

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
    .Validate(
    settings => settings.RefreshTokenExpirationDays > 0,
    "Refresh token expiration must be greater than zero.")
    .ValidateOnStart();


var emailSection = builder.Configuration.GetSection(
    EmailSettings.SectionName);

builder.Services.AddOptions<EmailSettings>()
    .Bind(emailSection)
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.Host),
        "Email host is required.")
    .Validate(
        settings => settings.Port > 0,
        "Email port must be greater than zero.")
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.SenderEmail),
        "Sender email is required.")
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.Username),
        "Email username is required.")
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.Password),
        "Email password is required.")
    .Validate(
        settings => Uri.TryCreate(
            settings.ConfirmationUrl,
            UriKind.Absolute,
            out _),
        "A valid email confirmation URL is required.")
    .Validate(
        settings => Uri.TryCreate(
            settings.ResetPasswordUrl,
            UriKind.Absolute,
            out _),
        "A valid password reset URL is required.")
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

    options.SignIn.RequireConfirmedEmail = true;
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(2));

var authenticationBuilder = builder.Services
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

var googleClientId = builder.Configuration["ExternalAuthentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["ExternalAuthentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authenticationBuilder.AddGoogle("Google", options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
}

var microsoftClientId = builder.Configuration["ExternalAuthentication:Microsoft:ClientId"];
var microsoftClientSecret = builder.Configuration["ExternalAuthentication:Microsoft:ClientSecret"];
if (!string.IsNullOrWhiteSpace(microsoftClientId) && !string.IsNullOrWhiteSpace(microsoftClientSecret))
{
    authenticationBuilder.AddMicrosoftAccount("Microsoft", options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = microsoftClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
}

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
        options.AddPolicy(permission, policy => policy.RequireClaim(Permissions.ClaimType, permission));
});
builder.Services.AddCors(options => options.AddPolicy("Angular", policy => policy
    .WithOrigins("http://localhost:4200", "https://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.AddPolicy("authenticated", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
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
app.UseCors("Angular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "TaskFlow API is running.");

app.Run();
