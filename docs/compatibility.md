# Compatibility

## Supported platforms

| Area | Support |
| --- | --- |
| .NET target | `net10.0` |
| Operating systems | Windows, Linux, macOS |
| CLI usage | Yes |
| MSBuild integration | Yes |

## Supported asset types

| Type | Supported | Notes |
| --- | --- | --- |
| CSS | Yes | Built-in CSS minifier available. |
| JavaScript | Yes | Built-in JavaScript minifier available. |
| HTML | No | Not supported in the current release. |

## Path and input behavior

- `bundleconfig.json` is read from the project root by default.
- Input and output paths are typically relative to the project root.
- Use `/` in JSON when possible; `\` also works.
- Exact-path lookups follow the host filesystem, so keep casing correct on Linux and macOS.
- Glob patterns are supported and resolved deterministically.
- Glob matching is case-insensitive when resolving candidates.
- Duplicate outputs are checked with Windows-aware path comparison.

See [Configuration](configuration.md) for the full glob syntax and config shape.

## Practical compatibility notes

- The tool is designed for small, deterministic asset pipelines.
- It is a fit for ASP.NET MVC, Razor, and other .NET web apps that only need CSS/JS bundling.
- It is not a SPA bundler and does not replace Vite, Webpack, Rollup, or similar frontend toolchains.
