# Installation

## CLI

Install the CLI tool from NuGet:

```bash
dotnet tool install --global WebBundler.Tool
```

Use it from the project directory:

```bash
webbundler build --config bundleconfig.json
webbundler check --config bundleconfig.json
webbundler validate --config bundleconfig.json
```

## MSBuild

Reference the MSBuild package from the project you want to bundle:

```xml
<ItemGroup>
  <PackageReference Include="WebBundler.MSBuild" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

The target runs automatically during build and publish.

## Notes

- `bundleconfig.json` is read from the project directory unless `WebBundlerConfigFile` is set.
- Set `WebBundlerEnabled=false` to disable the MSBuild target.
- Set `WebBundlerEnableFingerprinting=true` to enable fingerprinting for supported bundles.
- Set `WebBundlerWriteOutputs=false` to validate without writing files.
