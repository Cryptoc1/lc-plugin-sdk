namespace LethalCompany.SdkSample;

[BepInPlugin(GeneratedPluginInfo.Identifier, GeneratedPluginInfo.Name, GeneratedPluginInfo.Version)]
public sealed class SamplePlugin : BaseUnityPlugin
{
    public void Awake()
    {
        _ = Harmony.CreateAndPatchAll(
            typeof(SamplePlugin).Assembly,
            GeneratedPluginInfo.Identifier);
    }
}
