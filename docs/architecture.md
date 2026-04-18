# Architecture

WebBundler is split into small projects so the CLI, MSBuild integration, and future packaging can reuse the same build logic.

## Layers

- `WebBundler.Configuration`
  - loads `bundleconfig.json`
  - validates structure and basic rules
  - keeps configuration parsing separate from execution
- `WebBundler.Core`
  - defines the main build domain
  - resolves inputs and orchestrates bundle builds
  - stays free of MSBuild and UI concerns
- `WebBundler.Minification`
  - provides CSS and JS minifiers
  - can be replaced or extended later
- `WebBundler.Fingerprinting`
  - provides hash-based fingerprinting helpers
  - remains optional for the first release
- `WebBundler.Tool`
  - CLI entry point
  - handles command parsing, exit codes, and console output
- `WebBundler.MSBuild`
  - MSBuild task wrapper around the shared services
  - fails the build on config or asset errors

## Build flow

1. Load `bundleconfig.json`
2. Validate the config document
3. Resolve bundle inputs
4. Concatenate files in declared order
5. Minify according to bundle type
6. Optionally fingerprint
7. Write outputs
8. Emit diagnostics and exit codes

## Design goals

- deterministic output
- small and testable classes
- reusable services for CLI and MSBuild
- no dependency on a frontend toolchain
- straightforward CI behavior

## Near-term extension points

- fingerprinting
- manifests
- source maps
- `check` and `clean` commands
- future config schema versions
