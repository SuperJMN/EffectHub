namespace EffectHub.Core.Models;

public enum TestSurfaceKind
{
    LinearGradient,
    RadialGradient,
    SolidColor,
    CustomImage
}

public record TestSurfaceConfig
{
    public TestSurfaceKind Kind { get; init; } = TestSurfaceKind.LinearGradient;
    public string? CustomImagePath { get; init; }
}
