using BepInEx.Unity.IL2CPP;
using IronXNestCommand.Core;

namespace IronXNestCommand.Host.BepInEx;

internal static class CoopPresence
{
    public static bool IsPluginLoaded()
    {
        try
        {
            var plugins = IL2CPPChainloader.Instance?.Plugins;
            return plugins != null && plugins.ContainsKey(ModInfo.CoopPluginGuid);
        }
        catch
        {
            return false;
        }
    }
}
