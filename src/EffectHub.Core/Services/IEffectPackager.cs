using CSharpFunctionalExtensions;
using EffectHub.Core.Models;

namespace EffectHub.Core.Services;

public interface IEffectPackager
{
    Task<Result> Export(Effect effect, Stream outputStream);
    Task<Result<Effect>> Import(Stream inputStream);
}
