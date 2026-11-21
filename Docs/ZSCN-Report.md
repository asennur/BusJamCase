# ZSCN-1 — Imports and clip-driven celebration

Unity **2022.3.19f1**, pinned `BusJamCase@c4dcbf17c52e6f1b`, main branch. All verification used **Edit mode**. No Play, push, scene save, Blender source save, or AsenOS write.

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-1/`.

## Run 1 — Import and binding

Imported the approved FBX/PNG exports. Existing Yolcu and Otobus GUIDs, clip file IDs, explicit clips, and gameplay transitions remain intact. Added looping Cheer_Jump/Lean/Wave and Celebrate_Hold; Arrive/Leave remain one-shots. Special bus controllers follow the existing explicit Idle/Drive/Brake state pattern, with celebration states on OtobusKutlama. Cheer states have no gameplay transitions.

Bus import scale is 1.3, matching Otobus; Yolcu and scenery use 1. Generic rigs retain their transform hierarchies. Only Yolcu keeps CPU mesh readback, for the one-time sole alignment. Environment/Menu palette materials use Simple Lit; the water material uses a cheap scrolling texture shader. Palette imports are Point/Clamp/no mipmaps/uncompressed/sRGB; tiling imports use Repeat/mipmaps/Trilinear/sRGB. MenuSky stays 1080×1920, Clamp, without mipmaps. Dedicated instanced presentation materials preserve the protected gameplay colours.

Headless Blender extraction wrote EnvironmentKit/MenuKit `Onizleme_Yerlesim` transforms, mesh identities, and camera data to JSON. Conversion is `(-X, Z, -Y)`; 406 known Env_Tree_A vertices match the seam-split Unity import within **2.67073484e-7 units** after removing the prototype's display translation. Palette readback compared **65,536 texels per palette**, with **zero changed texels**. See `imports.txt`, `coordinate-validation.txt`, and `palette-validation.txt`.

## Run 2 — Seat orientation and silhouette

The first capture exposed the imported seat bones' rest orientations. Empty standing anchors now cancel those rest orientations while following the animated seat bones. Two pooled riders stand on Seat_0/Seat_1; front plays Cheer_Jump, rear plays Cheer_Lean. `Rear Rider Waves` selects Cheer_Wave, and `Rider Count` defaults to 2 and clamps to 1–2.

The compact mesh was smaller under the former bus framing. Presentation normalization preserves the previous **8.44-unit silhouette width** beneath the unchanged authored Bus Scale. The whole cast scales together. All ResultTimeline.asset bytes are unchanged, including its 0.65-second hold, camera, arc, timing, easing, bounce and scale values. Procedural rider arm rotations, hop, squash and thigh modifications are removed. Their legacy timeline controls remain serialized with obsolete tooltips.

Drive runs outside the stop-specific phase windows; Arrive covers the last 0.5 seconds of entry, Hold loops during the stop, Leave covers the first 0.5 seconds of exit. Imported clips own body, seat and wheel motion. The existing RenderTexture, unscaled timeline, pooling, colour selection and pointer-skip paths remain in use.

## Run 3 — Native evidence and verification

Native Game-view captures cover before, entry, stop, hold, leaving and exit at **580×1250**, **1080×1920**, and **2560×1080**. Raw readbacks remain unchanged. Only labelled comparison crops correct vertical readback. Three `timeline-reference-vs-ours-*.png` strips compare six phases against the reference. `_contact.png` and k_005–k_018 were inspected for the two-rider poses.

`Complete-sequence-crop.mp4` contains the full 3.6-second Complete presentation at 30 fps, from 108 native 1080×1920 frames. It uses an upright **1080×1700 crop**, removing 110 pixels from each vertical edge. Original full-size frames remain in `complete-frames/`.

`validation.txt` records **7,200 imported transform-binding comparisons**, including wheels and both rider variants; count clamping; all seven last-bus colours; pointer skip/release; disable and Fail cleanup; resize disposal; unchanged coins/timeline. **1,600 warmed direct sampler frames and 200 production UI sampler frames allocate 0 managed bytes.** After 20 warmups and 50 Complete/disable/enable cycles: transforms **85→85**, UI elements **232→232**, render targets **1→1**, stable shared material count, and the same RenderTexture identity.

Frame Debugger captured the same Complete hold UI with celebration hidden and visible: **21→40 draw calls, 1,566→17,247 triangles, 18→32 batches**. Events explicitly include CelebrationCamera and the existing UI RenderTexture composite; see `frame-debugger-before.json` and `frame-debugger-after.json`. This is the incremental celebration cost, not a benchmark against a restored legacy implementation. `stats.tsv` also records UnityStats for each native capture; Editor/UI repaint differences affect totals.

## Run 4 — Final asset audit

Removed internal `__preview__` clips accidentally exposed by Unity's asset enumeration and excluded them from setup. Final base-layer state counts are Yolcu **8**, Otobus **8**, OtobusKutlama **6**, OtobusMenu **3**, all defaulting to Idle. Added states have no transitions. The full pose/allocation/reuse validation passes again; see `animator-audit.txt` and `validation.txt`.

## Remaining differences and limits

- **Zero compiler/runtime errors; warning-free import is not achieved.** The supplied bus exports produce two distinct FBX warnings: 102/4,104 and 92/4,134 source vertices lack bone weights; Unity assigns them to bone 0. Reimports repeat these warnings. Source FBX bytes and read-only Blender sources were not rewritten to conceal or repair them. A transient MCP reconnect warning also occurred during domain reload.
- The approved mesh and imported clips approximate the reference rather than reproducing every pose or surface: seat backs/rail, wheel hubs, glass highlights, proportions and colour shading differ. The reference alternates rider gestures; this implementation keeps the requested front Jump/rear Lean pairing. The rear leans according to its authored clip; it does not reproduce every reference hand-to-rail contact.
- The retained 10° camera pitch, 0.8 Bus Scale and 0.65-second hold intentionally preserve Asen's timeline, so reference composition and gesture timing are not frame-identical. The reference comparison uses matching phases, not matching absolute video timestamps.
- The current board/background and UI art remain visible in comparisons. Environment/Menu imports and extracted layouts are ready; scenery placement, static batching flags on placed scenery, menu camera/layout and water placement belong to subsequent scene packages. No scenery was added to Game.unity in ZSCN-1.
- Allocation/growth checks are deterministic Edit-mode checks of production samplers. They are not device profiling or Play-mode gameplay verification.

Protected hashes are unchanged (`protected-before.json` / `protected-after.json`): ResultTimeline, Game.unity, M_Purple, M_Yellow, LevelDatabase and Level_26. Game.unity was never saved; Asen's on-disk directional light intensity remains **0.75**. Sprite atlases, ArtSource and the AsenOS vault were not modified.

# ZSCN-2 — Modular game background

21 November 2026. Unity **2022.3.19f1**, pinned `BusJamCase@c4dcbf17c52e6f1b`, main. **Edit mode only**, including native Game View captures and UnityStats; no Play, push, Blender source save or AsenOS write.

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-2/`. Start with `comparison-all-sizes.png`, the four `comparison-*.png` sheets, `budget.tsv`, `validation.txt` and `scene-preservation.txt`. Raw native captures are `before-*.png` and `after-final-*.png`; only derived comparison crops correct vertical readback.

