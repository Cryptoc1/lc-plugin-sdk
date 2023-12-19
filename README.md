# Lethal Company Plugin SDK

An [MSBuild Sdk](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk?view=vs-2022) for creating Lethal Company mods that:

- Optimizes Build Defaults
- Enables Modern Language Features with [`PolySharp`](https://github.com/Sergio0694/PolySharp)
- References Publicized Binaries from [`LethalAPI.GameLibs`](https://github.com/dhkatz/LethalAPI.GameLibs)
- Creates Thunderstore Packages
- And More...


## Usage

To start using the Sdk, create a new Class Library:
```bash
$ dotnet new classlib -n {NAME}
```

In the new `.csproj`, update the `Sdk="Microsoft.NET.Sdk"` attribute at the top of the file to `Sdk="LethalCompany.Plugin.Sdk/{VERSION}"`, and replace any existing content with metadata about the plugin:
```xml
<Project Sdk="LethalCompany.Plugin.Sdk/1.0.0">
  
  <PropertyGroup>
    <Title>Plugin Example</Title>
    <Description>My example plugin!</Description>
    <PluginId>example.plugin</PluginId>
    <Version>1.0.0</Version>
  </PropertyGroup>

</Project>
```

Add a new `.cs` file, and define the plugin:
```csharp
[BepInPlugin(GeneratedPluginInfo.Identifier, GeneratedPluginInfo.Name, GeneratedPluginInfo.Version)]
public sealed class SamplePlugin : BaseUnityPlugin
{
    // ...
}
```
> _The Sdk generates a `GeneratedPluginInfo` class from the metadata provided in your project for usage in code._
> 
> _The name of the generated class can be changed using the `<PluginInfoTypeName></PluginInfoTypeName>` MSBuild property._

### Publish to Thunderstore

> _In order to create a Thunderstore Package, the Sdk requires that `icon.png`, `CHANGELOG.md` and `README.md` files exist at the project root._

In the `.csproj` of the plugin, provide the metadata used to generate a `manifest.json` for publishing:
```xml
<Project Sdk="LethalCompany.Plugin.Sdk/1.0.0">
  
  <PropertyGroup>
    <!-- ... -->

    <Description>My example plugin!</Description>
    <ThunderId>ExamplePlugin</ThunderId>
    <ThunderWebsiteUrl>https://example.com</ThunderWebsiteUrl>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <ThunderDependency Include="ExampleTeam-OtherPlugin-1.0.0" />
  </ItemGroup>

</Project>
```

The following `manifest.json` would be generated for the example metadata:
```json
{
  "name": "ExamplePlugin",
  "dependecies": ["BepInEx-BepInExPack-5.4.2100", "ExampleTeam-OtherPlugin-1.0.0"],
  "description": "My example plugin!",
  "version_number": "1.0.0",
  "website_url": "https://example.com"
}
```

To create a Thunderstore package, use `dotnet publish`:
```
$ dotnet publish 
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  ExamplePlugin -> .\bin\Debug\netstandard2.1\ExamplePlugin.dll
  ExamplePlugin -> .\bin\Debug\netstandard2.1\publish\
  Zipping directory ".\bin\Debug\netstandard2.1\publish\" to ".\bin\Debug\netstandard2.1\ExamplePlugin-1.0.0.zi
  p".
```

#### Specify Thunderstore Dependencies

To specify Thunderstore dependencies in the generated `manifest.json`, use the `ThunderDependency` item:
```xml
<ItemGroup>
  <ThunderDependency Include="ExampleTeam-ExamplePlugin-1.0.0" />
</ItemGroup>
```

> _The Sdk specifies a default `ThunderDependency` on `BepInExPack`, specifying one yourself is unnecessary._