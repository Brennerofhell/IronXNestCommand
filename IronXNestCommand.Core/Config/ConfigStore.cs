using System.Text.Json;
using IronXNestCommand.Core.Paths;

namespace IronXNestCommand.Core.Config;

public static class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static ModConfig LoadOrCreate()
    {
        ModPaths.EnsureDirectories();

        if (!File.Exists(ModPaths.ConfigFile))
        {
            var created = new ModConfig();
            Save(created);
            return created;
        }

        try
        {
            var json = File.ReadAllText(ModPaths.ConfigFile);
            return JsonSerializer.Deserialize<ModConfig>(json, JsonOptions) ?? new ModConfig();
        }
        catch (JsonException)
        {
            return new ModConfig();
        }
        catch (IOException)
        {
            return new ModConfig();
        }
    }

    public static void Save(ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ModPaths.EnsureDirectories();
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ModPaths.ConfigFile, json);
    }
}
