# Architecture

Bus Jam Case — Unity 2022.3.19f1, URP 14, UI Toolkit, portrait mobile (reference 1080×1920).

## Principles

- **Plain and readable.** No DI framework, no interface layers, no "manager of managers".
- **Inspector references and C# events.** Systems don't look each other up, and `Find` calls aren't used.
  - **Exception:** `AudioManager` is the one deliberate singleton. Sound is requested from gameplay and every UI button, so it exposes a static `Play(Sfx)`.
  - **Exception:** `EditorReturn` uses one `FindObjectOfType`, only in the editor's Test Play flow.
- **No per-frame allocations.**
  - Passengers and buses come from `UnityEngine.Pool.ObjectPool`. The bus pool is pre-warmed at level load.
  - BFS and path buffers are allocated once and reused.
  - Tweens that run repeatedly are created once and restarted.
  - Idle systems don't run `Update`. For example, `LevelTimer` is enabled only while it is counting.
- **Assets come through Inspector references**, not `Resources.Load`.
- **Editor-only code** lives in `Scripts/Editor` or inside `#if UNITY_EDITOR`. The level solver is not included in player builds.

## Code layout

| Namespace / folder | Contents |
| --- | --- |
| `BusJam.Core` | `GameManager` (state machine, level loading, progress), `LivesSystem`, `GameRules` (constants), `AudioManager`, `LayoutModeController` (safe area and aspect bands) |
| `BusJam.Level` | `LevelData`, `LevelDatabase`, `CellData`, `TunnelData`, `LevelSolver`, auto-fill helpers |
| `BusJam.Gameplay` | `Board`, `BoardView`, `GameplayController`, `Passenger`, `Bus`, `BusStation`, `WaitingArea`, `InputHandler`, `LevelTimer`, `TunnelView`, `CameraFitter`, background |
| `BusJam.UI` | `ToolkitGameUI` (partial: HUD and menus, `.Results`, `.Layout`, `.Transition`), `TapTutorial`, `CompleteCelebration`, `ResultTimeline` |
| `BusJam.LevelEditor` | The level editor scene UI: `LevelEditorPanel`, board input, viewport, return from Test Play |
| `BusJam.Save` | `SaveSystem` (PlayerPrefs) |
| `BusJam.EditorTools` | The SVG importer and the `ResultTimeline` inspector |

## Data

- **`LevelData`** (ScriptableObject) holds:
  - `width`, `height` (up to 9×9)
  - `CellData[] cells`, indexed `y * width + x`
  - `busOrder`, `timeLimit`
  - `tunnels`
- **`CellData`** has a type (`Empty`, `Passenger`, `Void` hole, `Tunnel`, `Obstacle`), a colour, and passenger modifiers: `hidden`, `ropes` (0–3) and `cloak`. New enum values are only appended, so existing assets stay valid.
- **`TunnelData`** holds a cell, a facing direction and an ordered passenger queue. The cell the tunnel faces is its exit.
- **`LevelData.Validate`** checks that:
  - each colour count (grid plus tunnel queues) equals that colour's bus count × 3;
  - the exit row is reachable;
  - tunnel exits are empty tiles;
  - ropes can actually be removed;
  - cloak combinations are legal.
- **`LevelDatabase`** is an ordered list. After the last level it loops back to the first, and the displayed level number keeps increasing.
- **`LevelSolver`** (editor only) runs a DFS with full-state memoisation and limits of 200,000 states or 5 seconds. Every shipped level has at least one solution. Levels with ropes or cloaks have at least two.

## Game flow

`GameManager` owns `GameState { Start, Ready, Playing, Complete, Fail }` and raises `StateChanged`.

```
Start (menu) --Level button, lives > 0--> Ready (board and stopped timer visible)
Ready --any tap--> Playing (timer runs)
Playing --last bus leaves--> Complete --Continue--> next level, Ready
Playing --slots full / out of time / no moves--> Fail --Retry--> same level, Ready
```

- **Progress** (level index, coins, tutorial flags) is saved through `SaveSystem`, so the game resumes from the last unlocked level.
- **Lives:** there are 5, and one regenerates every 20 minutes. Regeneration uses UTC and works offline, with a guard against the clock being moved back.
  - Fail, HUD Restart and Settings > Main Menu each spend one life. Retry after a Fail doesn't spend another.
  - Test Play from the level editor never touches real lives, coins or progress.

