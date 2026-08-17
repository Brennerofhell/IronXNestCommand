namespace IronXNestCommand.Core.Paths;

public static class ModPaths
{
    public static string GameRoot { get; private set; } = AppContext.BaseDirectory;

    public static string DataRoot => Path.Combine(GameRoot, "UserData", ModInfo.DataFolderName);

    public static string ConfigFile => Path.Combine(DataRoot, "config.json");

    public static string ProgressFile => Path.Combine(DataRoot, "player_progress.json");

    public static string LoadoutsFile => Path.Combine(DataRoot, "loadouts.json");

    public static string NotesFile => Path.Combine(DataRoot, "notes.json");

    public static void Initialize(string gameRoot)
    {
        if (string.IsNullOrWhiteSpace(gameRoot))
            throw new ArgumentException("Game root is required.", nameof(gameRoot));

        GameRoot = Path.GetFullPath(gameRoot);
    }

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataRoot);
    }
}
