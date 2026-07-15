using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json;
using UserService.Api.Middleware;
using UserService.Application.Features.Auth;
using UserService.Application.Interfaces;
using UserService.Application.Mapping;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddControllers();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<UserDbContext>("PostgreSQL");

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("Jwt__Key")
    ?? builder.Configuration["Jwt:Key"]!);

    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

MapsterConfig.RegisterMappings();

Log.Information("Starting UserService...");

var app = builder.Build();

for (int retry = 0; retry < 10; retry++)
{
    try
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        db.Database.Migrate();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await UserDbSeeder.SeedAdminAsync(db, configuration);

        Log.Information("Database migration successful.");

        break;
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database not ready. Retrying...");
        Thread.Sleep(5000);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(x => new
            {
                Name = x.Key,
                Status = x.Value.Status.ToString()
            })
        }));
    }
});

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "UserService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}