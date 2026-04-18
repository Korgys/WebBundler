# Configuration

WebBundler starts with a single JSON file named `bundleconfig.json`.

## Versioning

The root document includes a `version` field. The initial version is `1`.

## Shape

```json
{
  "version": 1,
  "bundles": [
    {
      "output": "wwwroot/dist/site.min.css",
      "inputs": [
        "wwwroot/css/reset.css",
        "wwwroot/css/site.css"
      ],
      "type": "css",
      "minify": true
    }
  ]
}
```

## Fields

- `version`
  - configuration schema version
- `bundles`
  - ordered list of bundle definitions
- `output`
  - output file path relative to the project root
- `inputs`
  - ordered list of file paths or glob patterns
- `type`
  - `css` or `js`
- `minify`
  - enables the built-in minifier

## Behavior

- bundle order follows the config order
- glob matches are resolved deterministically
- missing files fail validation/build
- duplicate outputs are rejected

## Future-friendly fields

The schema is intentionally small, but it is designed to evolve toward:

- fingerprinting
- source maps
- environment-specific behavior
- include/exclude filters
- manifests
- output style options
