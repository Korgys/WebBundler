# MSBuild

`WebBundler.MSBuild` is a thin integration layer over the shared build services.

## Purpose

- run WebBundler during build or publish
- fail the build when configuration is invalid
- keep behavior aligned with the CLI
- avoid duplicating bundling logic in MSBuild targets

## Installation

Add the package to the project that should run bundling:

```xml
<ItemGroup>
  <PackageReference Include="WebBundler.MSBuild" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

The package is `buildTransitive`, so the target runs automatically after restore.

## When It Runs

- `dotnet build` runs WebBundler before `BeforeBuild`.
- `dotnet publish` runs WebBundler before build unless `NoBuild=true`.
- `dotnet publish /p:NoBuild=true` runs WebBundler before `BeforePublish`.
- the target runs once per invocation, not twice during publish.

By default the config file is `$(MSBuildProjectDirectory)/bundleconfig.json`.

## Supported Properties

- `WebBundlerConfigFile` overrides the config path.
- `WebBundlerEnabled=false` disables the target.
- `WebBundlerEnableFingerprinting=true` enables fingerprinting for supported bundles.
- `WebBundlerWriteOutputs=false` validates and builds without writing files.
- Manifest output is configured in `bundleconfig.json` and is written only when outputs are written.
- Source maps follow the same rule: `sourceMap: true` writes a sibling `.map` file only when outputs are written.

## Behavior

- validates configuration before building
- resolves inputs from the project directory
- writes outputs by default
- writes the configured manifest alongside bundle outputs
- writes source maps alongside source outputs when enabled
- honors `WebBundlerWriteOutputs=false` for validation-only runs
- keeps the MSBuild path aligned with the CLI core services

## Example

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <WebBundlerWriteOutputs>true</WebBundlerWriteOutputs>
    <WebBundlerEnableFingerprinting>true</WebBundlerEnableFingerprinting>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="WebBundler.MSBuild" Version="1.0.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

## Design note

The MSBuild project is intentionally thin. It should remain a wrapper around the same core services used by the CLI.
