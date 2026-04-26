namespace ForEachDb.Desktop.Services;

public interface IFileDialogService
{
    Task<string?> SaveFileAsync(string title, string suggestedName, string extension, string filterLabel);
    Task<string?> OpenFileAsync(string title, string extension, string filterLabel);
}
