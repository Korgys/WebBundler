# WebBundler

![Build](https://github.com/korgys/WebBundler/actions/workflows/ci.yml/badge.svg) ![Status](https://img.shields.io/badge/status-active-success) ![Last Commit](https://img.shields.io/github/last-commit/korgys/WebBundler) ![Coverage](https://img.shields.io/sonar/coverage/Korgys_WebBundler?server=https%3A%2F%2Fsonarcloud.io&color=brightgreen&style=flat-square&label=coverage) [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) ![.NET](https://img.shields.io/badge/.NET-10.0-blue) ![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey) ![License](https://img.shields.io/github/license/korgys/WebBundler)

**Bundle and minify web assets in .NET projects without Node.js.**

WebBundler is a lightweight, deterministic and CI-friendly asset bundler for .NET web applications.

It helps you bundle and minify CSS and JavaScript files without requiring Node.js, npm, Vite, Webpack, Rollup or any heavy frontend toolchain.

NuGet packages :

- https://www.nuget.org/packages/WebBundler.Minification
- https://www.nuget.org/packages/WebBundler.Tool
- https://www.nuget.org/packages/WebBundler.MSBuild
- https://www.nuget.org/packages/WebBundler.Configuration
- https://www.nuget.org/packages/WebBundler.Core
- https://www.nuget.org/packages/WebBundler.Fingerprinting

## Why use WebBundler?

Many .NET web projects do not need a full JavaScript build stack.

Sometimes you only need to:

- combine a few CSS files
- bundle JavaScript files in a fixed order
- minify outputs during build
- generate fingerprinted assets
- produce a manifest file for cache-busting
- run the same behavior locally and in CI/CD
- keep the asset pipeline inside the .NET ecosystem

**WebBundler is built for this use case.**

## Quick start

### 1. Create a bundleconfig.json file

Create a `bundleconfig.json` file at the root of your project:

```json
{
  "$schema": "https://raw.githubusercontent.com/korgys/WebBundler/main/schemas/bundleconfig.v1.schema.json",
  "version": 1,
  "manifestOutput": "wwwroot/dist/webbundler.manifest.json",
  "bundles": [
    {
      "output": "wwwroot/dist/site.min.css",
      "inputs": ["wwwroot/css/reset.css", "wwwroot/css/site.css"],
      "type": "css",
      "minify": true,
      "fingerprint": true,
      "sourceMap": true
    },
    {
      "output": "wwwroot/dist/site.min.js",
      "inputs": ["wwwroot/js/vendor/*.js", "wwwroot/js/app.js"],
      "type": "js",
      "minify": true,
      "fingerprint": true,
      "sourceMap": true
    }
  ]
}
```

### 2. Install and run the CLI

Install the tool from NuGet:

```bash
dotnet tool install --global WebBundler.Tool
```

Run it from the project root:

```bash
webbundler validate --config bundleconfig.json
webbundler check --config bundleconfig.json
webbundler build --config bundleconfig.json
```

#### Usage

| Command               | Purpose                                                  | Writes files |
| --------------------- | -------------------------------------------------------- | ------------ |
| `webbundler validate` | Validates configuration structure only                   | No           |
| `webbundler check`    | Validates configuration and resolves asset inputs        | No           |
| `webbundler build`    | Builds bundles, source maps, and any configured manifest | Yes          |

### 3. Or integrate with MSBuild

Add the package to a web project:

```xml
<ItemGroup>
  <PackageReference Include="WebBundler.MSBuild" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

The build target looks for `bundleconfig.json` in the project directory by default and runs once per build or publish invocation.

Useful MSBuild properties:

- `WebBundlerConfigFile`: path to the configuration file.
- `WebBundlerEnabled`: enables or disables WebBundler during build.
- `WebBundlerEnableFingerprinting`: globally enables fingerprinted outputs.
- `WebBundlerWriteOutputs`: controls whether output files are written.

## Features

Current capabilities include:

- CSS and JavaScript minification
- CSS and JavaScript bundling
- fingerprinted output files
- CLI usage, CI/CD support
- MSBuild integration
- JSON configuration via `bundleconfig.json`
- versioned JSON Schema for IDE completion
- manifest generation via `manifestOutput`
- external sibling `.map` source maps for CSS and JavaScript

## Design principles

WebBundler follows a few key principles:

1. **CLI-first** for automation and scripting
2. **MSBuild-first** for .NET project integration
3. **Visual Studio friendly**, but not IDE-dependent
4. **Deterministic behavior** so local and CI outputs match
5. **Simple configuration** and explicit behavior
6. **Maintainable architecture** with clean separation of concerns

## Docs

- [Quickstart](docs/quickstart.md)
- [Migration guide](docs/migration.md)
- [Compatibility](docs/compatibility.md)
- [FAQ](docs/faq.md)
- [CLI reference](docs/cli.md)
- [Configuration](docs/configuration.md)
- [Installation](docs/installation.md)
- [MSBuild](docs/msbuild.md)
- [Architecture](docs/architecture.md)

See the docs above for examples and implementation details.
