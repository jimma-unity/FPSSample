# Unity Bug Report: BuildContentDirectory + Unity.Entities import worker dependency import

## Summary
`BuildPipeline.BuildContentDirectory` fails in FPSSample when root assets include Entities scene-reference data (`LoadableScene`).

Error observed repeatedly on worker threads:

`ArgumentException: Importing dependent assets on an import workers is currently not supported`

## Environment
- Project: FPSSample
- OS: Windows 11 x64
- Repro editor (user-reported): `C:/Program Files/Unity/6000.5.0a8_baed6e5c4658/Unity.exe`
- Also observed in project with `com.unity.entities@f1a74678b079`
- Package callstack points into:
  - `Unity.Scenes.AssetDependencyTracker<T>.AddCompleted`
  - `Unity.Scenes.ResolveSceneReferenceSystem.OnUpdate`

## Repro Steps
1. Open FPSSample in Unity Editor.
2. Run menu item:
   - `FPS Sample/BuildSystem/Repro/Run FPS Sample Shape Repro (Default Workers)`
   - (or the Workers=0 requested variant)
3. Observe Console output during build content-directory phase.

## Actual Result
Console logs repeated worker-thread exceptions such as:

- `[Worker7] ArgumentException: Importing dependent assets on an import workers is currently not supported`
- `[Worker6] ArgumentException: Importing dependent assets on an import workers is currently not supported`
- `[Worker8] ArgumentException: Importing dependent assets on an import workers is currently not supported`
- `[Worker9] ArgumentException: Importing dependent assets on an import workers is currently not supported`

Representative callstack:
- `Unity.Scenes.AssetDependencyTracker<T>.AddCompleted (...)` (`Unity.Scenes/AssetDependencyTracker.cs:181`)
- `Unity.Scenes.AssetDependencyTracker<T>.GetCompleted (...)` (`Unity.Scenes/AssetDependencyTracker.cs:167`)
- `Unity.Scenes.ResolveSceneReferenceSystem.OnUpdate ()` (`Unity.Scenes/ResolveSceneReferenceSystem.cs:167`)
- `Unity.Entities.SystemBase.Update ()` (`Unity.Entities/SystemBase.cs:205`)
- `Unity.Entities.ComponentSystemGroup.UpdateAllSystems ()` (`Unity.Entities/ComponentSystemGroup.cs:734`)

## Expected Result
`BuildPipeline.BuildContentDirectory` should either:
1. complete successfully with these supported root asset patterns, or
2. fail deterministically with a single clear, top-level error and guidance,
without repeated worker-thread exceptions during dependency resolution.

## Root Assets Used in Repro Shape
- `Assets/ContentRoots/ClientContentRoot.asset`
- `Assets/ContentRoots/ServerContentRoot.asset`
- `Assets/Resources/Content/SceneListRoot.asset`

## Impact
- Blocks migration to ContentDirectories-only pipeline in this project.
- Forces fallback to legacy bundle build path for reliable Autobuild output.

## Current Workaround
- Use fallback path (`BuildBundles`) when known Entities scene-reference roots are present.
- Keep strict ContentDirectories-only path available for validation, but expect failure in affected editor/package combinations.

## Migration Status (FPSSample)
- [x] Runtime registration layer added via `RuntimeContentDirectoryRegistration`.
- [x] Startup registration in `ClientGameLoop`, `ServerGameLoop`, and `PreviewGameLoop`.
- [x] Registration handles tracked and unregistered on shutdown/leave-state.
- [x] Quit path explicitly forces unregister before process exit.
- [x] Hardcoded bundle-path assumptions replaced with shared registry constants in loop resource-manager setup.
- [ ] ContentDirectories-only pipeline enabled as default (blocked by Unity.Entities import-worker exception described above).

Latest validation signal:
- Observed runtime unregister log on quit, e.g. `ClientGameLoop: Unregistered content directory: C:/UnitySrc/FPSSample/Autobuild/Autobuild_Data`.

## Notes
- A smaller external repro project did not initially reproduce until project shape included FPSSample-like scene-reference roots and systems context.
- The FPSSample UI repro is currently the strongest signal for the issue.
