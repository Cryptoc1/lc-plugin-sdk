namespace LethalCompany.SdkSample;

[BepInPlugin(SamplePluginInfo.Identifier, SamplePluginInfo.Name, SamplePluginInfo.Version)]
public sealed partial class SamplePlugin : BaseUnityPlugin
{
    public void Awake()
    {
        _ = Harmony.CreateAndPatchAll(
            typeof(SamplePlugin).Assembly,
            SamplePluginInfo.Identifier);
    }
}
