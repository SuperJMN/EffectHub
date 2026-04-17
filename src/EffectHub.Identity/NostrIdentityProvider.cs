using EffectHub.Core.Services;

namespace EffectHub.Identity;

/// <summary>
/// Bridges NostrIdentityService to the IIdentityProvider and IIdentitySigner interfaces used by the app.
/// </summary>
public class NostrIdentityProvider : IIdentityProvider, IIdentitySigner
{
    private readonly NostrIdentityService service;

    public NostrIdentityProvider(NostrIdentityService service)
    {
        this.service = service;
    }

    public string CurrentAlias => service.Alias;
    public string CurrentId => service.PublicKeyHex;

    public Task<string> SignAsync(string message) =>
        Task.FromResult(service.Sign(message));
}
