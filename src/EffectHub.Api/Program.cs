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
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

app.UseCors();
app.MapEffectsEndpoints();

app.Run();
