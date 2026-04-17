namespace EffectHub.Identity;

/// <summary>
/// Abstraction for persisting a Nostr keypair across sessions.
/// </summary>
public interface IKeyStore
{
    Task<string?> LoadAsync();
    Task SaveAsync(string data);
    Task DeleteAsync();
}
