# AGENTS.md

## Cursor Cloud specific instructions

This repository is a **single Unity 6 game** (`DungeonSoul`), pinned to Unity Editor
`6000.4.9f1` (see `ProjectSettings/ProjectVersion.txt`). There is no Node/Python/web
backend, no `package.json`, and no automated test suite. "Development" means running the
project in the Unity Editor; the shipping target is **WebGL** (see
`Assets/Editor/ItchWebGLBuild.cs`, menu `DungeonSoul/Build/...`).

### Toolchain (already installed in the VM snapshot)
- Unity Editor `6000.4.9f1` lives at `/opt/unity/editors/6000.4.9f1/Editor/Unity`,
  symlinked to `unity-editor` on `PATH`. The **WebGL** build module and the bundled
  StandaloneLinux64 (Mono) build support are both installed. Do NOT add the multi-GB
  editor download to the update script — it is captured by the snapshot.
- System libraries Unity needs at runtime (GTK/NSS/GL/X11 + `xvfb`) are installed.
- Run headless GUI/editor commands under `xvfb-run` if a display is required, e.g.
  `xvfb-run -a unity-editor -batchmode ...`. Pure `-batchmode -nographics` build/test
  invocations do not need a display.

### Git LFS (REQUIRED — do this every session)
All sprites/audio/art are stored in **Git LFS**. A fresh checkout contains pointer files
only; the game will be missing every sprite/sound until you materialize them. The update
script runs `git lfs install` + `git lfs pull`; if assets look like 130-byte text
pointers, run `git lfs pull` again. (One unused TextMesh Pro example font may stay a
pointer — harmless.)

### Unity license (MANUAL, one-time — REQUIRED before build/run)
The Editor refuses to compile/build/run without an activated license
(`No valid Unity Editor license found`). The license is **machine-bound** to
`/etc/machine-id`, which is part of the snapshot, so a license activated once during
setup persists across future runs. To activate a free Personal license:

1. Generate the activation request (no login needed):
   `unity-editor -batchmode -nographics -logFile /tmp/alf.log -createManualActivationFile -quit`
   → produces `Unity_v6000.4.9f1.alf`.
2. Upload that `.alf` to <https://license.unity3d.com/manual>, pick **Unity Personal**,
   and download the resulting `.ulf`.
3. Activate:
   `unity-editor -batchmode -nographics -logFile /tmp/act.log -manualLicenseFile <file>.ulf -quit`

If the team prefers, store the `.ulf` XML as a secret and write it to a file before
activating. A Pro/Plus serial (`-username`/`-password`/`-serial`) also works and is not
machine-bound.

### Build & run (after license is active)
- **WebGL (shipping target):**
  `unity-editor -batchmode -nographics -projectPath . -executeMethod ItchWebGLBuild.BuildForItch -quit`
  Output goes to `Builds/WebGL/`. Serve it with any static server
  (`python3 -m http.server` from the build dir) and open in a browser.
- **Quick Linux player (no extra module needed):** use `BuildPipeline.BuildPlayer` with
  target `StandaloneLinux64` via a one-off `-executeMethod`, then run the binary under
  `xvfb-run` (or on the desktop) for end-to-end gameplay testing.
- **Open in Editor / Play Mode:** `unity-editor -projectPath .` (needs a display).
- Scene/build order: `MainMenu` → `CharacterSelectScene` → `WeaponSelectScene` →
  `SampleScene` (gameplay arena). Custom editor actions are under the `DungeonSoul/` menu.

### Lint / Test
- No linter and no automated tests exist. "Lint" = a clean compile in the Unity Console.
  QA is manual Play-Mode testing; a checklist lives in `docs/Refactor-Report.md` §6
  (docs are in Vietnamese).
