using CommunityToolkit.Mvvm.Input;
using ForEachDbQueries;

namespace ForEachDb.Desktop.ViewModels;

// Recipe save/load — modal "Save recipe" panel and file-based import/export.
public sealed partial class WorkspaceViewModel
{
    [RelayCommand]
    private void BeginSaveRecipe()
    {
        SaveRecipeName = _initialRecipeName ?? string.Empty;
        IsSavingRecipe = true;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmSaveRecipe))]
    private void ConfirmSaveRecipe()
    {
        var name = SaveRecipeName.Trim();
        if (string.IsNullOrEmpty(name)) return;

        _recipes.Save(BuildCurrentRecipe(name));
        IsSavingRecipe = false;
        RunStatus = $"Saved recipe '{name}'.";
    }

    private bool CanConfirmSaveRecipe() => !string.IsNullOrWhiteSpace(SaveRecipeName);

    [RelayCommand]
    private void CancelSaveRecipe() => IsSavingRecipe = false;

    [RelayCommand]
    private async Task SaveRecipeToFileAsync()
    {
        var suggested = string.IsNullOrWhiteSpace(SaveRecipeName) ? "recipe" : SaveRecipeName.Trim();
        var path = await _fileDialogs.SaveFileAsync("Save recipe", $"{suggested}.json", "json", "Recipe (JSON)");
        if (path is null) return;

        try
        {
            new RecipeStore(path).Save(BuildCurrentRecipe(suggested));
            IsSavingRecipe = false;
            RunStatus = $"Exported recipe → {path}";
        }
        catch (Exception ex)
        {
            RunStatus = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadRecipeFromFileAsync()
    {
        var path = await _fileDialogs.OpenFileAsync("Load recipe", "json", "Recipe (JSON)");
        if (path is null) return;

        try
        {
            var loaded = new RecipeStore(path).Load();
            if (loaded.Count == 0)
            {
                RunStatus = "No recipes found in that file.";
                return;
            }
            ApplyRecipe(loaded[0]);
            RunStatus = $"Loaded recipe '{loaded[0].Name}' from {path}";
        }
        catch (Exception ex)
        {
            RunStatus = $"Load failed: {ex.Message}";
        }
    }

    private Recipe BuildCurrentRecipe(string name) =>
        new(
            Name: name,
            Connection: _settings with { Password = string.Empty },
            SelectedDatabases: GetSelectedDatabases().ToList(),
            Query: Query,
            Threads: Threads);

    private void ApplyRecipe(Recipe recipe)
    {
        Query = recipe.Query;
        Threads = recipe.Threads;
        var selected = new HashSet<string>(recipe.SelectedDatabases, StringComparer.Ordinal);
        foreach (var db in Databases)
            db.IsSelected = selected.Contains(db.Name);
    }
}
