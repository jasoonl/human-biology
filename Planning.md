# Planning: Interactive 3D Human Body Explorer (Unity)

Source spec: [`Interactive_3D_Human_Body_Explorer.md`](Interactive_3D_Human_Body_Explorer.md) — a 100-phase "master-grid" describing a full commercial-scale medical simulation product (HDRP/URP tiered rendering, VR/AR, DOTS/ECS microscopic sims, DICOM/MRI volumetric rendering, multiplayer classrooms, native C++ plugins, ML segmentation, CI/CD).

This plan translates that spec into an honest, buildable sequence for a single local Unity project, built and verified incrementally in the Unity Editor. Reality check up front:

- **Environment constraints.** No VR/XR headset, no AWS account/S3 bucket, no DICOM/MRI datasets, no dedicated Python ML backend, no multiplayer relay credentials, no code-signing certs, no CI runners are available in this environment. Phases requiring those are stubbed/interfaced but not connected to real external services.
- **"Perfect" is not attainable for a 100-phase AAA spec in one sitting.** This plan targets a working, testable vertical slice per module, expanding breadth only as each layer is verified in-Editor.
- **Unity version:** 6000.6.0f1 (Apple silicon), installed via Unity Hub CLI (6000.0.83f1 was originally targeted but the download failed in the assistant's sandboxed shell — network to download.unity3d.com is only reachable from the user's own Terminal, which is what actually completed the install and landed on 6000.6.0f1 instead). Render pipeline: URP (cross-platform, matches Tier 2/3 budgets in the spec; HDRP Tier 1 path deferred). Package versions (Addressables 4.0.1, Input System 1.20.0, Splines 2.9.1) had to be bumped well past the versions named in earlier plans because 6000.6 renamed `Object.GetInstanceID()` → `GetEntityId()` and older package releases fail to compile against it as hard errors, not warnings.

- **Placeholder assets.** No licensed anatomical meshes are available. Primitive/procedural placeholder meshes (capsules, spheres) stand in for "Mesh_Heart_LeftVentricle_LOD0" etc., tagged so real assets can be dropped in later without code changes.

## Status: all 8 modules implemented and verified (2026-09-14)

All 100 phases of the spec have a corresponding implementation in this repo, verified by actually compiling and running in the real Unity 6000.6.0f1 Editor (batch-mode `-quit` for compile checks, `-runTests` for EditMode/PlayMode) after every module — not just written and assumed correct. Final counts: **80/80 EditMode tests, 4/4 PlayMode tests passing**, 12 hand-written shader/compute files confirmed compiling via the Editor's own ShaderImporter/ComputeShaderImporter logs.

Per-module summary (see individual commit messages for full detail on every deviation):

| Module | Phases | Status |
|---|---|---|
| I — Bootstrapping, Data, DI | 1-10 | Fully implemented & tested |
| II — Camera, Input, Raycasting | 11-22 | Fully implemented & tested |
| III — Rendering & Shaders | 23-38 | Implemented via hand-written HLSL/ShaderLab (not Shader Graph assets) for reliable headless verification; Phase 28 (FEM soft body) is an explicit no-op stub |
| IV — Microscopic Simulation | 39-48 | Implemented in plain C#/MonoBehaviour instead of ECS/DOTS+Burst (not installed, unverifiable at scale here) |
| V — Gamification & UI | 49-62 | Fully implemented; 3 phases substitute a working equivalent for an unavailable dependency (Windows-only speech API, iTextSharp, native TTS bridge) |
| VI — Multiplayer & XR | 63-75 | Stub-only: no Netcode/Relay/Vivox/XR headset in this environment |
| VII — DICOM/AI Segmentation | 76-85 | Mixed: real implementations where no missing dependency blocks it, explicit stubs where one does (native parser, Python backend, Marching Cubes, cloud streaming, normal baking) |
| VIII — Optimization, Security, CI/CD | 86-100 | Fully implemented except Phase 91 (no Dotfuscator license); CI workflow written but never executed (no `UNITY_LICENSE` secret) |

**What "verified" means here, precisely:** every module's code was compiled in the actual Unity Editor via headless batch mode and its automated tests were actually run and passed, with real XML test results — not just written and assumed to work. What it does *not* mean: visual/artistic correctness of any shader or particle effect (no display was ever used to look at the rendered output), gameplay feel, or behavior at the scale the spec describes (100k-entity ECS crowds, VR framerates, multiplayer under real network conditions) — none of that is checkable without hardware/services this environment doesn't have.

