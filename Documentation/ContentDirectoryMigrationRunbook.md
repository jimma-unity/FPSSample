# Content Directory Migration Runbook (FPSSample)

## Goal
Migrate runtime content loading from legacy bundle-only assumptions to role/versioned content directories while keeping Autobuild stable during Unity import-worker limitations.

---

## 1) Preconditions
- Unity project opens and builds successfully.
- Required roots exist:
  - `Assets/Resources/Content/SceneListRoot.asset`
  - `Assets/ContentRoots/ClientContentRoot.asset`
  - `Assets/ContentRoots/ServerContentRoot.asset`
- `Autobuild` output is writable.

---

## 2) Migration Modes
Menu:
- `FPS Sample/BuildSystem/ContentDirectories/MigrationMode/FallbackPreferred (Stable)`
- `FPS Sample/BuildSystem/ContentDirectories/MigrationMode/StrictOnly (Validation)`

Use:
- **FallbackPreferred (Stable)** for daily Autobuild/unblocked CI.
- **StrictOnly (Validation)** only in environments where `BuildPipeline.BuildContentDirectory` succeeds for current roots.

Notes:
- Autobuild flow forces `FallbackPreferred` internally for safety, then restores previous user setting.
- Strict-only validation path remains available via dedicated strict build menu entries.

---

## 3) Packaging Model (Target State)
Role/version output directories:
- `Autobuild/Content/Base/<build-id>`
- `Autobuild/Content/Client/<build-id>`
- `Autobuild/Content/Server/<build-id>`

For Autobuild-like builds, `<build-id>` is `AutoBuild`.

Expected fallback package shape under each role directory:
- `AssetBundles/`
- `AssetBundles/AssetBundles.manifest`
- `AssetBundles/bundledresources/client`
- `AssetBundles/bundledresources/server`
- `AssetBundles/bundledresources/client_assets/...`
- `AssetBundles/bundledresources/server_assets/...`

Expected strict package marker (strict validation environments only):
- `BuildManifestHash.txt` at role/version directory root.

---

## 4) Build/Launch Commands
### Build
- `FPS Sample/BuildSystem/Win64/CreateAutoBuildLike`
  - Stable/default Autobuild path.
- `FPS Sample/BuildSystem/Win64/CreateAutoBuildLike-ContentDirectoriesOnly`
  - Strict validation path; may fail on known import-worker limitation.

### Launch (generated automatically in Autobuild output)
- `Autobuild/preview_local.bat`
  - Local preview mode, no server required.
- `Autobuild/client_localhost.bat`
  - Client mode, expects local server.
- `Autobuild/server.bat`
  - Starts server process.

---

## 5) Runtime Resolution Rules
- Runtime bundle path is set to role/versioned fallback bundle directory via:
  - `res.runtimebundlepath "Content/Client/AutoBuild/AssetBundles"`
- Registry bundles are loaded from `bundledresources/client` and `bundledresources/server`.
- Single-asset bundles resolve from `bundledresources/client_assets/<guid>` (and server equivalent).
- GUID-based single-asset bundle naming is preserved for runtime compatibility.

---

## 6) Verification Checklist
### Packaging
- [ ] Directories exist:
  - `Autobuild/Content/Base/AutoBuild`
  - `Autobuild/Content/Client/AutoBuild`
  - `Autobuild/Content/Server/AutoBuild`
- [ ] `AssetBundles/bundledresources` exists in each role output.
- [ ] In strict validation environment only: `BuildManifestHash.txt` present in each role/version root.

### Launch
- [ ] Run `Autobuild/preview_local.bat`.
- [ ] `Autobuild/game.log` contains:
  - `cmd: preview level_01`
  - `Scene level_01 loaded`
  - `PreviewGameLoop: Content resolver backend: LoadableIndexed`
- [ ] No `Unable to open archive file ... bundledresources/client` errors.

### UX
- [ ] `Esc` opens/closes in-game menu in preview mode.

---

## 7) Known Limitations
- Unity import-worker limitation can block strict content-directory build for current roots.
- In fallback mode, strict manifest files (`BuildManifestHash.txt`) are not expected.
- Runtime content-directory registration that requires strict manifests is only valid when strict outputs are produced.

---

## 8) Rollback / Safety Switches
- If strict validation blocks builds:
  - Switch migration mode to `FallbackPreferred (Stable)`.
  - Use `CreateAutoBuildLike`.
- If runtime content asset resolution fails:
  - Verify `res.runtimebundlepath` points to `Content/Client/AutoBuild/AssetBundles`.
  - Rebuild Autobuild output to refresh scripts/config/content.

---

## 9) Cutover Status (Current)
- Legacy bundle build buttons in Project Tools are disabled after parity.
- Role/version packaging and generated launch scripts are in place.
- Partial WeakAssetReference retirement is done where guidKey/ownerGuidKey mappings exist.
- Full legacy backend removal should be done in a dedicated follow-up once strict-only parity is validated in target CI/editor environments.
