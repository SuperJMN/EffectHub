using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using EffectHub.Core.Models;
using EffectHub.Core.Services;
using SkiaSharp;

namespace EffectHub.Core.Compilation;

public partial class ShaderCompiler : IShaderCompiler
{
    public Result<ShaderCompilationResult> Compile(string skslCode)
    {
        if (string.IsNullOrWhiteSpace(skslCode))
        {
            return Result.Success(ShaderCompilationResult.Failure("Shader code is empty."));
        }

        try
        {
            var effect = SKRuntimeEffect.CreateShader(skslCode, out var errors);

            if (effect is null)
            {
                var errorMessage = errors ?? "Unknown compilation error";
                return Result.Success(ShaderCompilationResult.Failure(errorMessage));
            }

            effect.Dispose();

            var uniforms = ParseUniforms(skslCode);
            return Result.Success(ShaderCompilationResult.Success(uniforms));
        }
        catch (Exception ex)
        {
            return Result.Failure<ShaderCompilationResult>($"Unexpected error: {ex.Message}");
        }
    }

    private static IReadOnlyList<UniformDefinition> ParseUniforms(string skslCode)
    {
        var uniforms = new List<UniformDefinition>();
        var matches = UniformRegex().Matches(skslCode);

        foreach (Match match in matches)
        {
            var typeName = match.Groups["type"].Value;
            var name = match.Groups["name"].Value;

            // Skip child shader uniforms and built-in types
            if (typeName is "shader" or "blender" or "colorFilter")
            {
                continue;
            }

            var uniformType = MapType(typeName);
            if (uniformType.HasValue)
            {
                uniforms.Add(new UniformDefinition
                {
                    Name = name,
                    Type = uniformType.Value,
                    DefaultValue = GetDefaultForType(uniformType.Value),
                    Min = uniformType.Value == UniformType.Float ? 0.0 : null,
                    Max = uniformType.Value == UniformType.Float ? 1.0 : null
                });
            }
        }

        return uniforms;
    }

    private static UniformType? MapType(string glslType) => glslType switch
    {
        "float" or "half" => UniformType.Float,
        "float2" or "half2" or "vec2" => UniformType.Float2,
        "float3" or "half3" or "vec3" => UniformType.Float3,
        "float4" or "half4" or "vec4" => UniformType.Float4,
        "int" => UniformType.Int,
        "bool" => UniformType.Bool,
        _ => null
    };

    private static double GetDefaultForType(UniformType type) => type switch
    {
        UniformType.Float => 0.5,
        UniformType.Int => 0,
        UniformType.Bool => 0,
        _ => 0
    };

    [GeneratedRegex(@"uniform\s+(?<type>\w+)\s+(?<name>\w+)\s*;", RegexOptions.Compiled)]
    private static partial Regex UniformRegex();
}
