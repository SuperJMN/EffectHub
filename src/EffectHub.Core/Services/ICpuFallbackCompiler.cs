using EffectHub.Core.Rendering;

namespace EffectHub.Core.Services;

public interface ICpuFallbackCompiler
{
    CpuFallbackCompilationResult Compile(string csharpCode);
}