## Run 1 — Baseline and headless extraction

The active image was `World Background/Road Sidewalk and Grass`, a horizontal quad at **(0, -0.30, 0.72)**, scale **(45, 45, 1.26)**, using M_GameBG / URP Unlit. The separate 30-section curb row spans X **-21.75 to +21.75** at Z **8.58**. Board Z **3.72**, waiting area Z **6.64**, DoorPoint Z **10.14**, the camera, gameplay bindings and UI are preserved.

Reopened EnvironmentKit.blend headlessly with Blender 5.1 and extracted **46 Onizleme_Yerlesim mesh instances**, mesh identities, matrices, positions, rotations, scales, bounds and the preview camera. No source file was saved. The exact FBX axis conversion is **(-X, Z, -Y)**: 406 Blender Env_Tree_A vertices match 2,237 seam-split imported vertices within **2.27079549e-7 units**, with matching bounds (`coordinate-validation.txt`).

The preview frame differs from Unity's authored camera. One parent adjustment maps it to the old quad: Y rotation **180 degrees**, uniform scale **45 / 17.0544 = 2.638615**, translation **(0, -0.3, 55)**. All 46 relative transforms remain unchanged. Pond is on the left, planter on the right, with the approved trees, bushes, flowers, rocks and tufts along the grass band.

## Run 2 — Geometry, materials and initial comparison

Added `Background/Environment` and a ten-triangle ground mesh containing separate floor, sidewalk, road, upper curb and grass bands. Boundaries map directly from GameBG source rows **516 / 406 / 292 / 278** to world Z **4.703254 / 8.650622 / 12.741531 / 13.243923**. Projected boundary differences against the original quad are **under 0.005 pixels** at all four capture sizes. Ground extends to X **+/-160**, Z **+/-140**; all four ground-frustum corners are inside it at every requested aspect. The existing lower curb remains and extends to X **+/-82.5** with the same sections.

`BusJam/Modular Ground` provides world-locked hashed light/dark purple tiles, rounded tile corners, grout and a small bevel highlight; lilac sidewalk; purple road; rounded white dashes; yellow road edges; white upper curb; green grass. Analytic derivatives antialias the pattern at native resolution. All bands share one instanced material and one draw. The warm global grading remains in place.

