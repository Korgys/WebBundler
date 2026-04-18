# MSBuild

`WebBundler.MSBuild` is a thin integration layer over the shared build services.

## Purpose

- run WebBundler during build or publish
- fail the build when configuration is invalid
- keep behavior aligned with the CLI
- avoid duplicating bundling logic in MSBuild targets

## Basic usage

Import the target file and point it at `bundleconfig.json`.

## Behavior

- validates configuration before building
- resolves inputs from the project directory
- writes outputs during build
- can be extended later for fingerprinting and manifests

## Design note

The MSBuild project is intentionally thin. It should remain a wrapper around the same core services used by the CLI.
