# ZBG — Backgrounds, counter plate and fonts

2026-11-19. Unity **2022.3.19f1**, BusJamCase. Completed in **Edit mode**; Play was never entered. One English ZBG commit, no push.

Resolved: the previous stop was overlapping capture requests while one was pending; sequential capture/status polling now completes and cleans up each capture before the next.

## ZBG-2 final results

- Found one DontSave `ZBG disposable Edit preview` with five children, HUD visible, demo lives 3 and countdown 12:34 in editor memory. Completed its pending callback, removed residual preview state and reloaded the saved production scene.
- Verified production defaults before commit: StartPanel active; HUD/Complete/Fail/NoLives inactive; HUD/Start lives 5; their countdown groups hidden, text 20:00; modal count 0 and `Next life in 20:00`; Level 1 / timer 30 / Start 30s; regeneration 20 minutes. No preview objects remain.
- Opened LevelEditor.unity: clean, no disposable/ZBG preview objects; left unchanged. Game.unity was reloaded, saved and left open. Game view restored to 1080×1920.
- counter_plate source borders **L 400 / B 140 / R 400 / T 140 px**; imported borders approximately **188.58 / 65.94 / 188.58 / 65.94 px**. Four HUD/Start life/countdown Images use **Sliced**. PPU multiplier **6.464285** at 270×112, **11.677419** at 270×62. Formula: sprite height / target height × canvas reference PPU / sprite PPU (100 / 47.09945 correction). The former missing PPU correction distorted the ends.
- Other HUD/Start sprites are Simple; the existing main gameplay timer is text only. No coin counter/plate exists; no coin economy or placeholder was invented. NoLives modal/header remain their own sliced artwork.
- Overwrote and inspected all four After captures at **1080×1920** and **1242×2688**, one request at a time with completion polling. UI, gold/cyan rims and rounded ends are visible; road, sidewalk and board alignment remain correct.
- Script compilation completed; no project compile errors/warnings. Refresh recovered its internal domain-reload reconnect; one MCP WebSocket warning was inspected and cleared. Final Console read: **0 entries**. One inspection snippet initially referenced an internal helper and was corrected; this was a snippet compiler error, not a transport failure.
- M_Purple, M_Yellow and ArtSource were neither edited nor staged. No Play acceptance was performed.
- After the production commit, the authorized ZM3 menu command creates the deliberately **uncommitted** one-minute / one-life / Level_01 seed; its activation is verified in the final response.

## Initial production restore

Read `Docs/ZUI-Report.md`, `Docs/ZM3-Report.md`, `Docs/MIMARI.md`, the ZF report and the vault prompt/common ending. Executed **Bus Jam > ZM3 > Restore Production (Edit)** first. Console confirmed regeneration 20 minutes, saved index 49 and fresh default five lives. Before package edits, `git status --short` showed only the existing M_Purple/M_Yellow changes and untracked ArtSource; `git diff -- Assets/_BusJam/Scenes/Game.unity` was empty.

## Picked and skipped sources

All three Downloads candidates were modified on 19 November after 15:04. Both selected files were visually inspected; step 1 was a sanity check of the supplied choices.

| Source in `C:/Users/Kullanici/Downloads/` | Modified | Selection |
| --- | --- | --- |
| `Renkli Park ve Yol Oyun Tahtası.png` | 15:10:17 | Picked: straight horizontal, single-lane park/road/grass; copied to `Assets/_BusJam/Art/Textures/bg_game.png`. |
| `Renkli Kasaba Otobüs Durağı.png` | 15:10:44 | Picked: town/menu; copied to `Assets/_BusJam/Art/Textures/bg_menu.png`. |
| `Renkli Alçak Poligon Park Manzarası.png` | 15:08:07 | Skipped: older park version, as explicitly instructed. |

The staged `counter_plate.png` was copied from the supplied `scratchpad/ui-stage/stage/` directory. The four supplied font/license files were copied from `scratchpad/fonts/` to `Assets/_BusJam/Art/Fonts/`.

## Band measurements and alignment

Both game background sources are **941 × 1672**. Measurements below use native source pixels, origin at the top. Boundaries include a few pixels of antialiasing/shadow. Samples at x = 100, 470 and 840 confirmed the new horizontal boundaries; the old boundaries descend about 18–21 pixels across those samples.

| Band | Old GameBG, center sample | New bg_game |
| --- | --- | --- |
| Upper decorative grass | y 0–212, **0–12.68%** | y 0–175, **0–10.47%** |
| Upper curb / edge | y 212–227, **12.68–13.58%** | curb y 175–206, **10.47–12.32%**; white edge y 206–216, **12.32–12.92%** |
| Asphalt road | y 227–369, **13.58–22.07%** | y 216–364, **12.92–21.77%** |
| Road/sidewalk edge | y 369–390, **22.07–23.33%** | y 364–404, **21.77–24.16%** |
| Walkable sidewalk slabs | y 390–491, **23.33–29.37%** | y 404–534, **24.16–31.94%** |
| Lower curb / shadow | y 491–506, **29.37–30.26%** | y 534–559, **31.94–33.43%** |
| Main grass | y 506–1672, **30.26–100%** | y 559–1672, **33.43–100%** |

