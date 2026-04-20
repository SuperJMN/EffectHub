using EffectHub.Api.Data;
using EffectHub.Api.Endpoints;
using EffectHub.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EffectHubDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=effecthub.db"));

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // No explicit origins configured (typical in Development): allow any origin so the
            // Browser/Desktop heads can talk to the API without extra setup. For production set
            // the Cors:AllowedOrigins array in appsettings.json.
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EffectHubDbContext>();
    await db.Database.EnsureCreatedAsync();

    // Seed gallery with built-in effects if empty
    if (!await db.Effects.AnyAsync())
    {
        foreach (var seed in SeedEffects.GetAll())
        {
            db.Effects.Add(seed.ToEntity());
        }
        await db.SaveChangesAsync();
    }
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var message = feature?.Error.Message ?? "Internal server error";
        await context.Response.WriteAsJsonAsync(new { error = message });
    });
});

app.UseCors();
app.MapEffectsEndpoints();

app.Run();
