namespace EffectHub.Core.Configuration;

/// <summary>
/// Provides the current API base URL and notifies subscribers when it changes.
/// </summary>
public interface IApiEndpointProvider
{
    /// <summary>Current API base URL. Empty if not configured.</summary>
    string CurrentUrl { get; }

    /// <summary>True if a non-empty URL is currently configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Pushes the URL on subscription and on every change. Does not push duplicates.</summary>
    IObservable<string> Changes { get; }
}
