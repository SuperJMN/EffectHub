using CSharpFunctionalExtensions;
using EffectHub.Core.Models;

namespace EffectHub.Core.Services;

public interface IShaderCompiler
{
    Result<ShaderCompilationResult> Compile(string skslCode);
}
