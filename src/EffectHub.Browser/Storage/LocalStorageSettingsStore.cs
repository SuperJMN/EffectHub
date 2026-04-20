using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using Zafiro.Settings;

namespace EffectHub.Browser.Storage;

/// <summary>
/// ISettingsStore implementation backed by <c>window.localStorage</c> via JSImport.
/// Sync-safe because the JavaScript localStorage API is itself synchronous.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed partial class LocalStorageSettingsStore : ISettingsStore
{
    private const string KeyPrefix = "EffectHub:";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [JSImport("globalThis.localStorage.getItem")]
    internal static partial string? GetItem(string key);

    [JSImport("globalThis.localStorage.setItem")]
    internal static partial void SetItem(string key, string value);

    [JSImport("globalThis.localStorage.removeItem")]
    internal static partial void RemoveItem(string key);

    public Result<T> Load<T>(string path, Func<T> createDefault)
    {
        try
        {
            var key = KeyPrefix + path;
            var json = GetItem(key);
            if (string.IsNullOrEmpty(json))
            {
                var def = createDefault();
                var save = Save(path, def);
                return save.IsFailure ? Result.Failure<T>(save.Error) : Result.Success(def);
            }

            var loaded = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            return loaded is null
                ? Result.Failure<T>($"Invalid JSON in localStorage entry: {key}.")
                : Result.Success(loaded);
        }
        catch (Exception ex)
        {
            return Result.Failure<T>($"localStorage load error at {path}: {ex.Message}");
        }
    }

    public Result Save<T>(string path, T instance)
    {
        try
        {
            var key = KeyPrefix + path;
            var json = JsonSerializer.Serialize(instance, SerializerOptions);
            SetItem(key, json);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"localStorage save error at {path}: {ex.Message}");
        }
    }
}
