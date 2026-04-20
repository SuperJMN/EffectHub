using System.Reactive.Linq;
using Zafiro.Settings;

namespace EffectHub.Core.Configuration;

/// <summary>
/// IApiEndpointProvider backed by Zafiro <see cref="ISettings{T}"/>.
/// </summary>
public sealed class ApiEndpointProvider : IApiEndpointProvider
{
    private readonly ISettings<EffectHubSettings> settings;

    public ApiEndpointProvider(ISettings<EffectHubSettings> settings)
    {
        this.settings = settings;
        // Trigger initial load so ReplaySubject buffers the current value for late subscribers.
        _ = settings.Get();
        Changes = settings.Changes
            .Select(s => s.ApiBaseUrl ?? string.Empty)
            .DistinctUntilChanged();
    }

    public string CurrentUrl
    {
        get
        {
            var current = settings.Get();
            return current.IsSuccess ? (current.Value.ApiBaseUrl ?? string.Empty) : string.Empty;
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(CurrentUrl);

    public IObservable<string> Changes { get; }
}
