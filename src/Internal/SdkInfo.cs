namespace LethalCompany.Plugin.Sdk.Internal;

internal static class SdkInfo
{
    public static readonly string AssemblyName = typeof(SdkInfo).Assembly.GetName().Name!;
    public static readonly Version Version = typeof(SdkInfo).Assembly.GetName().Version!;
}