**Repo:** [github.com/jasoonl/human-biology](https://github.com/jasoonl/human-biology)

## Milestones (mapped to spec modules)

### M0 — Project Bootstrap (this session)
- Unity 6 project created at `HumanBodyExplorer/`, URP template.
- Packages: Newtonsoft.Json, Addressables, Input System, TextMeshPro, NUnit (Test Framework), UI Toolkit.
- Folder structure under `Assets/`: `Scripts/{Core,Data,Camera,Input,UI,Tests}`, `Data/`, `Prefabs/`, `Scenes/`, `Addressables/`.

### M1 — Module I: Bootstrapping, Data & DI (Phases 1–10)
Implements: GameBootstrapper/GameManager + GameState enum, custom `[Inject]` DI container, JSON anatomy dictionary loader (Newtonsoft), SQLite `userdata.db` + `UserProgress` table, SM-2 spaced-repetition tracker, Addressables asset streaming manager, WebGL cloud manifest stub, NUnit data/unit tests, AES-encrypted SecurePrefs, and a state-machine (`AppStateMachine`) with 4 concrete states.
**Verification:** Play Mode boot logs all subsystems initialized in order; EditMode/PlayMode tests green in Test Runner.

### M2 — Module II: Camera, Input & Raycasting (Phases 11–22)
Orbital camera with SmoothDamp + gimbal clamp + collision spherecast, New Input System action maps (mouse/touch — XR bindings stubbed, no headset to test), multi-touch gesture interpreter, bounds auto-focus, non-alloc raycaster + node-selection event, simple BVH prefilter, exploded-view lerp controller, slice-plane globals, isolation/ghosting via MaterialPropertyBlock, minimap camera, spline flythrough (Unity Splines package), cutaway sphere shader globals.
**Verification:** manual in-Editor interaction test — orbit, zoom, click-select, explode slider, slice plane — on placeholder anatomy prefab.

### M3 — Module III: Rendering & Shaders (Phases 23–38, reduced scope)
URP-compatible Shader Graph equivalents only (HDRP SSS/raytracing from the spec is out of scope without HDRP + a GPU capable target confirmed). Boolean clip node, backface cap material, render-queue-based transparency sort, vertex-color muscle bulge, X-ray Fresnel shader, post-process bloom/DoF link, colorblind renderer feature, blood-flow UV scroll. FEM soft-body (ML-Agents/ONNX), volumetric raymarching of real DICOM, capillary L-system compute, GPU point-cloud renderer are stubbed with clear `NotImplemented`/TODO markers — they need real volumetric data and compute budgets this environment cannot supply.

### M4 — Module IV: DOTS/ECS Micro-sim (Phases 39–48, reduced scope)
Requires Entities/Burst packages. Scale-dive coroutine, minimal ECS blood-cell job + instanced renderer as a proof of concept (100s–1000s of cells, not 100,000 without profiling on real hardware). Immune-response/flocking/pharmacology/gas-exchange/sarcomere/action-potential visualizers implemented as simplified MonoBehaviour coroutines first; ECS-ified only if M4 proof-of-concept profiles cleanly.

### M5 — Module V: Gamification & UI (Phases 49–62)
Label billboarding/repulsion, procedural heartbeat audio/visual sync, pathology severity blend-shape + color lerp, quiz controller wired to StudyTracker, streak multiplier, assembly/drag-drop mode, object pooling, PDF export (iTextSharp via NuGet-for-Unity), TTS via macOS `NSSpeechSynthesizer`/`System.Speech` equivalent (platform-gated), audio occlusion low-pass, Unity Localization tables (EN only initially), UI accessibility validator editor script, performance-monitor overlay. Voice recognition (`UnityEngine.Windows.Speech`) is Windows-only — stub with a warning on other platforms.

### M6 — Module VI: Multiplayer & XR (Phases 63–75) — deferred / stub-only
Requires Netcode for GameObjects, Unity Relay account, Vivox/Photon Voice license, and an XR headset for any real testing. Scripts will be written to the spec's interfaces but explicitly marked untested — no relay credentials or XR hardware exist in this environment.

### M7 — Module VII: DICOM/AI Segmentation (Phases 76–85) — deferred / stub-only
Requires a native C++ plugin toolchain, real `.dcm` datasets, and a Python 3D U-Net backend. Out of scope without those. Interfaces stubbed for future integration.

### M8 — Module VIII: Optimization, Security, CI/CD (Phases 86–100)
AES telemetry encryption, binary session save/load, Addressable leak detector editor tool, raycast/serialization integration tests, WebGL touch overrides, dynamic resolution scaling, shader variant stripping, battery-aware frame rate, SSAO zoom-tuning, async scene loading, GC/unload defrag utility, crash-log breadcrumb hook (Sentry SDK optional/stubbed — no account), GitHub Actions build workflow (written, not run — no `game-ci` license/runner in this environment to verify).

## What "done" means for this pass
- M0–M2 fully implemented and manually verified running in the Unity Editor (Play Mode) on this machine.
- M3–M5 implemented for the parts that don't need external assets/hardware; gaps clearly marked.
- M6–M7 stubbed to the documented interfaces only, explicitly called out as untested.
- M8 implemented where mechanically verifiable locally (encryption round-trip tests, serialization tests, editor tools); CI workflow written but not executed.

## Immediate next steps (this session)
1. Finish Unity Editor install (6000.0.83f1, background).
2. Create Unity project via CLI (`-createProject`), add packages via manifest.
3. Implement Phases 1–10, run NUnit tests in Editor batch mode to verify.
4. Continue to M2 camera/input, verify interactively.
5. Report status honestly per milestone rather than declaring the whole 100-phase spec "done."
