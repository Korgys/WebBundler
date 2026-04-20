# WebBundler

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
  <PackageReference Include="WebBundler.MSBuild" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>
```

The build target looks for `bundleconfig.json` in the project directory by default.

## Usage

- `build` writes bundled output files.
- `validate` checks configuration structure only.
- `check` validates configuration and asset inputs without writing files.

See `docs/cli.md` and `docs/msbuild.md` for more examples.
