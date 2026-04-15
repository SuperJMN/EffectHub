using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using EffectHub.Core.Models;

namespace EffectHub.Core.Services;

public class EffectPackager : IEffectPackager
{
    private const string EffectJsonEntry = "effect.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Result> Export(Effect effect, Stream outputStream)
    {
        try
        {
            using var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true);
            var entry = archive.CreateEntry(EffectJsonEntry, CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await JsonSerializer.SerializeAsync(entryStream, effect, JsonOptions);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to export effect: {ex.Message}");
        }
    }

    public async Task<Result<Effect>> Import(Stream inputStream)
    {
        try
        {
            using var archive = new ZipArchive(inputStream, ZipArchiveMode.Read, leaveOpen: true);
            var entry = archive.GetEntry(EffectJsonEntry);

            if (entry is null)
                return Result.Failure<Effect>($"Invalid package: missing {EffectJsonEntry}.");

            await using var entryStream = entry.Open();
            var effect = await JsonSerializer.DeserializeAsync<Effect>(entryStream, JsonOptions);

            if (effect is null)
                return Result.Failure<Effect>("Failed to deserialize effect.");

            var imported = effect with
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            return Result.Success(imported);
        }
        catch (Exception ex)
        {
            return Result.Failure<Effect>($"Failed to import effect: {ex.Message}");
        }
    }
}
