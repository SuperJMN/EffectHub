namespace EffectHub.Core.Models;

public record ShaderCompilationResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<UniformDefinition> DetectedUniforms { get; init; } = [];

    public static ShaderCompilationResult Success(IReadOnlyList<UniformDefinition> uniforms) =>
        new() { IsSuccess = true, DetectedUniforms = uniforms };

    public static ShaderCompilationResult Failure(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };
}
