using System.Text.Json;
using System.Text.Json.Serialization;
using EffectHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EffectHub.Api.Data;

public class EffectEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string SkslCode { get; set; } = "";
    public string? CpuFallbackCode { get; set; }
    public string AuthorPubKey { get; set; } = "";
    public string AuthorAlias { get; set; } = "Anonymous";
    public string TagsJson { get; set; } = "[]";
    public string UniformsJson { get; set; } = "[]";
    public string? UniformValuesJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[]? PreviewImage { get; set; }
    public byte[]? Icon { get; set; }
}

public class EffectHubDbContext : DbContext
{
    public DbSet<EffectEntity> Effects => Set<EffectEntity>();

    public EffectHubDbContext(DbContextOptions<EffectHubDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dtoToTicks = new ValueConverter<DateTimeOffset, long>(
            v => v.UtcTicks,
            v => new DateTimeOffset(v, TimeSpan.Zero));

        modelBuilder.Entity<EffectEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AuthorPubKey);
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.CreatedAt).HasConversion(dtoToTicks);
            entity.Property(e => e.UpdatedAt).HasConversion(dtoToTicks);
        });
    }
}

public static class EffectMapping
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static Effect ToModel(this EffectEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        SkslCode = entity.SkslCode,
        CpuFallbackCode = entity.CpuFallbackCode,
        AuthorPubKey = entity.AuthorPubKey,
        AuthorAlias = entity.AuthorAlias,
        Tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson, JsonOptions) ?? [],
        Uniforms = JsonSerializer.Deserialize<List<UniformDefinition>>(entity.UniformsJson, JsonOptions) ?? [],
        UniformValues = entity.UniformValuesJson is not null
            ? JsonSerializer.Deserialize<Dictionary<string, double>>(entity.UniformValuesJson, JsonOptions)
            : null,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static EffectEntity ToEntity(this Effect effect) => new()
    {
        Id = effect.Id,
        Name = effect.Name,
        Description = effect.Description,
        SkslCode = effect.SkslCode,
        CpuFallbackCode = effect.CpuFallbackCode,
        AuthorPubKey = effect.AuthorPubKey,
        AuthorAlias = effect.AuthorAlias,
        TagsJson = JsonSerializer.Serialize(effect.Tags, JsonOptions),
        UniformsJson = JsonSerializer.Serialize(effect.Uniforms, JsonOptions),
        UniformValuesJson = effect.UniformValues is not null
            ? JsonSerializer.Serialize(effect.UniformValues, JsonOptions)
            : null,
        CreatedAt = effect.CreatedAt,
        UpdatedAt = effect.UpdatedAt
    };
}