The new game artwork envelopes normalized canvas frame **(x 0, y −0.3, width 1, height 1.3)** with uniform scaling and horizontal cropping. Its top stays at the screen top. Consequently asphalt occupies **16.79–28.30%**, sidewalk slabs **31.41–41.52%**, and main grass begins at **43.46%** of screen height, at both requested portrait ratios.

| Transform/setting | Before | Saved new setup |
| --- | --- | --- |
| Main Camera world position | (0, 21.060741, −11.320784) | **(0, 25.37662, −13.83331)** |
| Main Camera Euler rotation / scale | (65, 0, 0) / (1, 1, 1) | **(65, 0, 0) / (1, 1, 1)** |
| Main Camera vertical FOV | 40° | **40°**, fixed camera |
| Background Canvas | Screen Space Camera, Main Camera, distance 99, order −10 | Same mode/binding/distance/order; canvas position follows the camera automatically. |
| Background Canvas reported old transform | position (0, −68.6637, 30.5184), rotation (65, 0, 0), scale approximately 0.0375 | Camera-facing Canvas transform is derived by Unity; its pitch is required to face the camera and is not a road compensation. Final numeric readback was not completed. |
| Image name / sprite | GameBG / GameBG.png | **bg_game / bg_game.png** |
| Image local Euler rotation | (359.9529, 0.2925, 1.7669) | **(0, 0, 0)** |
| Image scale | (1.15, 1.15, 1.15) | **(1, 1, 1)**; size is fitted uniformly by CanvasArtwork. |
| Old image layout | anchors (0, −0.52)–(1, 1.08), local position (6, −540.3999, 0) | Centered envelope using the normalized frame above; no compensating yaw or roll. |
| Board world position | (0, 0, −0.75) | **(0, 0, −1.5)** |
| WaitingArea world position | (0, 0, 1.6) | **(0, 0, 1.2)** |
| DoorPoint world position | (0, 0, 4.2) | **(0, 0, 4.2)** |
| slotSpacing / SlotTile scale | 1.003 / 1.18 | **1.003 / 1.18**, retained |

The old image's approximately 1.77° roll and 0.29° yaw were the compensation. The camera already had zero yaw and roll.

### Completed geometric evidence

`ZBGSetup.ValidateGeometry` instantiated disposable production BoardView fixtures, built all ten levels plus a fully occupied **7 × 9 Empty-cell footprint**, and projected active renderer bounds, including base, rim and tunnel geometry, through the actual camera. Passenger bounds are excluded from the board-footprint test. All **22 board/grass checks** and both bus-body/asphalt checks passed after the final road-anchor adjustment.

| Measure | 1080 × 1920 | 1242 × 2688 |
| --- | --- | --- |
| Maximum board + rim horizontal bounds | **13.723–86.277%** | **5.836–94.164%** |
| Maximum board + rim top/bottom | **44.751–91.291%** | **44.751–91.291%** |
| Bus body bounds from screen top | **17.225–27.845%**, inside asphalt | **17.225–27.845%**, inside asphalt |
| Slot ground center from top | **36.427%** | **36.427%** |
| Sidewalk slab midpoint from top | **36.465%** | **36.465%** |

These are static Edit-mode geometry checks, not moving-bus or Play acceptance. The first geometry pass found the bus bounds slightly above asphalt at DoorPoint z 4.35; restoring z **4.2** passed the strict bound check. Existing level/data assets were not edited.

## Importers, counter and fonts

- `bg_game` and `bg_menu`: Sprite (2D and UI), Single, Full Rect, alpha transparency, no mipmaps, clamp, no NPOT rescaling, Normal compression, **2048** maximum; Android and iPhone overrides **ASTC 6×6 / 2048**. Old `GameBG.png` remains in place; the Game background Image now references bg_game.
- `counter_plate`: same ZUI conventions with **1024** maximum and ASTC 6×6 on both mobile targets. Source **2172 × 724**, imported **1024 × 341**. Final native borders **Left 400 / Bottom 140 / Right 400 / Top 140**, including the rounded caps and gold/cyan rims. Final pixel-density correction and appearance were verified in all four After captures.
- Added `UIArt.counter_plate` and assigned its Sprite. Replaced **four** panel_header fallbacks: the HUD and Start life plates and their small countdown plates. Each uses Sliced rendering with pixel density derived from sprite height / destination height to retain the capsule's vertical proportions.
- **No HUD coin counter exists in the current scene/code.** Coin behavior/counter creation is in the later ZG prompt. No fake number or coin economy was added; the shared counter sprite is ready for ZG. Therefore the requested coin-placeholder replacement has no existing target and remains reported explicitly.
- Created **Fredoka-Bold SDF** and **LilitaOne SDF**: SDFAA, sampling size 64, atlas **1024 × 1024**, padding **9**, one static atlas each. Fredoka contains all **191** printable ASCII (32–126) and Latin-1 (160–255) characters. Lilita contains **190**; its source lacks U+00AF, so its assigned Fredoka fallback supplies that glyph.
- TMP Settings default is **Fredoka-Bold SDF**. All **17** existing HUD/Start/Complete/Fail/NoLives TMP UI texts were assigned Fredoka and its shared material. Lilita is available for later large-number/title styling. Material uses dark outline **0.2**, underlay offset **(0.25, −0.35)**, softness **0.2**, alpha **0.65**. Both OFL licenses are present.

