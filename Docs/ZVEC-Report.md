# ZVEC — Vector UI artwork

The original sections record v1. The v2 correction and current measurements are at the end.

21 November 2026. Baseline: `main`, `fefcd04`. Unity **2022.3.19f1**, Android target. MCP was pinned to `BusJamCase@c4dcbf17c52e6f1b`; project root and Edit mode were checked before execution. No Play mode, build, push or AsenOS access.

## Converted artwork

All green/red/yellow `.art-button` and `.editor-button` controls, `.counter`, `.timer-counter`, `.level-badge`, `.modal` (including the editor ScrollView modals), `.confirm-modal`, `.modal-heading`, and `.sound-toggle` with its round indicator and checked tick now use UI Toolkit elements and USS. The existing editor workspace surfaces were already USS shapes and retain their styling.

`Theme.uss` no longer references `btn_green`, `btn_red`, `btn_yellow`, `panel_inner`, `panel_header`, `panel_modal`, `badge_level` or `btn_round_blue`. Their PNG files remain on disk. Logo, icons, coin pile, tap hand, effects and passenger icon remain raster. The tutorial's separate `panel_tip` is outside the requested selector list and remains raster.

No background, scene, gameplay, 3D, lighting, material or level-data changes are part of these commits. The pre-existing local changes to LevelDatabase, M_Purple, M_Yellow, Level_26, ArtSource and Otobus Materials were excluded.

## Source sampling

Spatial samples reuse the original `SampledColours.json` opaque bounds. `sample_regions.py` takes channel medians from small, named, fully opaque regions; it does not choose colours by global frequency. Coordinates, pixel counts and exact hex values are in `SpatialColours.json`; `SpatialSamples.png` marks the regions.

| Source PNG in `Assets/_BusJam/Art/UI` | Body | Outer rim | Bottom depth | Top gloss sample |
| --- | --- | --- | --- | --- |
| `btn_green.png` | `#71DB06` | `#522100` | `#963D01` | `#CFF583` |
| `btn_red.png` | `#E1020A` | `#6D2901` | `#702200` | `#FC6D6D` |
| `btn_yellow.png` | `#FDE906` | `#7E3001` | `#963701` | `#FCF69A` |
| `panel_inner.png` | `#EFEFF1` | `#ACACAD` | `#CBCBCD` | `#FEFEFE` |
| `panel_header.png` | `#01286E` | `#0048C2` | `#0043E1` | `#1D57AA` |
| `panel_modal.png` | `#095DF3` | `#A5C4FC` | `#86A0FC` | `#48B3FB` |
| `badge_level.png` | `#E8EBF5` | `#8C23DE` | `#B145F9` | `#F1F1F4` |
| `btn_round_blue.png` | `#015DF3` | `#013EAA` | `#011847` | `#3BC0FB` |

Additional sampled edges: gold `#FDC808`, pale gold `#FDF294` / `#FDF3AD` / `#FDF6A7`; blue `#0D9CFC`, `#1FA3FB`, `#97BAFB`, `#E4F2FD`; purple `#CC71FB`, `#610C9C`. Face shading interpolates through the sampled body colour, between these spatial endpoints: green `#A5EE3C`–`#219600`, red `#F53136`–`#800203`, yellow `#FDF158`–`#ED9702`, header `#0E4090`–`#011444`, modal `#0D73FA`–`#0543E1`, round blue `#219BFC`–`#0032CD`. Gloss is composited as translucent lighter strips and white glints; the table records the source composite colour rather than claiming an exact alpha reconstruction.

## Helper API

`BusJam.UI.VectorArt.Apply(root)` is idempotent and runs when each controller caches a new document root. It inserts non-pickable decoration behind existing content: dark rounded rim, pale/coloured edge, body with a thick bottom border, clipped face, translucent top gloss and glints. Coloured faces use 32 solid element bands to reproduce source shading without USS gradients, box shadows, shaders, generated textures or PNGs. No Painter2D was necessary.

UXML uses `<art:VectorButton>` for the replaced buttons. It inherits native `Button` activation and exposes the same virtual `text` property through a foreground Label. The Label participates in intrinsic sizing, preserving the editor's text-sized button widths. Existing `Q<Button>`, `clicked`, enabled state, focus and controller APIs continue to work. No new update loop, scheduler or event subscriptions were added by the helper.

USS supplies 0.10-second press transitions: bottom depth 13→4 logical pixels and downward translation 5 pixels; editor buttons use 7→2 and 3 pixels. Hover changes gloss tint. Native pseudo-state captures confirm the depth reduction and translation; scaled border values undergo normal pixel rounding.

## Raster imports

All 16 retained UI textures referenced by the shared sheet have mipmaps enabled and Trilinear filtering. Their Android **ASTC 4×4** overrides are unchanged. A baseline diff audit allows only `enableMipMap: 0→1` and `filterMode: 1→2`; no other import fields changed. `bg_menu` and all backgrounds were excluded.

## Evidence and checks

