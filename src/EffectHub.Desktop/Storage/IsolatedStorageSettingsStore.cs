using System.IO.IsolatedStorage;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using Zafiro.Settings;

namespace EffectHub.Desktop.Storage;

/// <summary>
/// ISettingsStore implementation backed by <see cref="IsolatedStorageFile"/>.
/// Works on Desktop (Windows/Linux/macOS) and Mobile (Android/iOS) without hard-coded paths.
/// </summary>
public sealed class IsolatedStorageSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object gate = new();

    public Result<T> Load<T>(string path, Func<T> createDefault)
    {
        try
        {
            lock (gate)
            {
                using var store = IsolatedStorageFile.GetStore(
                    IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly,
                    null, null);

                if (!store.FileExists(path))
                {
                    var def = createDefault();
                    var save = SaveCore(store, path, def);
                    return save.IsFailure ? Result.Failure<T>(save.Error) : Result.Success(def);
                }

                using var stream = store.OpenFile(path, FileMode.Open, FileAccess.Read);
                var loaded = JsonSerializer.Deserialize<T>(stream, SerializerOptions);
                return loaded is null
                    ? Result.Failure<T>($"Invalid JSON in isolated storage entry: {path}.")
                    : Result.Success(loaded);
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<T>($"IsolatedStorage load error at {path}: {ex.Message}");
        }
    }

    public Result Save<T>(string path, T instance)
    {
        try
        {
            lock (gate)
            {
                using var store = IsolatedStorageFile.GetStore(
                    IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly,
                    null, null);
                return SaveCore(store, path, instance);
            }
        }
        catch (Exception ex)
        {
            return Result.Failure($"IsolatedStorage save error at {path}: {ex.Message}");
        }
    }

    private static Result SaveCore<T>(IsolatedStorageFile store, string path, T instance)
    {
        try
        {
            using var stream = store.OpenFile(path, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(stream, instance, SerializerOptions);
            stream.Flush();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"IsolatedStorage write error at {path}: {ex.Message}");
        }
    }
}
