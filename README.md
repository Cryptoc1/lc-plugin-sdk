# LethalCompany.Plugin.Sdk
[![NuGet](https://img.shields.io/nuget/vpre/LethalCompany.Plugin.Sdk)](https://www.nuget.org/packages/LethalCompany.Plugin.Sdk)
[![Build](https://img.shields.io/github/actions/workflow/status/cryptoc1/lc-plugin-sdk/default.yml)](https://github.com/cryptoc1/lc-plugin-sdk/actions/workflows/default.yml)
![Language](https://img.shields.io/github/languages/top/cryptoc1/lc-plugin-sdk)


An [MSBuild Sdk](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk?view=vs-2022) for creating Lethal Company mods that:

- Optimizes Build Defaults
- Enables Modern Language Features with [`PolySharp`](https://github.com/Sergio0694/PolySharp)
- References Publicized Binaries from [`LethalAPI.GameLibs`](https://github.com/dhkatz/LethalAPI.GameLibs)
- References BepInEx packages from the [BepInEx Registry](https://nuget.bepinex.dev/)
- Creates Thunderstore Packages with `dotnet publish`
- Stages plugins to local a Thunderstore profile
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
> _The name of the generated class can be changed using the `<PluginInfoTypeName />` MSBuild property._
>
> _By default, the generated class is `internal static`, this can be changed using the `<PluginInfoTypeAccessModifier />` MSBuild property._


### Publish to Thunderstore

> _In order to create a Thunderstore Package, the Sdk requires that `icon.png` and `README.md` files exist at the project root._

> _The location of the `CHANGELOG.md` and `README.md` files can be customized using the `<PluginChangelogFile />` and `<PluginReadMeFile />` MSBuild properties._

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
  "dependencies": ["BepInEx-BepInExPack-5.4.2100", "ExampleTeam-OtherPlugin-1.0.0"],
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

#### Staging Plugins

"Staging" a plugin refers to the process of publishing a plugin directly to a local Thunderstore profile, and is performed by specifiying the `PluginStagingProfile` MSBuild property when publishing:
```bash
dotnet publish -p:PluginStagingProfile="..."
```

> _It is recommended to set the `<PluginStagingProfile />` MSBuild property in a `.csproj.user` file._

#### Specify Thunderstore Dependencies

To specify Thunderstore dependencies in the generated `manifest.json`, use the `ThunderDependency` item:
```xml
<ItemGroup>
  <ThunderDependency Include="ExampleTeam-ExamplePlugin-1.0.0" />
</ItemGroup>
```

> _The Sdk specifies a default `ThunderDependency` on `BepInExPack`, specifying one yourself is unnecessary._