Evidence is outside the repo: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/`.

- `ContactSheet-Before.png`, `ContactSheet-After.png`: Start, HUD, Settings, Complete, Fail, NoLives and LevelEditor at **1080×1920, 1080×2400, 1170×2532, 1536×2048, 1080×1080, 1768×2208, 2208×1768, 1920×1080, 2560×1080**. Each `Compare-<screen>.png` places the same sizes before/after.
- `Before/`: 99 captures, including menu settings, confirmation and editor popup states. `After/`: 126 captures, adding hover, pressed and sound-off states at every size. Settings is the in-game settings panel, so the sound control is included.
- `Compare-Button-3x.png`, `Compare-Panel-3x.png`, plus separate before/after crops: matched coordinates at 3× nearest-neighbour zoom.
- Temporary preview scenes, cloned PanelSettings and offscreen render textures render UI over a flat clear colour. Menu keeps its existing background. No world rendering or scene saving was used.
- `Verification.json`: all 126 requested before/after images have correct dimensions and nonblank content; **819 matched control-bound records, zero changes**; 16 importer diffs verified; zero replaced PNG references.
- `Interactions.txt`: native Restart/Main Menu/Cancel click bindings, `VectorButton.text`, sound OFF/ON and non-pickable decoration passed in Edit mode. `ButtonStates.txt` records pressed/hover depth and translation.

## Retention check

The temporary Edit-mode harness warms each controller for 20 cycles, then opens/closes all applicable screens and confirmations 50 times, including controller disable/enable and repeated idempotent decoration. Editor popups use the existing visibility mechanism without changing the draft or world.

| Measurement, before → after 50 cycles | Game | LevelEditor |
| --- | --- | --- |
| Visual elements | 929 → 929 | 599 → 599 |
| Native callbacks plus Button.clicked delegates | 336 → 336 | 679 → 679 |
| Callbacks while disabled | 316 | 608 |
| Managed heap, bytes | 731717632 → 728125440 | 647069696 → 647041024 |

Element and callback growth is **zero**; managed heap did not grow. Heap measurements are editor-wide GC snapshots, not a device allocation profile. Exact output: `LeakCheck.txt`. The temporary harness is preserved outside the repository and removed from Assets after verification.

## Open issues and limits

The replacement preserves source palette, silhouette proportions, layout and glossy/depth treatment, but is **not pixel-identical** to the painted PNGs. Solid shading bands, geometric glints and USS borders approximate their continuous shading, irregular highlights and soft painted bevels; differences are visible in the 3× comparisons. No new visual theme was introduced.

The layered approach increases retained element counts. It adds no per-frame application code, but mobile GPU/UI rendering cost still needs a device profile. Gameplay, saved rewards/lives, sound playback, device touch and world/UI composition were not exercised under the Edit-mode-only restriction.

Final validation: script compilation clean; Console **0 entries**. No scene changes or temporary review objects remain.

Implementation commit: `cd7aab7`. Retained raster import commit: `d8cf6d0`. Both are on `main`; no push.

## v2 — Cached gradients and smooth face edges

21 November 2026. Removed the 32 `vector-tone` children and `.vector-face` overflow mask. Each palette now shares one static-cached **1×64 RGBA32** texture: Clamp, Bilinear, no mipmaps, `HideFlags.DontSave`. The sampled top/body/bottom colours are unchanged. `StyleBackground` stretches the texture to the face; its border radius supplies the smooth rounded edge without a stencil mask. Eight textures retain identical instance IDs across a real editor domain reload; no runtime destruction is used. After warm-up, 1,000 idempotent `Apply` calls allocated **0 bytes** on the calling thread.

Each surface has **six decorative elements** (plus the existing v1 caption on VectorButton). The secondary glint dot was removed; highlight/glint insets now keep them within the face without clipping. Rim/body/face dimensions, depth bands, hover tint and press transitions remain unchanged. The sound control has separate plate/indicator surfaces and its existing tick.

| Visual elements | Original `fefcd04` | ZVEC v1 | ZVEC v2 |
| --- | ---: | ---: | ---: |
| Game | 174 | 929 | **329** |
| LevelEditor | 249 | 599 | **203** |

Original totals are the archived U4 measurements in `UITK-Report.md`; v1/v2 use the same ZVEC preview harness with a Level_01 editor draft. Editor totals depend on draft contents, so the original column is historical rather than an identical-fixture comparison.

The repeated 20-warm-up/50-cycle check had **zero growth**: Game elements 329→329, callbacks 336→336; editor elements 203→203, callbacks 679→679; gradient textures 8→8 with unchanged IDs. Managed heap did not grow (editor-wide GC snapshots).

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/v2/`. `ContactSheet-v2.png` covers the same nine resolutions and seven screens; `After/` contains 126 captures, including hover/pressed states. `Compare-Button-3x.png` and `Compare-Panel-3x.png` show Original PNG / v1 / v2 side by side. Visual inspection confirms the v1 inner-edge stair steps are removed. All **954 v1 control-bound records match**; sampled geometry checks found no protrusion in 1,656 highlights/glints. See `Verification.json`, `LeakCheck.txt`, `ButtonStates.txt` and `TextureCache-*.json`.

Unity 2022.3.19f1 compiled clean; final Console **0 entries**. Pinned instance `BusJamCase@c4dcbf17c52e6f1b`, Edit mode only. Protected files/backgrounds unchanged; temporary harness removed. No Play, vault access or push. Device rendering cost remains unprofiled.
