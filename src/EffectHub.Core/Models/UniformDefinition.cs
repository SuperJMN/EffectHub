namespace EffectHub.Core.Models;

public record UniformDefinition
{
    public required string Name { get; init; }
    public required UniformType Type { get; init; }
    public double DefaultValue { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
}
