using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProcurementApi.Data;
using ProcurementApi.Middleware;
using ProcurementApi.Repositories;
using ProcurementApi.Services;
using ProcurementApi.Services.Vendors;

var builder = WebApplication.CreateBuilder(args);

// ---- Services ------------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Procurement API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {token}"
    });
    options.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<ProcurementDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")
        ?? "Host=localhost;Port=5432;Database=procurement;Username=postgres;Password=postgres"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPurchaseRequestRepository, PurchaseRequestRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddScoped<IBudgetService, SimulatedBudgetService>();
builder.Services.AddSingleton<IVendorGateway, TechSourceGateway>();
builder.Services.AddSingleton<IVendorGateway, OfficeMartGateway>();
builder.Services.AddSingleton<IVendorGateway, EnterpriseSupplyGateway>();
builder.Services.AddSingleton<IVendorGatewayFactory, VendorGatewayFactory>();
builder.Services.AddScoped<INotificationService, NotificationService>();
// Observer pattern: PurchaseRequestService fans events out to every IRequestEventObserver
// registered here — adding a second reactor (audit log, email, ...) is one more line here,
// no changes anywhere else (OCP).
builder.Services.AddScoped<IRequestEventObserver, NotificationObserver>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-signing-key-change-me-please-32-bytes-min";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ProcurementApi";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtIssuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

const string CorsPolicy = "FrontendCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// ---- Pipeline --------------------------------------------------------

// Global exception handling is first so every downstream failure (auth, model binding,
// business logic) comes back as a consistent problem+json body (Section 8, docs/ANALYSIS.md).
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();
