using CSharpFunctionalExtensions;
using DynamicData;
using EffectHub.Core.Models;

namespace EffectHub.Core.Services;

public class InMemoryEffectRepository : IEffectRepository
{
    private readonly SourceCache<Effect, string> cache = new(e => e.Id);

    public IObservable<IChangeSet<Effect, string>> Connect() => cache.Connect();

    public Task<Result<Effect>> GetById(string id)
    {
        var item = cache.Lookup(id);
        return Task.FromResult(item.HasValue
            ? Result.Success(item.Value)
            : Result.Failure<Effect>($"Effect '{id}' not found."));
    }

    public Task<Result> Save(Effect effect, Stream? previewImage = null, Stream? icon = null)
    {
        cache.AddOrUpdate(effect);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> Delete(string id)
    {
        cache.RemoveKey(id);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<Stream>> GetPreviewImage(string id)
        => Task.FromResult(Result.Failure<Stream>("Preview images not available in browser."));

    public Task<Result<Stream>> GetIcon(string id)
        => Task.FromResult(Result.Failure<Stream>("Icons not available in browser."));

    public Task<Result<IReadOnlyList<Effect>>> GetAll()
    {
        var items = cache.Items.ToList();
        return Task.FromResult(Result.Success<IReadOnlyList<Effect>>(items));
    }

    public void Seed(IEnumerable<Effect> effects)
    {
        cache.Edit(updater =>
        {
            foreach (var effect in effects)
            {
                updater.AddOrUpdate(effect);
            }
        });
    }
}
