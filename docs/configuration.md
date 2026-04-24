# Configuration

WebBundler starts with a single JSON file named `bundleconfig.json`.

## Schema And IDE Support

Add the versioned schema to opt into IDE completion and validation:

```json
{
  "$schema": "https://raw.githubusercontent.com/korgys/WebBundler/main/schemas/bundleconfig.v1.schema.json",
  "version": 1,
  "bundles": []
}
```

The schema file is versioned alongside the config shape. If you prefer an offline copy, store the schema in your repo and point `$schema` to that local path.

## Versioning

The root document includes a required `version` field. The current version is `1`.

## Advanced Example

```json
{
  "$schema": "https://raw.githubusercontent.com/korgys/WebBundler/main/schemas/bundleconfig.v1.schema.json",
  "version": 1,
  "manifestOutput": "wwwroot/dist/webbundler.manifest.json",
  "bundles": [
    {
      "output": "wwwroot/dist/site.min.css",
      "inputs": [
        "wwwroot/css/reset.css",
        "wwwroot/css/**/*.css"
      ],
      "type": "css",
      "minify": true
    },
    {
      "output": "wwwroot/dist/site.min.js",
      "inputs": [
        "wwwroot/js/vendor/*.js",
        "wwwroot/js/app.js"
      ],
      "type": "js",
      "minify": false,
      "fingerprint": true
    }
  ]
}
```

## Fields

| Scope | Field | Status | Notes |
| --- | --- | --- | --- |
| Root | `$schema` | optional | Schema association for editor tooling. |
| Root | `version` | required | Must be `1` for the current release. |
| Root | `manifestOutput` | optional | JSON manifest output path written during `build` when configured. |
| Root | `bundles` | required | Ordered list of bundle definitions. |
| Bundle | `output` | required | Output file path. Relative paths are recommended. |
| Bundle | `inputs` | required | Ordered list of file paths or glob patterns. |
| Bundle | `type` | required | `css` or `js`. |
| Bundle | `minify` | optional | Defaults to `true`. |
| Bundle | `fingerprint` | optional | Inserts a short hash before the file extension when a fingerprinter is available. |
| Bundle | `sourceMap` | reserved | Accepted by the parser for forward compatibility. |
| Bundle | `environment` | reserved | Accepted by the parser for forward compatibility. |
| Bundle | `include` | reserved | Accepted by the parser for forward compatibility. |
| Bundle | `exclude` | reserved | Accepted by the parser for forward compatibility. |

## Validation

Current validation checks:

- supported root `version`
- at least one bundle
- unique bundle outputs
- non-empty inputs per bundle
- extension hints for `css` and `js` outputs
- manifest output must not reuse a bundle output path

## Globbing Conventions

- `*` matches within a single path segment.
- `?` matches one character.
- `**` matches nested directories.
- `[` and `]` work for character classes.
- Input patterns are resolved relative to the project root.
- Both `/` and `\` separators are accepted in JSON; `/` is the most portable choice.
- Glob expansion is deterministic and keeps paths sorted.
- Duplicate files matched by multiple inputs in the same bundle are ignored after the first match.
- Negative glob syntax such as `!file.js` is not supported.
- Every input pattern must match at least one file.

## Manifest Format

When `manifestOutput` is set, `build` writes a deterministic JSON manifest alongside the bundle outputs. Paths in the manifest use forward slashes and are relative to the project root.

Example manifest: [manifest.example.json](manifest.example.json)

## Cross-Platform Path Behavior

- Exact file lookups follow the host filesystem, so casing must be correct on case-sensitive systems.
- Glob matching is case-insensitive when resolving candidates.
- Duplicate output detection is case-insensitive on Windows and ordinal on other platforms.
- Avoid paths that differ only by case if the configuration must work everywhere.
