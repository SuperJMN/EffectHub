using System.Text.Json;

namespace EffectHub.Identity;

/// <summary>
/// In-memory key store with no persistence. Used as a fallback when no platform-specific store is available.
/// </summary>
public class InMemoryKeyStore : IKeyStore
{
    private string? data;

    public Task<string?> LoadAsync() => Task.FromResult(data);

    public Task SaveAsync(string value)
    {
        data = value;
        return Task.CompletedTask;
    }

    public Task DeleteAsync()
    {
        data = null;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Manages Nostr identity: generates, stores, exports and imports keypairs.
/// Provides signing capability for API authentication.
/// </summary>
public class NostrIdentityService
{
    private readonly IKeyStore keyStore;
    private NostrKeyPair? currentKeyPair;
    private string alias = "Anonymous";

    public NostrIdentityService(IKeyStore keyStore)
    {
        this.keyStore = keyStore;
    }

    public NostrKeyPair? CurrentKeyPair => currentKeyPair;
    public string PublicKeyHex => currentKeyPair?.PublicKeyHex ?? "";
    public string Npub => currentKeyPair?.Npub ?? "";
    public bool IsLoaded => currentKeyPair is not null;

    public string Alias
    {
        get => alias;
        set => alias = value;
    }

    /// <summary>
    /// Loads existing keypair from store, or generates a new one.
    /// </summary>
    public async Task<NostrKeyPair> GetOrCreateKeyPairAsync()
    {
        if (currentKeyPair is not null)
            return currentKeyPair;

        var stored = await keyStore.LoadAsync();
        if (stored is not null)
        {
            try
            {
                var state = JsonSerializer.Deserialize<IdentityState>(stored);
                if (state is not null)
                {
                    currentKeyPair = NostrCrypto.FromNsec(state.Nsec);
                    alias = state.Alias ?? "Anonymous";
                    return currentKeyPair;
                }
            }
            catch
            {
                // Corrupted store — generate fresh key
            }
        }

        currentKeyPair = NostrCrypto.GenerateKeyPair();
        await PersistAsync();
        return currentKeyPair;
    }

    public async Task ImportNsecAsync(string nsec)
    {
        currentKeyPair = NostrCrypto.FromNsec(nsec);
        await PersistAsync();
    }

    public string ExportNsec()
    {
        if (currentKeyPair is null)
            throw new InvalidOperationException("No keypair loaded.");
        return currentKeyPair.Nsec;
    }

    public async Task SetAliasAsync(string newAlias)
    {
        alias = newAlias;
        if (currentKeyPair is not null)
            await PersistAsync();
    }

    public string Sign(string message)
    {
        if (currentKeyPair is null)
            throw new InvalidOperationException("No keypair loaded.");

        var hash = NostrCrypto.HashMessage(message);
        var sig = NostrCrypto.SignSchnorr(currentKeyPair.PrivateKey, hash);
        return Convert.ToHexString(sig).ToLowerInvariant();
    }

    private async Task PersistAsync()
    {
        if (currentKeyPair is null) return;
        var state = new IdentityState(currentKeyPair.Nsec, alias);
        var json = JsonSerializer.Serialize(state);
        await keyStore.SaveAsync(json);
    }

    private record IdentityState(string Nsec, string? Alias);
}
