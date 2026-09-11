# Unity — ONE folder to open

## Open this in Unity Hub

`C:\Users\Chemeleon\Documents\git\legendsoftheuniverse`

Cursor edits **`Assets/Scripts/`** here. Unity only compiles scripts under **`Assets/`** — not any other folder.

## Why changes sometimes “don’t compile”

### 1. Wrong Unity project folder (most common)

Your Unity Editor log shows this path:

`C:\Users\Chemeleon\Legends Of The Universe`

Cursor’s workspace is:

`C:\Users\Chemeleon\Documents\git\legendsoftheuniverse`

Those are **two copies**. If Unity opens the old folder, Cursor edits the git folder — Unity never sees them unless you sync manually.

**Fix:** In Unity Hub, remove the old project and add **`legendsoftheuniverse`** (the git repo). Open only that one.

### 2. Enter Play Mode Options (fast play mode)

If **Enter Play Mode Options** skips **Reload Domain**, Play mode can keep running **old code** even after a successful recompile. The log line to watch for:

`Entering Playmode with Reload Domain disabled.`

**Fix:** Edit → Project Settings → Editor → disable **Enter Play Mode Options**, or turn **Reload Domain** back on. This repo’s `ProjectSettings/EditorSettings.asset` now has fast play mode **off**.

### 3. Unity didn’t refresh yet

After a file save, click the Unity window (or **Assets → Refresh**) and wait for the spinner in the bottom-right to finish. **Stop Play mode** before expecting script changes in a new run.

## Deprecated path (stop using)

`C:\Users\Chemeleon\Legends Of The Universe` — old duplicate. Do not open in Hub.

## Quick check that Unity picked up new code

In `Assets/Scripts/Presentation/HandFlowController.cs`, **`BeginOpeningHand` must NOT call `DealStoreRoutine`**. Store opens only in `AfterKeepSequence` via `BeginRoundRoutine` after the Icon pick.
