namespace EffectHub.Core.Models;

public record Effect
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public required string SkslCode { get; init; }
    public string AuthorAlias { get; init; } = "Anonymous";
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<UniformDefinition> Uniforms { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
