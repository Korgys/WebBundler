# Migration Guide

This guide covers the common path from BundlerMinifier or BuildBundlerMinifier2022 to WebBundler.

## Package replacement

### CLI-first workflow

- Remove any old BundlerMinifier tooling you only used for command-line builds.
- Install `WebBundler.Tool` as a global tool.
- Replace `bundlerminifier` or custom build scripts with `webbundler build`, `check`, or `validate`.

### MSBuild workflow

- Replace `BuildBundlerMinifier` or `BuildBundlerMinifier2022` with `WebBundler.MSBuild`.
- Keep `bundleconfig.json` at the project root unless you intentionally override the path.
- Use the `WebBundlerConfigFile` property only if you need a non-default location.

## Config mapping

WebBundler uses the same basic `bundleconfig.json` concept, but the shape is explicit:

| BundlerMinifier / BuildBundlerMinifier2022 | WebBundler |
| --- | --- |
| `outputFileName` | `output` |
| `inputFiles` | `inputs` |
| `minify.enabled` | `minify` |
| inferred bundle type from extension | explicit `type` (`css` or `js`) |

Example migration:

```json
{
  "version": 1,
  "bundles": [
    {
      "output": "wwwroot/dist/site.min.css",
      "inputs": [
        "wwwroot/css/base.css",
        "wwwroot/css/site.css"
      ],
      "type": "css",
      "minify": true
    }
  ]
}
```

## What carries over cleanly

- Ordered concatenation of files
- Glob patterns such as `wwwroot/js/*.js`
- CSS and JavaScript bundling
- Build-time execution
- Relative paths from the project root

## What does not carry over

- HTML bundling/minification
- Negative glob syntax such as `!file.js`
- Source map settings from the old extension
- Complex minifier option blocks from the old extension
- Visual Studio-specific bundle management features

## Common gotchas

- Set `type` explicitly. WebBundler validates bundle type independently from the output filename.
- Keep output paths unique. Duplicate outputs fail validation and build.
- Make sure every input pattern matches at least one file.
- `check` resolves inputs without writing files, which is useful when validating a migration.

## Recommended migration path

1. Copy the existing `bundleconfig.json`.
2. Rename fields to the WebBundler shape.
3. Add `type` to every bundle.
4. Remove unsupported HTML or negative-pattern cases.
5. Run `webbundler validate` and then `webbundler check`.
6. Switch the build/package reference to WebBundler once the output matches expectations.
