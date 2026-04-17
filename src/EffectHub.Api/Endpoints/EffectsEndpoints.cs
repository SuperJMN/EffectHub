using EffectHub.Api.Auth;
using EffectHub.Api.Data;
using EffectHub.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EffectHub.Api.Endpoints;

public static class EffectsEndpoints
{
    public static void MapEffectsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/effects");

        group.MapGet("/", GetAll);
        group.MapGet("/{id}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id}", Update);
        group.MapDelete("/{id}", Delete);
        group.MapGet("/{id}/preview", GetPreview);
        group.MapPost("/{id}/preview", UploadPreview);
        group.MapGet("/{id}/icon", GetIcon);

        app.MapGet("/api/users/{pubkey}/effects", GetByAuthor);
    }

    private static async Task<IResult> GetAll(
        EffectHubDbContext db,
        [FromQuery] string? search = null,
        [FromQuery] string? tag = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = db.Effects.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(e =>
                e.Name.ToLower().Contains(s) ||
                e.Description.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(e => e.TagsJson.Contains(tag));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => e.ToModel())
            .ToListAsync();

        return Results.Ok(new { items, total, page, pageSize });
    }

    private static async Task<IResult> GetById(string id, EffectHubDbContext db)
    {
        var entity = await db.Effects.FindAsync(id);
        return entity is null
            ? Results.NotFound(new { error = $"Effect '{id}' not found." })
            : Results.Ok(entity.ToModel());
    }

    private static async Task<IResult> Create(Effect effect, EffectHubDbContext db)
    {
        var entity = effect.ToEntity();
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString("N");

        db.Effects.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/api/effects/{entity.Id}", entity.ToModel());
    }

    private static async Task<IResult> Update(string id, Effect effect, HttpContext httpContext, EffectHubDbContext db)
    {
        var existing = await db.Effects.FindAsync(id);
        if (existing is null)
            return Results.NotFound(new { error = $"Effect '{id}' not found." });

        // If the effect has an author, verify ownership
        if (!string.IsNullOrEmpty(existing.AuthorPubKey))
        {
            var auth = NostrAuth.Verify(httpContext);
            if (!auth.IsValid)
                return Results.Json(new { error = auth.Error }, statusCode: 401);
            if (!string.Equals(auth.PubKey, existing.AuthorPubKey, StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { error = "You are not the author of this effect." }, statusCode: 403);
        }

        existing.Name = effect.Name;
        existing.Description = effect.Description;
        existing.SkslCode = effect.SkslCode;
        existing.CpuFallbackCode = effect.CpuFallbackCode;
        existing.AuthorAlias = effect.AuthorAlias;
        existing.TagsJson = System.Text.Json.JsonSerializer.Serialize(effect.Tags);
        existing.UniformsJson = System.Text.Json.JsonSerializer.Serialize(effect.Uniforms);
        existing.UniformValuesJson = effect.UniformValues is not null
            ? System.Text.Json.JsonSerializer.Serialize(effect.UniformValues)
            : null;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(existing.ToModel());
    }

    private static async Task<IResult> Delete(string id, HttpContext httpContext, EffectHubDbContext db)
    {
        var existing = await db.Effects.FindAsync(id);
        if (existing is null)
            return Results.NotFound(new { error = $"Effect '{id}' not found." });

        if (!string.IsNullOrEmpty(existing.AuthorPubKey))
        {
            var auth = NostrAuth.Verify(httpContext);
            if (!auth.IsValid)
                return Results.Json(new { error = auth.Error }, statusCode: 401);
            if (!string.Equals(auth.PubKey, existing.AuthorPubKey, StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { error = "You are not the author of this effect." }, statusCode: 403);
        }

        db.Effects.Remove(existing);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetPreview(string id, EffectHubDbContext db)
    {
        var entity = await db.Effects.FindAsync(id);
        if (entity?.PreviewImage is null)
            return Results.NotFound();
        return Results.File(entity.PreviewImage, "image/png");
    }

    private static async Task<IResult> UploadPreview(string id, HttpContext httpContext, EffectHubDbContext db)
    {
        var entity = await db.Effects.FindAsync(id);
        if (entity is null)
            return Results.NotFound();

        if (!string.IsNullOrEmpty(entity.AuthorPubKey))
        {
            var auth = NostrAuth.Verify(httpContext);
            if (!auth.IsValid)
                return Results.Json(new { error = auth.Error }, statusCode: 401);
            if (!string.Equals(auth.PubKey, entity.AuthorPubKey, StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { error = "You are not the author of this effect." }, statusCode: 403);
        }

        using var ms = new MemoryStream();
        await httpContext.Request.Body.CopyToAsync(ms);
        entity.PreviewImage = ms.ToArray();
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> GetIcon(string id, EffectHubDbContext db)
    {
        var entity = await db.Effects.FindAsync(id);
        if (entity?.Icon is null)
            return Results.NotFound();
        return Results.File(entity.Icon, "image/png");
    }

    private static async Task<IResult> GetByAuthor(string pubkey, EffectHubDbContext db, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = db.Effects.Where(e => e.AuthorPubKey == pubkey.ToLower());
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => e.ToModel())
            .ToListAsync();

        return Results.Ok(new { items, total, page, pageSize });
    }
}
