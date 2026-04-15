namespace EffectHub.Services;

public interface IFileDialogService
{
    Task<Stream?> OpenFile(string title, string[] extensions);
    Task<Stream?> SaveFile(string title, string suggestedFileName, string[] extensions);
}