Added two main floor flower bushes, a small left companion bush, four rocks and ten tufts using source-image pixel anchors; the extracted collection contains only grass-band props. Added **24 seeded side variants** (`20261121`), distributed on both sides outside the central composition. `BackgroundScenery` uses the Inspector-bound layout event to enable them only in Landscape; portrait and square disable them. No scenery Update loop, per-frame spawning, collider or shadow geometry is added.

Static prop renderers are baked into four shared meshes: approved layout, floor details, wide left and wide right. Source transforms/meshes remain under their named objects, with their redundant renderers disabled. Water keeps its separate renderer and existing cheap EnvWater scrolling-UV shader. The curb extension is also combined. All rendering scenery is marked batching-static; all its shared materials enable instancing. A dedicated curb material copies the existing appearance, leaving the original asset untouched. EnvPalette remains Point/Clamp/no mipmaps/sRGB, EnvWater Repeat/Trilinear/mipmaps/sRGB; neither texture nor its previous palette validation changed.

The first captures exposed a dark floor and square dash ends; the final pass adjusted colours, rounded corners and dash phase. A repeat-build lookup initially failed because GameObject.Find excludes the already-disabled legacy quad; the rebuild now resolves it through its active parent. The provisional `after-*` captures lack the curb extension and are superseded by `after-final-*`. Validation also caught disabled instancing on the inherited curb material; the dedicated instanced copy fixes it.

## Run 3 — Final native captures and measured budget

All four sizes use the normal GameView final RenderTexture, with native dimensions verified before readback. UI is temporarily hidden to compare the background directly; the scene's gameplay objects are not spawned in Edit mode. These are **background-scene totals**, not a populated gameplay/device profile. UnityStats was read after 35 settled editor repaints per size. SetPass calls are **4 before / 5 after** throughout.

| Native size | Draw calls before → after | Delta | Triangles before → after | Batches before → after |
| --- | --- | --- | --- | --- |
| 580x1250 | 15 → 19 | +4 | 1,060 → 23,678 | 15 → 19 |
| 1080x1920 | 17 → 21 | +4 | 1,236 → 23,854 | 17 → 21 |
| 2560x1080 | 33 → 39 | +6 | 2,644 → 32,588 | 33 → 39 |
| 1080x1080 | 27 → 31 | +4 | 2,116 → 24,734 | 27 → 31 |

The requested measured **+10 draw-call / <50k triangle budget passes** at every size. New background meshes contain **29,946 triangles**, including inactive side extensions and the extended curb. The old quad is disabled; GameBG.png and M_GameBG remain on disk.

## Run 4 — Saved scene and protection audit

**110 Edit-mode checks pass after reloading the saved Game scene**: all extracted local transforms, inactive legacy quad, exact light intensity, material instancing, static flags, no scenery shadows/colliders, palette/water imports, landscape-only activation, frustum coverage, projected bands, triangle bound and missing-script audit. Final Console: **0 errors, 0 warnings, 0 logs**. Compilation briefly emitted MCP reconnect warnings; no FBX reimport or new source-model warning was introduced in this package.

All **124 original serialized scene blocks remain**. Camera, Light and original MonoBehaviour blocks compare identical to the initial on-disk snapshot. The 33 intentional existing-block changes are 30 curb prefab overrides (static flags/material), curb-root static flag, legacy-quad inactive flag and the scene root list. Viewport fitting caused a small temporary camera drift; its exact original on-disk pose was restored before saving. An automatically serialized existing UI timeline default was excluded from the final scene diff.

**35 protected file hashes are unchanged**, covering ArtSource files, sprite atlases, ResultTimeline.asset, M_Purple, M_Yellow, LevelDatabase, Level_26 and GameBG.png. Asen's directional-light intensity stays exactly **0.75 on disk**; its pre-existing 0.5 → 0.75 change is excluded from the staged scene and remains uncommitted. No protected asset is staged. No vault write or push.

## Remaining differences and limits

- The approved low-poly meshes have faceted foliage, different flower silhouettes and taller planter sides than the painted image. Their lit palette differs from the baked illustration; painted highlights, soft shadows, sidewalk flecks and water ripples are not reproduced pixel-for-pixel. Pond/planter silhouettes therefore differ even with their composition preserved.
- Tile colours use deterministic per-cell hashes and subtle bevel shading; they reproduce the tile style and dimensions, not each original tile's painted colour. Yellow lines and band boundaries align geometrically; minor painterly edge variation is replaced by clean lines.
- Only the grass band's seeded additions are aspect-gated. The two authored floor clusters remain world-locked and naturally crop out of narrow portrait views, as in the old quad.
- The budget above includes the scenery and existing curb in the saved Edit-mode scene. It does not establish a <50k-triangle limit for a runtime level populated with passengers/buses or for Complete celebration. Play and device profiling were excluded by the task.
