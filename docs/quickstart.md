# Quickstart

This is the fastest path to a working bundle.

## 1. Install the tool

```bash
dotnet tool install --global WebBundler.Tool
```

## 2. Create this file layout

```text
your-project/
  bundleconfig.json
  wwwroot/
    css/
      base.css
      site.css
    js/
      app.js
    dist/
```

## 3. Add `bundleconfig.json`

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
    },
    {
      "output": "wwwroot/dist/site.min.js",
      "inputs": [
        "wwwroot/js/app.js"
      ],
      "type": "js",
      "minify": true
    }
  ]
}
```

## 4. Run the CLI

```bash
webbundler validate --config bundleconfig.json
webbundler check --config bundleconfig.json
webbundler build --config bundleconfig.json
```

## 5. What success looks like

- `validate` returns `0` after confirming the config shape.
- `check` returns `0` after resolving the input files.
- `build` returns `0` and writes:
  - `wwwroot/dist/site.min.css`
  - `wwwroot/dist/site.min.js`

## Notes

- Run the commands from the project root so relative paths resolve correctly.
- `type` must be `css` or `js`.
- `inputs` are processed in order.
