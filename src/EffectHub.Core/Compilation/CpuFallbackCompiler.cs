using System.Reflection;
using System.Runtime.Loader;
using EffectHub.Core.Rendering;
using EffectHub.Core.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EffectHub.Core.Compilation;

public class CpuFallbackCompiler : ICpuFallbackCompiler
{
    private static readonly string WrapperTemplate =
        """
        using SkiaSharp;
        using EffectHub.Core.Rendering;

        public class __CpuFallbackImpl : ICpuFallbackRenderer
        {{
            public void Render(
                SKCanvas canvas,
                SKImage? contentImage,
                SKRect rect,
                float width,
                float height,
                float time,
                float[] floats,
                SKColor[] colors,
                bool[] bools,
                int[] ints)
            {{
        {0}
            }}
        }}
        """;

    private static readonly MetadataReference[] References = BuildReferences();

    public CpuFallbackCompilationResult Compile(string csharpCode)
    {
        if (string.IsNullOrWhiteSpace(csharpCode))
        {
            return CpuFallbackCompilationResult.Failure("C# code is empty.");
        }

        try
        {
            var fullSource = string.Format(WrapperTemplate, csharpCode);

            var syntaxTree = CSharpSyntaxTree.ParseText(fullSource);

            var compilation = CSharpCompilation.Create(
                assemblyName: $"CpuFallback_{Guid.NewGuid():N}",
                syntaxTrees: [syntaxTree],
                references: References,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Release)
                    .WithAllowUnsafe(false));

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                var errors = result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage())
                    .ToList();

                return CpuFallbackCompilationResult.Failure(string.Join("\n", errors));
            }

            ms.Seek(0, SeekOrigin.Begin);

            var loadContext = new CollectibleLoadContext();
            var assembly = loadContext.LoadFromStream(ms);
            var type = assembly.GetType("__CpuFallbackImpl")!;
            var renderer = (ICpuFallbackRenderer)Activator.CreateInstance(type)!;

            return CpuFallbackCompilationResult.Success(renderer);
        }
        catch (Exception ex)
        {
            return CpuFallbackCompilationResult.Failure($"Unexpected error: {ex.Message}");
        }
    }

    private static MetadataReference[] BuildReferences()
    {
        var references = new List<MetadataReference>();

        // Core runtime assemblies
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        foreach (var name in new[] { "System.Runtime.dll", "System.Private.CoreLib.dll", "netstandard.dll" })
        {
            var path = Path.Combine(runtimeDir, name);
            if (File.Exists(path))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        // SkiaSharp
        references.Add(MetadataReference.CreateFromFile(typeof(SkiaSharp.SKCanvas).Assembly.Location));

        // Our contract (ICpuFallbackRenderer)
        references.Add(MetadataReference.CreateFromFile(typeof(ICpuFallbackRenderer).Assembly.Location));

        return references.ToArray();
    }

    /// <summary>
    /// Collectible assembly load context that allows compiled assemblies to be garbage collected.
    /// </summary>
    private sealed class CollectibleLoadContext() : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }
}
