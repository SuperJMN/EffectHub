using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace EffectHub.Services;

public class FileDialogService : IFileDialogService
{
    public async Task<Stream?> OpenFile(string title, string[] extensions)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(title) { Patterns = extensions.Select(e => $"*.{e}").ToList() }]
        });

        return files.Count > 0 ? await files[0].OpenReadAsync() : null;
    }

    public async Task<Stream?> SaveFile(string title, string suggestedFileName, string[] extensions)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = [new FilePickerFileType(title) { Patterns = extensions.Select(e => $"*.{e}").ToList() }]
        });

        return file is not null ? await file.OpenWriteAsync() : null;
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}
