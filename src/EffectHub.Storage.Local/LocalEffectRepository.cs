using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using DynamicData;
using EffectHub.Core.Models;
using EffectHub.Core.Services;

namespace EffectHub.Storage.Local;

public class LocalEffectRepository : IEffectRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string basePath;
    private readonly SourceCache<Effect, string> cache = new(e => e.Id);

    public LocalEffectRepository(string? basePath = null)
    {
        this.basePath = basePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".effecthub", "effects");

        Directory.CreateDirectory(this.basePath);
    }

    public IObservable<IChangeSet<Effect, string>> Connect() => cache.Connect();

    public async Task<Result<Effect>> GetById(string id)
    {
        var jsonPath = GetEffectJsonPath(id);
        if (!File.Exists(jsonPath))
        {
            return Result.Failure<Effect>($"Effect '{id}' not found.");
        }

        return await LoadEffect(jsonPath);
    }

    public async Task<Result> Save(Effect effect, Stream? previewImage = null, Stream? icon = null)
    {
        try
        {
            var dir = GetEffectDirectory(effect.Id);
            Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(effect, JsonOptions);
            await File.WriteAllTextAsync(GetEffectJsonPath(effect.Id), json);

            if (previewImage is not null)
            {
                await using var fs = File.Create(GetPreviewPath(effect.Id));
                await previewImage.CopyToAsync(fs);
            }

            if (icon is not null)
            {
                await using var fs = File.Create(GetIconPath(effect.Id));
                await icon.CopyToAsync(fs);
            }

            cache.AddOrUpdate(effect);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to save effect: {ex.Message}");
        }
    }

    public Task<Result> Delete(string id)
    {
        try
        {
            var dir = GetEffectDirectory(id);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }

            cache.RemoveKey(id);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to delete effect: {ex.Message}"));
        }
    }

    public Task<Result<Stream>> GetPreviewImage(string id)
    {
        var path = GetPreviewPath(id);
        return Task.FromResult(GetFileStream(path));
    }

    public Task<Result<Stream>> GetIcon(string id)
    {
        var path = GetIconPath(id);
        return Task.FromResult(GetFileStream(path));
    }

    public async Task<Result<IReadOnlyList<Effect>>> GetAll()
    {
        try
        {
            var effects = new List<Effect>();

            if (!Directory.Exists(basePath))
            {
                return Result.Success<IReadOnlyList<Effect>>(effects);
            }

            foreach (var dir in Directory.GetDirectories(basePath))
            {
                var jsonPath = Path.Combine(dir, "effect.json");
                if (!File.Exists(jsonPath))
                {
                    continue;
                }

                var result = await LoadEffect(jsonPath);
                if (result.IsSuccess)
                {
                    effects.Add(result.Value);
                }
            }

            return Result.Success<IReadOnlyList<Effect>>(effects);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<Effect>>($"Failed to load effects: {ex.Message}");
        }
    }

    public async Task LoadAll()
    {
        var result = await GetAll();
        if (result.IsSuccess)
        {
            cache.Edit(updater =>
            {
                updater.Clear();
                updater.AddOrUpdate(result.Value);
            });
        }
    }

    private async Task<Result<Effect>> LoadEffect(string jsonPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            var effect = JsonSerializer.Deserialize<Effect>(json, JsonOptions);
            return effect is not null
                ? Result.Success(effect)
                : Result.Failure<Effect>("Failed to deserialize effect.");
        }
        catch (Exception ex)
        {
            return Result.Failure<Effect>($"Failed to load effect: {ex.Message}");
        }
    }

    private static Result<Stream> GetFileStream(string path)
    {
        if (!File.Exists(path))
        {
            return Result.Failure<Stream>("File not found.");
        }

        Stream stream = File.OpenRead(path);
        return Result.Success(stream);
    }

    private string GetEffectDirectory(string id) => Path.Combine(basePath, id);
    private string GetEffectJsonPath(string id) => Path.Combine(GetEffectDirectory(id), "effect.json");
    private string GetPreviewPath(string id) => Path.Combine(GetEffectDirectory(id), "preview.png");
    private string GetIconPath(string id) => Path.Combine(GetEffectDirectory(id), "icon.png");
}
