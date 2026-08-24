# LunaX Replacement Spec

## Purpose

This document defines the invariant architecture that the Unity editor tool must reproduce in order to replace LunaX.
It intentionally avoids overfitting to asset-name differences between levels or games.
The supplied samples show that LunaX is a stable transform from Luna `build project` input into a single-file playable runtime plus thin network emitters.

## Verified Input Boundary

The following files are present across every inspected Luna `build project` zip:

- `index.html`
- `engine/scripts.js`
- `engine/manifest.json`
- `cache/<version>/scripts.js`
- `cache/<version>/jsons.js`
- `cache/<version>/blobs.js`
- `assets/bundles.json`
- `assets/shaders.json`
- `assets/bundles/-1/bundle.json`
- `assets/bundles/-1/data.blob`
- `assets/bundles/-2/bundle.json`
- `assets/bundles/-2/data.blob`
- `luna.json`
- `playable_health_check.json`
- `playground.json`
- `resources/data.json`

Observed notes:

- `SandyJam` inputs carry `cache/210/*`.
- `CandyHouse` and `DogShower` carry both `cache/210/*` and `cache/211/*`.
- `resources/data.json` is always `{}` in the inspected inputs.
- `assets/bundles/-2/data.blob` can be present but empty.

## Verified Output Boundary

Across multiple LunaX outputs from different levels and different games, the final paid-network exports follow one shared shape:

- AppLovin, IronSource, Unity: single `.html` directly in the outer zip.
- Google Ads, Mintegral: inner `.zip` containing exactly one `.html`.
- The per-network delta is small compared with the shared core.

## Shared Core Runtime Invariants

All true LunaX outputs include the following runtime primitives:

- `_compressedAssets`
- `decompressString(...)`
- `decompressArrayBuffer(...)`
- `Bridge.ready(...)`
- `startGame()`
- `pc.waitForBox2D`
- `pc.waitForMecanim`
- `window.jsons`
- `window.blobs`
- `window.sounds`
- `data-src122` image hydration
- worker-backed decompression primitives such as `_freeWorkers`, `_workerWaitingList`, `_workerBlobUrl`

This means LunaX is not just a zipper. It is a shared single-html runtime builder.

## Boot Contract Invariants

The ready sequence is stable across the inspected outputs:

1. `DOMContentLoaded`
2. decompression scheduling begins
3. `bridge:ready`
4. `luna:ready`
5. `luna:build`
6. readiness gate waits for:
   - `_decode122Promise`
   - `pc.waitForBox2D`
   - `pc.waitForMecanim`
   - `pi.ready(...)`
7. `startGame()`
8. `luna:start`
9. `luna:started`

The builder must preserve this contract. Triggering `luna:ready` or `luna:build` too early breaks boot.

## Source To Output Mapping

The transform is category-based, not filename-based.

### Preserved As Inline Runtime

- `engine/scripts.js`
- core parts of `index.html`
- boot, lifecycle, `Bridge.ready`, `startGame`, and preloader scaffolding from source `index.html`

### Recreated As Resource Stores

The final runtime recreates these stores from the Luna input packages:

- `window.jsons`
  - `assets/bundles.json`
  - `assets/bundles/-1/bundle.json`
  - `assets/bundles/-2/bundle.json`
  - `assets/shaders.json`
- `window.blobs`
  - non-empty `.blob` payloads needed by the runtime, especially `assets/bundles/-1/data.blob`
- `window.sounds`
  - `.mp3` assets
- hidden DOM assets
  - images and videos materialized as DOM elements for engine lookups such as `document.getElementById(path)`

### Likely Source-Only Or Build-Time Inputs

The inspected final outputs do not carry standalone references to:

- `engine/manifest.json`
- `cache/<version>/*.js`
- `playable_health_check.json`
- `playground.json`
- `resources/data.json`
- `luna.json`

This does not mean they are useless. It means LunaX consumes them during build time or the final runtime no longer needs them as independent files.

## Encoding Model Invariants

The inspected outputs use a mixed payload strategy:

- compressed strings for JSON-like payloads
- compressed array buffers for blobs and sounds
- DOM image hydration through inline `data:image/...` and `data-src122`
- worker-backed parallel decompression

The replacement builder must remain category-aware. A single generic payload queue is not faithful enough.

## Network Tail Invariants

The network-specific layer is thin.

### Google Ads

- adds `ExitApi.exit()` CTA tail
- packaged as inner zip with one html

### Mintegral

- CTA tail uses `window.install()`
- lifecycle tail uses `window.gameReady()` and `window.gameEnd()`
- packaged as inner zip with one html

### AppLovin / IronSource / Unity

- thin MRAID tail using `mraid.open(...)`
- emitted as direct single html outputs

## Replacement Tool Architecture

The new Unity editor implementation must be split into explicit stages.

### Stage 1: Input Scan

Read the Luna `build project` and classify files into runtime categories:

- source html scaffold
- engine runtime js
- JSON payloads
- blob payloads
- sound payloads
- image payloads
- video payloads
- source-only support files

### Stage 2: Core Transform Plan

Build a deterministic plan describing:

- which source scripts stay structurally intact
- which payloads become `window.jsons`
- which payloads become `window.blobs`
- which payloads become `window.sounds`
- which payloads become DOM-backed assets
- boot graph order
- decompression mode per asset category

### Stage 3: Shared Core Builder

Generate one network-agnostic single-html core that provides:

- decompression bootstrap
- worker pool bootstrap
- resource-store reconstruction
- DOM asset reconstruction
- source-compatible boot sequencing
- lifecycle hooks required by the Luna runtime

### Stage 4: Network Bridge Builder

Apply only the thin network tail:

- Google Ads bridge
- Mintegral bridge
- AppLovin bridge
- optional IronSource / Unity bridges when needed

### Stage 5: Emitters

Package the shared core per network:

- `GoogleAds/<name>.zip` containing one html
- `Mintegral/<name>.zip` containing one html
- `AppLovin/<name>.html`

### Stage 6: Verification

The replacement tool is not done unless it verifies:

- the final HTML reaches the expected boot milestones
- the core runtime reconstructs required resource stores
- no required source payload was silently dropped
- network tail injection did not alter the shared boot graph

## Immediate Implementation Rules

- Do not hardcode level-specific asset names.
- Do not infer architecture from asset-count differences alone.
- Treat `SandyJamLv15_build (2).zip` as raw Luna input, not LunaX output.
- Treat the current `LunaPlayableSingleHtmlPacker` as a legacy prototype, not the final architecture.
- Build the new pipeline around the shared core first. Network emitters come after the shared core can boot locally.
