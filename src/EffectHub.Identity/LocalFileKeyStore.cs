namespace EffectHub.Identity;

/// <summary>
/// Stores the Nostr keypair as an nsec string in a local file (~/.effecthub/identity.json).
/// </summary>
public class LocalFileKeyStore : IKeyStore
{
    private readonly string filePath;

    public LocalFileKeyStore(string? basePath = null)
    {
        var dir = basePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".effecthub");
        Directory.CreateDirectory(dir);
        filePath = Path.Combine(dir, "identity.json");
    }

    public async Task<string?> LoadAsync()
    {
        if (!File.Exists(filePath))
            return null;
        return await File.ReadAllTextAsync(filePath);
    }

    public async Task SaveAsync(string data)
    {
        await File.WriteAllTextAsync(filePath, data);
    }

    public Task DeleteAsync()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }
}
