using CSharpFunctionalExtensions;

namespace EffectHub.Core.Services;

public interface IPreviewCapture
{
    Task<Result<Stream>> CapturePreview();
}
