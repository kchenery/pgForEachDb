using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ForEachDb.Desktop.Services;

public sealed class AvaloniaFileDialogService : IFileDialogService
{
    private readonly TopLevel _topLevel;

    public AvaloniaFileDialogService(TopLevel topLevel) => _topLevel = topLevel;

    public async Task<string?> SaveFileAsync(string title, string suggestedName, string extension, string filterLabel)
    {
        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(filterLabel) { Patterns = [$"*.{extension}"] }]
        });
        return file?.Path.LocalPath;
    }

    public async Task<string?> OpenFileAsync(string title, string extension, string filterLabel)
    {
        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(filterLabel) { Patterns = [$"*.{extension}"] }]
        });
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}
