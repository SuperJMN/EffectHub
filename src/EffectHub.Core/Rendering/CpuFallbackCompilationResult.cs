namespace EffectHub.Core.Rendering;

public record CpuFallbackCompilationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public ICpuFallbackRenderer? Renderer { get; init; }

    public static CpuFallbackCompilationResult Success(ICpuFallbackRenderer renderer) =>
        new() { IsSuccess = true, Renderer = renderer };

    public static CpuFallbackCompilationResult Failure(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };
}
