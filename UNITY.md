# Unity project layout

## Two folders — why?

| Location | Role |
| --- | --- |
| **`C:\Users\Chemeleon\Legends Of The Universe`** | **Open this in Unity.** Scenes, prefabs, audio, full `Assets/`, builds. |
| **`C:\Users\Chemeleon\Documents\git\legendsoftheuniverse`** | **Git / source control.** C# in `Scripts/`, card art in `Assets/Cards/`, catalog tools in `catalog/`. |

They are the same game, but only the Unity folder is a complete project. The git repo holds scripts and shared assets; changes are copied into the Unity project (or vice versa) until we merge into one root.

**Recommendation:** Treat **`Legends Of The Universe`** as the workspace. After editing scripts in git `Scripts/Presentation/`, sync to `Legends Of The Universe/Assets/Scripts/Presentation/`. Put new art in **both** `Assets/Arenas/` and `Assets/Playmats/` under the Unity project, then commit from git when those paths exist there too.

You can delete the empty stub `Legends Of The Universe/Assets/legendsoftheuniverse/` — it is not used.

## Arena vs playmat (table)

```
Table (prefab) + DojoArenaView
├── DojoWalls      ← FBX wall panels   Assets/Arenas/10thPlanetDojo/DojoWalls/
├── PurpleMat      ← FBX purple mat    Assets/Arenas/10thPlanetDojo/PurpleMat/
├── Playmat        ← optional flat plane (hidden when mesh playmat is on)
└── DeckPoint
```

- **`DojoWalls/`** — Meshy wall FBX around the table.
- **`PurpleMat/`** — Meshy purple floor mesh + textures; cards align to `PlaymatZones` on top.
- **`Assets/Playmats/`** — flat JPG playmat (shown by default).

On **Table** prefab, **Dojo Arena View** loads the Meshy **PurpleMat** FBX + albedo texture automatically. When the mesh is oriented and sized correctly, the flat JPG playmat hides itself. Right-click the component → **Rebuild Dojo Arena** after changing scale/rotation. Verify **Purple Mat Model** points at `PurpleMat/...fbx` (not the walls FBX).

## Paths

| What | Unity path |
| --- | --- |
| Arena prefabs | `Assets/Arenas/10thPlanetDojo/` |
| Purple playmat image | `Assets/Playmats/10th Planet.jpg` |
| Table prefab | `Assets/Table.prefab` |
| Card zones (code) | `Assets/Scripts/Presentation/PlaymatZones.cs` |
