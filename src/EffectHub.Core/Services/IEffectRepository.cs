using CSharpFunctionalExtensions;
using DynamicData;
using EffectHub.Core.Models;

namespace EffectHub.Core.Services;

public interface IEffectRepository
{
    IObservable<IChangeSet<Effect, string>> Connect();
    Task<Result<Effect>> GetById(string id);
    Task<Result> Save(Effect effect, Stream? previewImage = null, Stream? icon = null);
    Task<Result> Delete(string id);
    Task<Result<Stream>> GetPreviewImage(string id);
    Task<Result<Stream>> GetIcon(string id);
    Task<Result<IReadOnlyList<Effect>>> GetAll();
}
