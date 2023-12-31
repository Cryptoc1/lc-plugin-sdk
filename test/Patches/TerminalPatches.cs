using BepInEx.Logging;

namespace LethalCompany.SdkSample.Patches;

[HarmonyPatch(typeof(Terminal))]
public sealed class TerminalPatches
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(TerminalPatches));

    [HarmonyPatch(nameof(Terminal.Update))]
    [HarmonyPostfix]
    public static void OnUpdated() => Logger.LogWarning("UPDATE");
}
