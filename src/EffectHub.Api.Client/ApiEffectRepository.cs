using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using DynamicData;
using EffectHub.Core.Models;
using EffectHub.Core.Services;

namespace EffectHub.Api.Client;

/// <summary>
/// IEffectRepository implementation that communicates with the EffectHub API.
/// Mutations are signed with the user's Nostr identity.
/// </summary>
public class ApiEffectRepository : IEffectRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient httpClient;
    private readonly IIdentitySigner? signer;
    private readonly IIdentityProvider? identity;
    private readonly SourceCache<Effect, string> cache = new(e => e.Id);

    public ApiEffectRepository(HttpClient httpClient, IIdentitySigner? signer = null, IIdentityProvider? identity = null)
    {
        this.httpClient = httpClient;
        this.signer = signer;
        this.identity = identity;
    }

    public IObservable<IChangeSet<Effect, string>> Connect() => cache.Connect();

    public async Task<Result<Effect>> GetById(string id)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/effects/{id}");
            if (!response.IsSuccessStatusCode)
                return Result.Failure<Effect>($"API error: {response.StatusCode}");

            var effect = await response.Content.ReadFromJsonAsync<Effect>(JsonOptions);
            return effect is not null
                ? Result.Success(effect)
                : Result.Failure<Effect>("Failed to deserialize effect.");
        }
        catch (Exception ex)
        {
            return Result.Failure<Effect>($"API error: {ex.Message}");
        }
    }

    public async Task<Result> Save(Effect effect, Stream? previewImage = null, Stream? icon = null)
    {
        try
        {
            bool isUpdate = cache.Lookup(effect.Id).HasValue;

            HttpResponseMessage response;
            if (isUpdate)
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"/api/effects/{effect.Id}")
                {
                    Content = JsonContent.Create(effect, options: JsonOptions)
                };
                await SignRequest(request);
                response = await httpClient.SendAsync(request);
            }
            else
            {
                response = await httpClient.PostAsJsonAsync("/api/effects", effect, JsonOptions);
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return Result.Failure($"API error ({response.StatusCode}): {error}");
            }

            var saved = await response.Content.ReadFromJsonAsync<Effect>(JsonOptions);
            if (saved is not null)
                cache.AddOrUpdate(saved);

            if (previewImage is not null && saved is not null)
            {
                var previewRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/effects/{saved.Id}/preview")
                {
                    Content = new StreamContent(previewImage)
                };
                previewRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                await SignRequest(previewRequest);
                await httpClient.SendAsync(previewRequest);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"API error: {ex.Message}");
        }
    }

    public async Task<Result> Delete(string id)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/effects/{id}");
            await SignRequest(request);
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return Result.Failure($"API error ({response.StatusCode}): {error}");
            }

            cache.RemoveKey(id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"API error: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> GetPreviewImage(string id)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/effects/{id}/preview");
            if (!response.IsSuccessStatusCode)
                return Result.Failure<Stream>("Preview not available.");

            var stream = await response.Content.ReadAsStreamAsync();
            return Result.Success(stream);
        }
        catch (Exception ex)
        {
            return Result.Failure<Stream>($"API error: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> GetIcon(string id)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/effects/{id}/icon");
            if (!response.IsSuccessStatusCode)
                return Result.Failure<Stream>("Icon not available.");

            var stream = await response.Content.ReadAsStreamAsync();
            return Result.Success(stream);
        }
        catch (Exception ex)
        {
            return Result.Failure<Stream>($"API error: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<Effect>>> GetAll()
    {
        try
        {
            var response = await httpClient.GetAsync("/api/effects?pageSize=100");
            if (!response.IsSuccessStatusCode)
                return Result.Failure<IReadOnlyList<Effect>>($"API error: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var itemsJson = doc.RootElement.GetProperty("items").GetRawText();
            var items = JsonSerializer.Deserialize<List<Effect>>(itemsJson, JsonOptions) ?? [];
            return Result.Success<IReadOnlyList<Effect>>(items);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<Effect>>($"API error: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetches all effects from the API and populates the local cache.
    /// </summary>
    public async Task RefreshAsync()
    {
        var result = await GetAll();
        if (result.IsSuccess)
        {
            cache.Edit(updater =>
            {
                updater.Clear();
                updater.AddOrUpdate(result.Value);
            });
        }
    }

    /// <summary>Number of effects currently in the local cache.</summary>
    public int Count => cache.Count;

    /// <summary>
    /// Populates the local cache with the given effects (used as fallback when API is unavailable).
    /// </summary>
    public void SeedLocal(IEnumerable<Effect> effects)
    {
        cache.Edit(updater =>
        {
            updater.Clear();
            updater.AddOrUpdate(effects);
        });
    }

    private async Task SignRequest(HttpRequestMessage request)
    {
        if (signer is null || identity is null) return;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var message = $"{request.Method}:{request.RequestUri?.AbsolutePath}:{timestamp}";
        var signature = await signer.SignAsync(message);

        request.Headers.Add("X-Nostr-PubKey", identity.CurrentId);
        request.Headers.Add("X-Nostr-Signature", signature);
        request.Headers.Add("X-Nostr-Timestamp", timestamp);
    }
}
