namespace EffectHub.Core.Configuration;

/// <summary>
/// User-editable application settings persisted via Zafiro.Settings.
/// Stored as JSON in IsolatedStorage (Desktop/Mobile) or localStorage (WASM).
/// </summary>
public record EffectHubSettings
{
    /// <summary>
    /// Base URL of the EffectHub API (e.g. "http://localhost:5120" or "https://api.example.com").
    /// Empty means "not configured".
    /// </summary>
    public string ApiBaseUrl { get; init; } = string.Empty;
}