## Menu and capture evidence

`StartPanel/bg_menu` is the first sibling, behind the existing content. `CanvasArtwork` fits against the root canvas, so artwork can cover the full screen while interactive text/counters remain under SafeArea. Tap to start was moved to normalized SafeArea height **0.22**, timer to **0.16**, with white fill and dark outlined type. The inspected initial menu capture was readable on the lower grass, so no gradient strip was added. Explicit simulated safe-area inset testing remains incomplete.

The captures use the actual **Unity GameView RenderView**, fixed at the stated output sizes, in Edit mode. Disposable BoardView/WaitingArea fixtures and three static bus prefab instances show alignment; life values **3 / 12:34** are demonstrations, not live gameplay. The helper flips the D3D readback to PNG orientation, rebuilds UI meshes before rendering, and restores text/group/panel state after every capture. Final cleanup is confirmed.

| Capture | Evidence state |
| --- | --- |
| [Before HUD, 1080 × 1920](ZBG-Captures/Before-HudPanel-L04-1080x1920.png) | Captured and inspected; old compensated background. |
| [Before HUD, 1242 × 2688](ZBG-Captures/Before-HudPanel-L04-1242x2688.png) | Captured before artwork/alignment changes. |
| [After HUD, 1080 × 1920](ZBG-Captures/After-HudPanel-L04-1080x1920.png) | Final corrected plates, artwork and z 4.2 bus anchor; inspected. |
| [After HUD, 1242 × 2688](ZBG-Captures/After-HudPanel-L04-1242x2688.png) | Final corrected plates, artwork and z 4.2 bus anchor; inspected. |
| [Start, 1080 × 1920](ZBG-Captures/After-StartPanel-L04-1080x1920.png) | Final menu, readable text and corrected plates; inspected. |
| [Start, 1242 × 2688](ZBG-Captures/After-StartPanel-L04-1242x2688.png) | Final menu, readable text and corrected plates; inspected. |

All four ZBG-2 final captures are complete. Additional maximum-board and Complete/Fail/NoLives screenshots and simulated safe-area insets were not part of this final capture set; the earlier geometric checks remain recorded above.

## Changed files and verification

| File/group | Change and purpose |
| --- | --- |
| `Art/Textures/bg_game.png`, `bg_menu.png` + metas | Supplied artwork and importer configuration. |
| `Art/UI/counter_plate.png` + meta | Staged capsule plate, measured native slice borders. |
| `Art/Fonts/` + folder/file metas | Two TTFs, licenses and embedded TMP font/material/atlas assets. |
| `Scripts/UI/UIArt.cs`, `UI/UIArt.asset` | Shared counter Sprite reference. |
| `Scripts/UI/CanvasArtwork.cs` + meta | Inspector-assigned canvas/Image references; uniform envelope on enable/dimension changes, including full bleed behind SafeArea. No Update, runtime lookup, or per-frame allocation. |
| `Scripts/Editor/ZBGSetup.cs` + meta | Repeatable imports/fonts/scene setup, PPU-correct slicing, disposable Game-view capture and projected-bound checks. Editor-only. CaptureStatus advances only this helper's deferred callbacks when the unfocused editor defers them. |
| `Scenes/Game.unity` | Camera/board/slot/background/menu/font/plate assignments with production regeneration. |
| `Assets/TextMesh Pro/Resources/TMP Settings.asset` | Fredoka default; Unity also reserialized existing settings. |
| `Docs/ZBG-Captures/`, this report | Final visual evidence and results. |

The first helper compilation found an incorrect Board constructor call; it was corrected to `Board.Setup(level)`. Final script compilation completed successfully. Asset refreshes returned success with internal domain-reload reconnect recovery. The transport warning was inspected and cleared; final Console read had **0 entries**. No runtime gameplay/lives behavior changed, and the ZM3 regression suite was not rerun.

`M_Purple.mat`, `M_Yellow.mat` and `ArtSource/` were untouched. Their existing changes remain outside this package. Production regeneration is committed at 20 minutes; the requested post-commit ZM3 seed remains deliberately uncommitted. LevelEditor.unity was inspected and needed no change.
