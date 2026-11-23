# Bus Jam Case

A Bus Jam–style colour-matching puzzle built for the Rollic Game Developer case.
Unity **2022.3.19f1**, URP 14, UI Toolkit, portrait mobile (reference 1080×1920).

Tap passengers to send them to the bus of their colour. If their bus isn't at the door yet, they wait in a slot. You lose when every slot is full or the timer runs out.

## Opening the project

1. Clone the repository and open the folder with **Unity 2022.3.19f1** (Unity Hub > Add > select the folder).
2. Package resolution needs **git** on PATH. `com.coplaydev.unity-mcp` is an editor-only development tool fetched from GitHub; the game doesn't use it.
3. Open `Assets/_BusJam/Scenes/Game.unity` and press Play. Set the Game view to **1080×1920** (portrait), or use the Device Simulator.
4. Android build: File > Build Settings > Android > Build. Scenes in the build: `Game` (0) and `LevelEditor` (1).

## Scenes

| Scene | Purpose |
| --- | --- |
| `Scenes/Game.unity` | The game: Start menu, Ready ("Tap to start"), gameplay, Complete and Fail screens. |
| `Scenes/LevelEditor.unity` | The level editor. It runs in Play mode and uses the same board, buses and visuals as the game. |

## Case requirements

| Requirement | Where |
| --- | --- |
| Start screen, timer as a constraint, tap to start | `GameState.Ready`: the board and the full, stopped timer are visible. The first tap starts play. |
| Timer fail condition | `LevelTimer` raises `Expired`, which leads to `GameManager.Fail(OutOfTime)`. |
| Level indicator | HUD shows "Level N". |
| Resume where you left off | `SaveSystem` (PlayerPrefs) stores the level index. Lives and coins persist too. |
| 5+ unique levels | **28** levels, every one validated and solver-checked (each has at least one solution). |
| Complete / Fail with Next / Retry | `ToolkitGameUI.Results` |
| Level editor scene: create, update, edit timer | `Scenes/LevelEditor.unity` (see below). |

Extra mechanics: hidden passengers ("?" until reachable), tunnels (a queue of passengers that emerges into the tile in front of the tunnel), roped passengers (each departing neighbour removes one rope), cloaked passengers (cloaks flip on every accepted tap), obstacles and holes. Also included: a five-life / 20-minute regeneration system, coins, slot unlocks by level, and tutorials on levels 1, 2, 3, 6 and 8.

## Architecture

The full design notes are in `Docs/ARCHITECTURE.md`. In short:

- **References from the Inspector, communication through C# `event`s, no `Find` calls.** The one deliberate singleton is `AudioManager`: sound effects are requested from many places (gameplay, every UI button), so it exposes a static `Play(Sfx)`. It also listens to game and bus events by itself.
- **No per-frame allocations.** Passengers and buses come from `UnityEngine.Pool.ObjectPool`. BFS buffers are allocated once. Idle systems don't run `Update`.
- **One place for the rules.** `GameplayController` decides tap results, reservations, auto-boarding and win/lose. `Board` is a plain C# class: occupancy, BFS pathfinding, and reachability via a single flood fill from the exit row.
- **Data:** `LevelData` (ScriptableObject: grid cells, bus order, tunnels, time limit) and `LevelDatabase` (ordered list). `LevelData.Validate` enforces the level rules. `LevelSolver` (editor-only DFS with memoisation) proves that a level is solvable.

```
Assets/_BusJam/Scripts/
  Core/      GameManager (state machine), LivesSystem, GameRules, AudioManager, layout
  Level/     LevelData, LevelDatabase, CellData, TunnelData, LevelSolver, auto-fill
  Gameplay/  Board, BoardView, GameplayController, Passenger, Bus, BusStation,
             WaitingArea, InputHandler, LevelTimer, TunnelView, camera fitting
  UI/        ToolkitGameUI (partial: HUD, results, layout, transitions), TapTutorial,
             CompleteCelebration, LevelEditor/ (editor scene UI)
  Save/      SaveSystem
  Editor/    SVG importer, ResultTimeline inspector
```

**Game flow:** `Start` → (Level button, lives check) → `Ready` → (tap) → `Playing` → `Complete` | `Fail`. Next, Retry and Restart load the level into `Ready`.

**Tap flow:**
1. A tapped passenger is accepted only if it is reachable, has no ropes and isn't cloaked.
2. Its destination (bus seat, waiting slot or overflow queue) is reserved immediately.
3. Its cell is freed and reachability is recomputed, so several passengers can run at once without conflicts.

**Rendering:** one URP mobile pipeline asset with HDR off, MSAA 2×, no realtime shadows and the SRP Batcher on. A single keyword-free toon shader (`BusJam/Toon`) is used for all gameplay objects.

## Level editor

1. Open `Scenes/LevelEditor.unity` and press Play.
2. **Load** an existing level from the list, or press **New**.
3. Paint the board:
   - Tap a tile to cycle Tile > Hole > Obstacle > Tunnel.
   - Pick a colour to place passengers. In the passenger brush you can set hidden, ropes (0–3) and cloak.
   - Tap a passenger or a tunnel to edit it. For a tunnel you can set its direction and its passenger queue.
4. Set the size, the **time limit** and the **bus order**.
5. **Solve** checks solvability. **Auto Fill** places the passengers the bus order still needs on empty tiles and retries until the solver finds solutions. **Test Play** plays the current draft without saving anything, and the "Editor" button returns to the draft.
6. **Save** writes the asset only if it passes `Validate` and the solver finds a solution. A new level is added to the end of `LevelDatabase` as `Level_NN`.

## Performance

Measured in the editor with the Device Simulator: iPhone X and Galaxy S10e, including a 30-level long-session run (details in `Docs/PERFORMANCE.md`).

- Busiest level (9×9, 40 passengers): ~2–3 ms of game CPU work per frame (p95), ~350 batches, ~230k triangles.
- 0 B/frame from game code. About 0.6 KB/frame comes from URP itself.
- No leaks over 30 consecutive levels: object, tween and memory counts reach a plateau.
- Mobile textures use ASTC compression.
- The frame rate drops to 30 FPS while the game is paused.

## Credits and AI usage

Third-party content is listed in `Docs/CREDITS.md`:
- Sound effects: Pixabay (Pixabay Content License).
- Fonts: Fredoka and Baloo 2 (Google Fonts, OFL).
- Libraries: DOTween, TextMeshPro.

**AI usage (open disclosure):**
- **Code:** written with AI coding assistants (Claude Code, OpenAI Codex). I wrote the design rules (`Docs/ARCHITECTURE.md`), made every gameplay and architecture decision, and reviewed and play-tested the results.
- **3D models** (bus, passenger, board kit, menu scene): created in Blender with AI-written Python (bpy) scripts. The source `.blend` files are in `ArtSource/`.
- **UI art:** the first PNG UI kit (`ArtSource/UI/`) was generated in ChatGPT with OpenAI's image model (gpt-image); the files carry C2PA provenance metadata. The in-game UI (`Assets/_BusJam/UI/Vector/*.svg`) was then written as SVG code by Codex: most elements were redrawn from scratch to match the reference gameplay video's style, and a few (tap hand, sparkle, glow) were traced from the ChatGPT kit.
- **App icon:** generated with OpenAI image generation (through Codex). Rounded corners were added afterwards.

No assets were taken from the original Bus Jam game.
