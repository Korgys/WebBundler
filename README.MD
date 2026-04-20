# WebBundler

![Build](https://github.com/korgys/WebBundler/actions/workflows/ci.yml/badge.svg) ![Status](https://img.shields.io/badge/status-active-success) ![Last Commit](https://img.shields.io/github/last-commit/korgys/WebBundler) ![Coverage](https://img.shields.io/sonar/coverage/Korgys_WebBundler?server=https%3A%2F%2Fsonarcloud.io&color=brightgreen&style=flat-square&label=coverage) [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) [![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=Korgys_WebBundler&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=Korgys_WebBundler) ![.NET](https://img.shields.io/badge/.NET-10.0-blue) ![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20MacOS-lightgrey) ![License](https://img.shields.io/github/license/korgys/WebBundler)

**Lightweight bundling and minification for .NET web projects without JavaScript toolchains.**

WebBundler is a simple, deterministic and CI-friendly asset bundling tool for .NET web applications. It helps bundle and minify CSS and JavaScript files without requiring Node.js, npm, Vite, Webpack, or other heavyweight frontend tooling.

## Why WebBundler exists

Many .NET web projects still need a small, reliable asset pipeline:

- concatenate a few CSS files
- bundle JavaScript files in a known order
- minify output during build
- integrate cleanly with MSBuild and CI/CD
- avoid introducing a full JavaScript toolchain for simple needs

WebBundler is built for that exact use case.

## Goals

WebBundler aims to be:

- lightweight
- deterministic
- CI-friendly
- MSBuild-friendly
- easy to configure
- easy to maintain
- independent from JavaScript package managers and frontend bundlers

## Who it is for

WebBundler is a good fit for:

- ASP.NET MVC projects
- Razor-based web applications
- legacy or hybrid .NET web projects
- enterprise and intranet applications
- teams that want straightforward CSS/JS bundling
- teams that prefer .NET-native build integration
- projects that do not want to introduce Node.js just for asset bundling

## Who it is not for

WebBundler is **not** intended to replace modern frontend toolchains such as:

- Vite
- Webpack
- Rollup
- Parcel

It is not designed for:

- frontend framework development workflows
- hot module replacement
- dev servers
- advanced JavaScript transpilation pipelines
- full SPA asset ecosystems

If your project is heavily frontend-driven, a dedicated JavaScript toolchain is probably a better fit.

## Features

Current capabilities include:

- CSS bundling
- JavaScript bundling
- minification
- JSON configuration via `bundleconfig.json`
- versioned JSON Schema for IDE completion
- deterministic output
- CLI usage
- MSBuild integration
- CI/CD support
- wildcard/glob input support
- configuration validation
- `check` mode for CI
- fingerprinting
- manifest generation
- source maps
- support for additional asset-related scenarios

## Design principles

WebBundler follows a few key principles:

1. **CLI-first** for automation and scripting
2. **MSBuild-first** for .NET project integration
3. **Visual Studio friendly**, but not IDE-dependent
4. **Deterministic behavior** so local and CI outputs match
5. **Simple configuration** and explicit behavior
6. **Maintainable architecture** with clean separation of concerns

## Installation

### CLI

Install the tool from NuGet:

```bash
dotnet tool install --global WebBundler.Tool
```

Run it from the project root:

```bash
webbundler build --config bundleconfig.json
webbundler check --config bundleconfig.json
webbundler validate --config bundleconfig.json
```

### MSBuild

Add the package to a web project:

```xml
<ItemGroup>
  <PackageReference Include="WebBundler.MSBuild" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

The build target looks for `bundleconfig.json` in the project directory by default and runs once per build or publish invocation.

Useful MSBuild properties:

- `WebBundlerConfigFile`
- `WebBundlerEnabled`
- `WebBundlerEnableFingerprinting`
- `WebBundlerWriteOutputs`

## Usage

- `build` writes bundled output files.
- `validate` checks configuration structure only.
- `check` validates configuration and asset inputs without writing files.

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
