# FAQ

## Why does WebBundler exist if I can use Node-based tooling?

Some .NET web projects only need basic CSS and JavaScript bundling. WebBundler keeps that workflow inside .NET without introducing a separate frontend toolchain.

## What is the difference between `build`, `check`, and `validate`?

- `build` loads, validates, resolves inputs, and writes outputs.
- `check` loads, validates, and resolves inputs without writing files.
- `validate` only checks the configuration structure.

## Why do missing inputs fail the run?

WebBundler treats missing inputs as an error so the build stays deterministic and does not silently ship incomplete bundles.

## Why do duplicate outputs fail?

Two bundles cannot safely write to the same output path. WebBundler rejects duplicate outputs to avoid non-deterministic results.

## Is this meant for SPA or framework-heavy frontend apps?

No. It is intentionally scoped to simple CSS/JS bundling for .NET web apps. If you need transpilation, HMR, or a full frontend pipeline, use a dedicated frontend toolchain.

## Does it support legacy BundlerMinifier configs?

It supports the same overall idea and most simple CSS/JS bundle layouts, but the config shape is explicit and HTML bundling is not supported.
