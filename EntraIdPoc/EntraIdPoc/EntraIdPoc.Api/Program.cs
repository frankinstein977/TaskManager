using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OpenApi.Models;
using EntraIdPoc.Api.Data;
using EntraIdPoc.Api.Middleware;
using EntraIdPoc.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURATION ====================
var entraConfig = builder.Configuration.GetSection("AzureAd").Get<EntraIdPoc.Api.Configuration.EntraConfig>()
    ?? throw new InvalidOperationException("AzureAd configuration section is missing");

// ==================== SERVICES ====================

// 1. Microsoft Identity Web (Entra ID authentication)
// Handles JWT Bearer token validation for API calls
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Also add JWT Bearer for API-only scenarios (SPA calling API)
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(
        builder.Configuration.GetSection("AzureAd"),
        subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);

// 2. Authorization with custom policies
builder.Services.AddAuthorization(options => AuthorizationPolicies.Configure(options));

// 3. PostgreSQL Database via Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("EntraIdPoc.Api")
    ));

// 4. Application Services (Dependency Injection)
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<IUserResolutionService, UserResolutionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IProjectService, ProjectService>();

// 5. HTTP Context accessor (for IP address in audit logs)
builder.Services.AddHttpContextAccessor();

// 6. Controllers + Razor Pages
builder.Services.AddControllers();
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

// 7. CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173" };

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 8. Swagger with JWT auth support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Entra ID POC API",
        Version = "v1",
        Description = "ASP.NET Core API with Microsoft Entra ID authentication and PostgreSQL"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token."
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

var app = builder.Build();

// ==================== MIDDLEWARE PIPELINE ====================
// Order is critical: Authentication -> UserResolution -> Authorization -> Endpoints

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Entra ID POC v1"));

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// 1. Authentication: Validates JWT signature against Microsoft JWKS
app.UseAuthentication();

// 2. UserResolution: Bridges Entra claims to DB user (auto-provisioning)
app.UseMiddleware<UserResolutionMiddleware>();

// 3. Authorization: Checks app_role claims against policies
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
