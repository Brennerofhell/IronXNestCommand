using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using IronXNestCommand.Core;
using IronXNestCommand.Core.Config;
using IronXNestCommand.Core.Paths;
using IronXNestCommand.Host.BepInEx.Overlay;

namespace IronXNestCommand.Host.BepInEx;

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
[BepInDependency(ModInfo.CoopPluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    internal static Plugin? Instance { get; private set; }

    internal Harmony Harmony { get; } = new(ModInfo.Guid);

    internal ModConfig ConfigData { get; private set; } = new();

    public override void Load()
    {
        Instance = this;

        ModPaths.Initialize(BepInEx.Paths.GameRootPath);
        ModPaths.EnsureDirectories();
        ConfigData = ConfigStore.LoadOrCreate();

        AddComponent<CommandOverlay>();

        Log.LogInfo($"{ModInfo.Name} {ModInfo.Version} loaded. Overlay toggle: {ConfigData.ToggleKey}.");
        Log.LogInfo($"Data directory: {ModPaths.DataRoot}");
    }

    public override bool Unload()
    {
        Harmony.UnpatchSelf();
        Instance = null;
        return true;
    }
}
