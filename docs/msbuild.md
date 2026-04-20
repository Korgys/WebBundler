# MSBuild

`WebBundler.MSBuild` is a thin integration layer over the shared build services.

## Purpose

- run WebBundler during build or publish
- fail the build when configuration is invalid
- keep behavior aligned with the CLI
- avoid duplicating bundling logic in MSBuild targets

## Basic usage

Add the package and let the transitive target run automatically:

```xml
<ItemGroup>
  <PackageReference Include="WebBundler.MSBuild" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>
```

By default the target reads `bundleconfig.json` from the project directory.

Optional properties:

- `WebBundlerConfigFile` overrides the config path.
- `WebBundlerEnabled=false` disables the target.
- `WebBundlerEnableFingerprinting=true` turns on fingerprinting.

## Behavior

- validates configuration before building
- resolves inputs from the project directory
- writes outputs during build
- supports dry-run behavior through the shared build service
- can be extended later for fingerprinting and manifests

## Design note

The MSBuild project is intentionally thin. It should remain a wrapper around the same core services used by the CLI.
