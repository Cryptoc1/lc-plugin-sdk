# Lethal Company Plugin SDK

Get modding Lethal Company easier, faster, and *better*.


## Usage

- Add a reference to the `LethalCompany.Plugin.Sdk` package to an empty Class Library targeting `netstandard2.1`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="LethalCompany.Plugin.Sdk" Version="..." PrivateAssets="all" />
  </ItemGroup>
</Project>
```

- Define your plugin metadata in your `.csproj`
```xml
<PropertyGroup>
  <Title>Plugin Example</Title>
  <Description>My example plugin!</Description>
  <PluginId>example.plugin</PluginId>
  <ThunderId>Example_Plugin</ThunderId>
  <Version>1.0.0</Version>
</PropertyGroup>

<ItemGroup>
  <ThunderReference Include="..." />
</ItemGroup>
```

- Create your plugin class
```csharp
[BepInPlugin(GeneratedPluginInfo.Identifier, GeneratedPluginInfo.Name, GeneratedPluginInfo.Version)]
public sealed class SamplePlugin : BaseUnityPlugin
{
    // ...
}
```
> _The Sdk creates a `GeneratedPluginInfo` class from the metadata provided in your project for usage in code._
> 
> _The name of the generated class can be changed using the `<PluginInfoTypeName>...</PluginInfoTypeName>` MSBuild property._


- Use `dotnet publish` to create a package that's ready for upload to Thunderstore
```
> dotnet publish -c Release -o package
  ...
  
  Zipping directory ".\bin\Release\netstandard2.1\publish\" to ".\package\Example_Plugin-1.0.0.zip".
```


## Features

### Default Build Configuration

This Sdks provides out-of-the-box configuration of your project, ensuring proper Release builds when publishing, and default `TargetFramework` optimizations.

### Use Modern Language Features

This Sdk adds a reference to [PolySharp](https://github.com/Sergio0694/PolySharp) to your project, allowing you to use the latest language and compiler features, like `ImplicitUsings`, `Nullable`, and more (enabled by default).

### Game Library References

This Sdk adds a reference to [`LethalAPI.GameLibs`](https://github.com/dhkatz/LethalAPI.GameLibs) to your project, allowing capabilities like CI/CD.

### Publish to Thunderstore

This Sdk provides custom build properties for generating a `manifest.json`, and extends publish targets to generate an archive from project assets that's ready for upload to Thunderstore.

### Code Analysis

This Sdk adds a reference to the [NETAnalyzers](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview?tabs=net-8) to your project, and sets the default analysis level to `latest-recommended`.

Additional analyzers, such as [`BepInEx.Analyzers`](https://github.com/BepInEx/BepInEx.Analyzers), are also included.