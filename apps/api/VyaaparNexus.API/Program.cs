using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Serilog;
using Serilog.Formatting.Json;
using System;
using System.IO;
using VyaaparNexus.API.Middlewares;
using VyaaparNexus.Application;
using VyaaparNexus.Infrastructure;
using VyaaparNexus.Infrastructure.Observability;
using VyaaparNexus.Infrastructure.Persistence;
using VyaaparNexus.Infrastructure.Persistence.Seed;

// Configure Serilog bootstrap logger (before DI is built)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting VyaaparNexus API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new JsonFormatter())
        // Phase 5: wire the StreamSnapshotStore sink — resolved after DI is built
        // via WriteTo.Sink() with the singleton resolved from the service provider.
        .WriteTo.Sink(services.GetRequiredService<StreamSnapshotStore>()));

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddHealthChecks();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // Seed Database
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "VyaaparNexus.Infrastructure"));
            if (!Directory.Exists(basePath))
            {
                // Fallback for docker or other environments
                basePath = AppContext.BaseDirectory;
            }
            await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(context.Database);
            await DatabaseSeeder.SeedAsync(context, basePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating or seeding the database.");
        }
    }

    // Configure the HTTP request pipeline.
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowFrontend");

    app.UseRouting();

    app.UseHttpMetrics(); // prometheus-net

    app.UseMiddleware<ApiKeyMiddleware>(); // Authentication

    app.UseAuthorization();

    app.MapControllers();
    
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    app.MapMetrics(); // prometheus-net

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }