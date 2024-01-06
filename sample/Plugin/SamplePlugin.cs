using SdkSample.Library;

namespace SdkSample.Plugin;

[BepInPlugin(GeneratedPluginInfo.Identifier, GeneratedPluginInfo.Name, GeneratedPluginInfo.Version)]
public sealed class SamplePlugin : BaseUnityPlugin
{
    public void Awake()
    {
        Logger.LogInfo($"TEST: {LethalLib.Modules.Levels.LevelTypes.OffenseLevel}, {LibraryClass.Value}");
        _ = Harmony.CreateAndPatchAll(
            typeof(SamplePlugin).Assembly,
            GeneratedPluginInfo.Identifier);
    }
}
