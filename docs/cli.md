# CLI

The CLI command is `webbundler`.

## Commands

```bash
webbundler build --config bundleconfig.json
webbundler validate --config bundleconfig.json
```

## Exit codes

- `0` success
- `1` invalid command or arguments
- `2` configuration or validation failure
- `3` build failure

## Notes

- no interactive prompts
- no dev server
- output is deterministic and scriptable
- console messages are intended for CI logs

## Planned commands

- `webbundler check`
- `webbundler clean`
