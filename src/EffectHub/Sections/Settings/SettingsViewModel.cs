using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using EffectHub.Core.Configuration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Zafiro.Settings;

namespace EffectHub.Sections.Settings;

public partial class SettingsViewModel : ReactiveObject
{
    private readonly ISettings<EffectHubSettings> settings;
    private readonly IApiEndpointProvider endpoint;

    [Reactive] private string apiBaseUrl = "";
    [Reactive] private string statusMessage = "";
    [Reactive] private bool isBusy;

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> TestCommand { get; }

    public SettingsViewModel(ISettings<EffectHubSettings> settings, IApiEndpointProvider endpoint)
    {
        this.settings = settings;
        this.endpoint = endpoint;

        var current = settings.Get();
        if (current.IsSuccess)
        {
            apiBaseUrl = current.Value.ApiBaseUrl ?? string.Empty;
        }

        var canSave = this.WhenAnyValue(x => x.ApiBaseUrl, x => x.IsBusy,
            (url, busy) => !busy && IsLikelyValidUrl(url));

        SaveCommand = ReactiveCommand.Create(Save, canSave);
        TestCommand = ReactiveCommand.CreateFromTask(TestAsync, canSave);
    }

    public string CurrentEffectiveUrl => endpoint.CurrentUrl;

    private void Save()
    {
        var trimmed = (ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var result = settings.Update(s => s with { ApiBaseUrl = trimmed });
        StatusMessage = result.IsSuccess
            ? $"Saved. Active URL: {trimmed}"
            : $"Save failed: {result.Error}";
    }

    private async Task TestAsync()
    {
        var trimmed = (ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        IsBusy = true;
        StatusMessage = "Testing...";
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(trimmed), Timeout = TimeSpan.FromSeconds(8) };
            var response = await client.GetAsync("/api/effects?pageSize=1");
            StatusMessage = response.IsSuccessStatusCode
                ? $"OK ({(int)response.StatusCode})."
                : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool IsLikelyValidUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
