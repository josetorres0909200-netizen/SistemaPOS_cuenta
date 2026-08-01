using Microsoft.EntityFrameworkCore;
using SistemaPOS.API.Data;
using Serilog;
using SistemaPOS.GVG.API.Services;
using SistemaPOS.GVG.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SistemaPOS.GVG.API.Middleware;
using AspNetCoreRateLimit;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using FluentValidation;
using SistemaPOS.GVG.API.Utilities;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/sistemaPOS-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicación SistemaPOS API");

    var builder = WebApplication.CreateBuilder(args);

    // Reemplazar logging por defecto con Serilog
    builder.Host.UseSerilog();

    // 1. Configuración de Base de Datos
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' no configurada");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));

    // 2. Configuración de Rate Limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // 3. Configuración de Autenticación JWT
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT Key no configurada");

    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SistemaPOS.API";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SistemaPOS.Clients";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("Autenticación JWT fallida: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Information("Token JWT validado para usuario: {User}", 
                    context.Principal?.Identity?.Name ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // 4. Registro de Servicios
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IProductoService, ProductoService>();
    builder.Services.AddScoped<IVentaService, VentaService>();
    builder.Services.AddScoped<PasswordMigrationUtility>(); // Utilidad de migración

    // 5. Manejo de Excepciones
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // 6. Registro de Controladores
    builder.Services.AddControllers();

    // 6b. FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // 7. Health Checks
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionString,
            name: "database",
            tags: new[] { "db", "sql", "sqlserver" });

    // 8. Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // 9. Configuración de CORS segura
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowDesktopApp", policyBuilder =>
        {
            policyBuilder
                .WithOrigins("http://localhost:5275", "https://localhost:7269")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Pipeline de Middleware
    app.UseExceptionHandler();

    // Configuración del entorno de desarrollo
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SistemaPOS API v1");
            c.RoutePrefix = string.Empty; // Swagger en raíz
        });
    }

    app.UseHttpsRedirection();

    // Middleware personalizado
    app.UseMiddleware<PerformanceMonitoringMiddleware>();

    // Rate Limiting
    app.UseIpRateLimiting();

    // CORS
    app.UseCors("AllowDesktopApp");

    // Autenticación y Autorización
    app.UseAuthentication();
    app.UseAuthorization();

    // Health Checks
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapControllers();

    Log.Information("Aplicación configurada correctamente. Iniciando servidor...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar");
}
finally
{
    Log.CloseAndFlush();
}
