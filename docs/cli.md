# CLI

The CLI command is `webbundler`.

## Commands

```bash
webbundler build --config bundleconfig.json
webbundler check --config bundleconfig.json
webbundler validate --config bundleconfig.json
```

## Exit codes

| Code | Meaning | When it happens |
| --- | --- | --- |
| `0` | Success | The command completed successfully. This includes `help`, `validate`, `check`, and `build` when no errors occurred. |
| `1` | Invalid command or arguments | The command name is unknown, an argument is unknown, or `--config` is missing its value. The CLI prints the parse error and then shows help. |
| `2` | Configuration or validation failure | `bundleconfig.json` could not be found or parsed, or the configuration failed validation. This is returned by `build`, `check`, and `validate`. |
| `3` | Build failure | The configuration loaded and validated, but the build failed because of asset resolution or build-time errors. This is returned by `build` and `check`. |

## Notes

- no interactive prompts
- no dev server
- output is deterministic and scriptable
- console messages are intended for CI logs

## Command Summary

- `build` writes output files after loading and validating `bundleconfig.json`.
- `check` loads and validates the config, resolves bundle inputs, and does not write files.
- `validate` loads and validates the config structure only.
- `clean` is planned for later.

## Behavior Details

- `webbundler` with no arguments shows help and returns `0`.
- `webbundler help` shows help and returns `0`.
- `webbundler build --config bundleconfig.json` writes bundle outputs when the config is valid.
- `webbundler check --config bundleconfig.json` performs the same load and validation steps as `build`, then stops before writing outputs.
- `webbundler validate --config bundleconfig.json` stops after configuration validation and never touches asset files.