## Gameplay systems

- **`Board`** is a plain C# class, not a MonoBehaviour. It tracks occupancy, finds paths with BFS, and computes reachability with a single flood fill from the exit row (`y = 0`).
- **`GameplayController`** is the only place that applies the rules. It listens to `InputHandler.PassengerTapped`. When a tap is accepted:
  1. Reject the tap (angry feedback) if the passenger is unreachable, roped or cloaked closed.
  2. Pick a destination: the bus seat if the bus at the door matches the passenger's colour, otherwise a free waiting slot, otherwise the overflow queue if a slot is guaranteed to open.
  3. **Reserve** the destination immediately, free the passenger's cell, and recompute reachability. Hidden passengers reveal, ropes loosen, tunnels release their next passenger, and cloaks flip.
  4. The passenger runs along the BFS path. Several passengers can run at once without conflicts because every destination was reserved up front.
- **Win / lose:**
  - **Win:** the last bus departs.
  - **Lose:** the slots are still full after every pending run and bus arrival has settled; or the timer runs out; or no legal tap remains.
- **`BusStation`** runs the bus queue: arrival, boarding, departure, and bringing the next bus in. It raises `BusArrived`, `BusFilled`, `BusDeparted` and `AllBusesDone`. When a bus arrives, matching passengers waiting in slots board automatically.
- **`WaitingArea`** holds 7 fixed slots and their reservations. The two outer slots unlock by level number.
- **`InputHandler`** raycasts against the passenger layer with a cached camera, then raises `PassengerTapped` and `ScreenTapped`.
- **`LevelTimer`** raises `TimeChanged` only when the displayed second changes, and `Expired` when it reaches 0.

## UI

- **UI Toolkit:** `GameUI.uxml` and `Theme.uss`, with Scale With Screen Size at a 1080×1920 reference. The safe area is applied as padding on the root element.
- **Art:** UI art is SVG, rasterised by `AntialiasedSvgImporter`. Mobile uses ASTC.
- **`ToolkitGameUI`** listens to `StateChanged` and the gameplay events. The Complete and Fail animations are timed from a `ResultTimeline` asset, which holds named steps with start, duration and ease. That way, timing is tuned without code changes.
- **`TapTutorial`** guides the first encounter with each mechanic: level 1 (basic tap), 2 (hidden), 3 (tunnel), 6 (ropes) and 8 (cloaks).
  - It filters taps only while it is pointing at a target.
  - It never affects the fail checks.
  - It releases itself if its target becomes illegal.

## Level editor (`Scenes/LevelEditor.unity`)

- **Same building blocks as the game.** It runs in Play mode with the same prefabs (`Board`, `BusStation`, `WaitingArea`, lighting, background), so what you edit looks exactly like the game.
- **UI:** a UI Toolkit panel (`LevelEditorPanel`) handles:
  - load or new level;
  - size;
  - tile painting: Tile > Hole > Obstacle > Tunnel;
  - the passenger brush: colour, hidden, ropes, cloak;
  - tunnel direction and queue;
  - bus order and time limit.
- **Picking:** cells are picked from what is visible under the pointer. Passengers, tunnels and obstacles are tested as boxes; the centre of an empty tile selects the tile.
- **Saving:** **Save** writes only when the level passes `Validate` and the solver finds a solution. New levels are added to the end of `LevelDatabase`.
- **Background solving:** Solve, Save and Auto Fill run the solver on a background thread, so the editor never freezes.
- **Test Play** plays a copy of the draft and returns to the editor with the changes still there.

## Rendering and performance

- **Pipeline:** one URP mobile asset with HDR off, MSAA 2×, no realtime shadows and the SRP Batcher on. Post-processing is quarter-resolution Bloom and a vignette.
- **Shaders:** gameplay uses one keyword-free toon shader, `BusJam/Toon`. Other lit surfaces share the same lighting model.
- **Frame rate:** 60 FPS target, 30 FPS while paused.
- **Camera:** perspective, top-down at about 65°. `CameraFitter` pulls it back along the view axis for grids larger than the reference and for different aspect ratios.

Measurements are in `Docs/PERFORMANCE.md`.
