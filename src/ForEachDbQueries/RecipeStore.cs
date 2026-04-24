using System.Text.Json;

namespace ForEachDbQueries;

public sealed class RecipeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _path;

    public RecipeStore(string? pathOverride = null)
    {
        _path = pathOverride ?? DefaultPath();
    }

    public string Path => _path;

    public IReadOnlyList<Recipe> Load()
    {
        if (!File.Exists(_path)) return [];

        try
        {
            var json = File.ReadAllText(_path);
            var doc = JsonSerializer.Deserialize<RecipeFile>(json, JsonOptions);
            return doc?.Recipes?.ToList() ?? [];
        }
        catch (JsonException)
        {
            // Treat a corrupt file as empty rather than crashing; user can recreate.
            return [];
        }
    }

    public void Save(Recipe recipe)
    {
        var existing = Load().ToList();
        var index = existing.FindIndex(r => string.Equals(r.Name, recipe.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) existing[index] = recipe;
        else existing.Add(recipe);

        Write(existing);
    }

    public bool Delete(string name)
    {
        var existing = Load().ToList();
        var removed = existing.RemoveAll(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed) Write(existing);
        return removed;
    }

    public bool Exists(string name)
    {
        return Load().Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private void Write(IEnumerable<Recipe> recipes)
    {
        var directory = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var doc = new RecipeFile(recipes.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList());
        File.WriteAllText(_path, JsonSerializer.Serialize(doc, JsonOptions));
    }

    private static string DefaultPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return System.IO.Path.Combine(appData, "pgForEachDb", "recipes.json");
        }

        var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var configHome = string.IsNullOrWhiteSpace(xdg)
            ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
            : xdg;
        return System.IO.Path.Combine(configHome, "pgForEachDb", "recipes.json");
    }

    private sealed record RecipeFile(IReadOnlyList<Recipe> Recipes);
}
