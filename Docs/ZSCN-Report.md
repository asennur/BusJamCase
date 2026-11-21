# ZSCN-1 â€” Imports and clip-driven celebration

Unity **2022.3.19f1**, pinned `BusJamCase@c4dcbf17c52e6f1b`, main branch. All verification used **Edit mode**. No Play, push, scene save, Blender source save, or AsenOS write.

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-1/`.

## Run 1 â€” Import and binding

Imported the approved FBX/PNG exports. Existing Yolcu and Otobus GUIDs, clip file IDs, explicit clips, and gameplay transitions remain intact. Added looping Cheer_Jump/Lean/Wave and Celebrate_Hold; Arrive/Leave remain one-shots. Special bus controllers follow the existing explicit Idle/Drive/Brake state pattern, with celebration states on OtobusKutlama. Cheer states have no gameplay transitions.

Bus import scale is 1.3, matching Otobus; Yolcu and scenery use 1. Generic rigs retain their transform hierarchies. Only Yolcu keeps CPU mesh readback, for the one-time sole alignment. Environment/Menu palette materials use Simple Lit; the water material uses a cheap scrolling texture shader. Palette imports are Point/Clamp/no mipmaps/uncompressed/sRGB; tiling imports use Repeat/mipmaps/Trilinear/sRGB. MenuSky stays 1080Ã—1920, Clamp, without mipmaps. Dedicated instanced presentation materials preserve the protected gameplay colours.

Headless Blender extraction wrote EnvironmentKit/MenuKit `Onizleme_Yerlesim` transforms, mesh identities, and camera data to JSON. Conversion is `(-X, Z, -Y)`; 406 known Env_Tree_A vertices match the seam-split Unity import within **2.67073484e-7 units** after removing the prototype's display translation. Palette readback compared **65,536 texels per palette**, with **zero changed texels**. See `imports.txt`, `coordinate-validation.txt`, and `palette-validation.txt`.

## Run 2 â€” Seat orientation and silhouette

The first capture exposed the imported seat bones' rest orientations. Empty standing anchors now cancel those rest orientations while following the animated seat bones. Two pooled riders stand on Seat_0/Seat_1; front plays Cheer_Jump, rear plays Cheer_Lean. `Rear Rider Waves` selects Cheer_Wave, and `Rider Count` defaults to 2 and clamps to 1â€“2.

The compact mesh was smaller under the former bus framing. Presentation normalization preserves the previous **8.44-unit silhouette width** beneath the unchanged authored Bus Scale. The whole cast scales together. All ResultTimeline.asset bytes are unchanged, including its 0.65-second hold, camera, arc, timing, easing, bounce and scale values. Procedural rider arm rotations, hop, squash and thigh modifications are removed. Their legacy timeline controls remain serialized with obsolete tooltips.

Drive runs outside the stop-specific phase windows; Arrive covers the last 0.5 seconds of entry, Hold loops during the stop, Leave covers the first 0.5 seconds of exit. Imported clips own body, seat and wheel motion. The existing RenderTexture, unscaled timeline, pooling, colour selection and pointer-skip paths remain in use.

## Run 3 â€” Native evidence and verification

Native Game-view captures cover before, entry, stop, hold, leaving and exit at **580Ã—1250**, **1080Ã—1920**, and **2560Ã—1080**. Raw readbacks remain unchanged. Only labelled comparison crops correct vertical readback. Three `timeline-reference-vs-ours-*.png` strips compare six phases against the reference. `_contact.png` and k_005â€“k_018 were inspected for the two-rider poses.

`Complete-sequence-crop.mp4` contains the full 3.6-second Complete presentation at 30 fps, from 108 native 1080Ã—1920 frames. It uses an upright **1080Ã—1700 crop**, removing 110 pixels from each vertical edge. Original full-size frames remain in `complete-frames/`.

`validation.txt` records **7,200 imported transform-binding comparisons**, including wheels and both rider variants; count clamping; all seven last-bus colours; pointer skip/release; disable and Fail cleanup; resize disposal; unchanged coins/timeline. **1,600 warmed direct sampler frames and 200 production UI sampler frames allocate 0 managed bytes.** After 20 warmups and 50 Complete/disable/enable cycles: transforms **85â†’85**, UI elements **232â†’232**, render targets **1â†’1**, stable shared material count, and the same RenderTexture identity.

Frame Debugger captured the same Complete hold UI with celebration hidden and visible: **21â†’40 draw calls, 1,566â†’17,247 triangles, 18â†’32 batches**. Events explicitly include CelebrationCamera and the existing UI RenderTexture composite; see `frame-debugger-before.json` and `frame-debugger-after.json`. This is the incremental celebration cost, not a benchmark against a restored legacy implementation. `stats.tsv` also records UnityStats for each native capture; Editor/UI repaint differences affect totals.

## Run 4 â€” Final asset audit

Removed internal `__preview__` clips accidentally exposed by Unity's asset enumeration and excluded them from setup. Final base-layer state counts are Yolcu **8**, Otobus **8**, OtobusKutlama **6**, OtobusMenu **3**, all defaulting to Idle. Added states have no transitions. The full pose/allocation/reuse validation passes again; see `animator-audit.txt` and `validation.txt`.

## Remaining differences and limits

- **Zero compiler/runtime errors; warning-free import is not achieved.** The supplied bus exports produce two distinct FBX warnings: 102/4,104 and 92/4,134 source vertices lack bone weights; Unity assigns them to bone 0. Reimports repeat these warnings. Source FBX bytes and read-only Blender sources were not rewritten to conceal or repair them. A transient MCP reconnect warning also occurred during domain reload.
- The approved mesh and imported clips approximate the reference rather than reproducing every pose or surface: seat backs/rail, wheel hubs, glass highlights, proportions and colour shading differ. The reference alternates rider gestures; this implementation keeps the requested front Jump/rear Lean pairing. The rear leans according to its authored clip; it does not reproduce every reference hand-to-rail contact.
- The retained 10Â° camera pitch, 0.8 Bus Scale and 0.65-second hold intentionally preserve Asen's timeline, so reference composition and gesture timing are not frame-identical. The reference comparison uses matching phases, not matching absolute video timestamps.
- The current board/background and UI art remain visible in comparisons. Environment/Menu imports and extracted layouts are ready; scenery placement, static batching flags on placed scenery, menu camera/layout and water placement belong to subsequent scene packages. No scenery was added to Game.unity in ZSCN-1.
- Allocation/growth checks are deterministic Edit-mode checks of production samplers. They are not device profiling or Play-mode gameplay verification.

Protected hashes are unchanged (`protected-before.json` / `protected-after.json`): ResultTimeline, Game.unity, M_Purple, M_Yellow, LevelDatabase and Level_26. Game.unity was never saved; Asen's on-disk directional light intensity remains **0.75**. Sprite atlases, ArtSource and the AsenOS vault were not modified.

# ZSCN-2 â€” Modular game background

21 November 2026. Unity **2022.3.19f1**, pinned `BusJamCase@c4dcbf17c52e6f1b`, main. **Edit mode only**, including native Game View captures and UnityStats; no Play, push, Blender source save or AsenOS write.

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-2/`. Start with `comparison-all-sizes.png`, the four `comparison-*.png` sheets, `budget.tsv`, `validation.txt` and `scene-preservation.txt`. Raw native captures are `before-*.png` and `after-final-*.png`; only derived comparison crops correct vertical readback.

## Run 1 â€” Baseline and headless extraction

The active image was `World Background/Road Sidewalk and Grass`, a horizontal quad at **(0, -0.30, 0.72)**, scale **(45, 45, 1.26)**, using M_GameBG / URP Unlit. The separate 30-section curb row spans X **-21.75 to +21.75** at Z **8.58**. Board Z **3.72**, waiting area Z **6.64**, DoorPoint Z **10.14**, the camera, gameplay bindings and UI are preserved.

Reopened EnvironmentKit.blend headlessly with Blender 5.1 and extracted **46 Onizleme_Yerlesim mesh instances**, mesh identities, matrices, positions, rotations, scales, bounds and the preview camera. No source file was saved. The exact FBX axis conversion is **(-X, Z, -Y)**: 406 Blender Env_Tree_A vertices match 2,237 seam-split imported vertices within **2.27079549e-7 units**, with matching bounds (`coordinate-validation.txt`).

The preview frame differs from Unity's authored camera. One parent adjustment maps it to the old quad: Y rotation **180 degrees**, uniform scale **45 / 17.0544 = 2.638615**, translation **(0, -0.3, 55)**. All 46 relative transforms remain unchanged. Pond is on the left, planter on the right, with the approved trees, bushes, flowers, rocks and tufts along the grass band.

## Run 2 â€” Geometry, materials and initial comparison

Added `Background/Environment` and a ten-triangle ground mesh containing separate floor, sidewalk, road, upper curb and grass bands. Boundaries map directly from GameBG source rows **516 / 406 / 292 / 278** to world Z **4.703254 / 8.650622 / 12.741531 / 13.243923**. Projected boundary differences against the original quad are **under 0.005 pixels** at all four capture sizes. Ground extends to X **+/-160**, Z **+/-140**; all four ground-frustum corners are inside it at every requested aspect. The existing lower curb remains and extends to X **+/-82.5** with the same sections.

`BusJam/Modular Ground` provides world-locked hashed light/dark purple tiles, rounded tile corners, grout and a small bevel highlight; lilac sidewalk; purple road; rounded white dashes; yellow road edges; white upper curb; green grass. Analytic derivatives antialias the pattern at native resolution. All bands share one instanced material and one draw. The warm global grading remains in place.

Added two main floor flower bushes, a small left companion bush, four rocks and ten tufts using source-image pixel anchors; the extracted collection contains only grass-band props. Added **24 seeded side variants** (`20261121`), distributed on both sides outside the central composition. `BackgroundScenery` uses the Inspector-bound layout event to enable them only in Landscape; portrait and square disable them. No scenery Update loop, per-frame spawning, collider or shadow geometry is added.

Static prop renderers are baked into four shared meshes: approved layout, floor details, wide left and wide right. Source transforms/meshes remain under their named objects, with their redundant renderers disabled. Water keeps its separate renderer and existing cheap EnvWater scrolling-UV shader. The curb extension is also combined. All rendering scenery is marked batching-static; all its shared materials enable instancing. A dedicated curb material copies the existing appearance, leaving the original asset untouched. EnvPalette remains Point/Clamp/no mipmaps/sRGB, EnvWater Repeat/Trilinear/mipmaps/sRGB; neither texture nor its previous palette validation changed.

The first captures exposed a dark floor and square dash ends; the final pass adjusted colours, rounded corners and dash phase. A repeat-build lookup initially failed because GameObject.Find excludes the already-disabled legacy quad; the rebuild now resolves it through its active parent. The provisional `after-*` captures lack the curb extension and are superseded by `after-final-*`. Validation also caught disabled instancing on the inherited curb material; the dedicated instanced copy fixes it.

## Run 3 â€” Final native captures and measured budget

All four sizes use the normal GameView final RenderTexture, with native dimensions verified before readback. UI is temporarily hidden to compare the background directly; the scene's gameplay objects are not spawned in Edit mode. These are **background-scene totals**, not a populated gameplay/device profile. UnityStats was read after 35 settled editor repaints per size. SetPass calls are **4 before / 5 after** throughout.

| Native size | Draw calls before â†’ after | Delta | Triangles before â†’ after | Batches before â†’ after |
| --- | --- | --- | --- | --- |
| 580x1250 | 15 â†’ 19 | +4 | 1,060 â†’ 23,678 | 15 â†’ 19 |
| 1080x1920 | 17 â†’ 21 | +4 | 1,236 â†’ 23,854 | 17 â†’ 21 |
| 2560x1080 | 33 â†’ 39 | +6 | 2,644 â†’ 32,588 | 33 â†’ 39 |
| 1080x1080 | 27 â†’ 31 | +4 | 2,116 â†’ 24,734 | 27 â†’ 31 |

The requested measured **+10 draw-call / <50k triangle budget passes** at every size. New background meshes contain **29,946 triangles**, including inactive side extensions and the extended curb. The old quad is disabled; GameBG.png and M_GameBG remain on disk.

## Run 4 â€” Saved scene and protection audit

**110 Edit-mode checks pass after reloading the saved Game scene**: all extracted local transforms, inactive legacy quad, exact light intensity, material instancing, static flags, no scenery shadows/colliders, palette/water imports, landscape-only activation, frustum coverage, projected bands, triangle bound and missing-script audit. Final Console: **0 errors, 0 warnings, 0 logs**. Compilation briefly emitted MCP reconnect warnings; no FBX reimport or new source-model warning was introduced in this package.

All **124 original serialized scene blocks remain**. Camera, Light and original MonoBehaviour blocks compare identical to the initial on-disk snapshot. The 33 intentional existing-block changes are 30 curb prefab overrides (static flags/material), curb-root static flag, legacy-quad inactive flag and the scene root list. Viewport fitting caused a small temporary camera drift; its exact original on-disk pose was restored before saving. An automatically serialized existing UI timeline default was excluded from the final scene diff.

**35 protected file hashes are unchanged**, covering ArtSource files, sprite atlases, ResultTimeline.asset, M_Purple, M_Yellow, LevelDatabase, Level_26 and GameBG.png. Asen's directional-light intensity stays exactly **0.75 on disk**; its pre-existing 0.5 â†’ 0.75 change is excluded from the staged scene and remains uncommitted. No protected asset is staged. No vault write or push.

## Remaining differences and limits

- The approved low-poly meshes have faceted foliage, different flower silhouettes and taller planter sides than the painted image. Their lit palette differs from the baked illustration; painted highlights, soft shadows, sidewalk flecks and water ripples are not reproduced pixel-for-pixel. Pond/planter silhouettes therefore differ even with their composition preserved.
- Tile colours use deterministic per-cell hashes and subtle bevel shading; they reproduce the tile style and dimensions, not each original tile's painted colour. Yellow lines and band boundaries align geometrically; minor painterly edge variation is replaced by clean lines.
- Only the grass band's seeded additions are aspect-gated. The two authored floor clusters remain world-locked and naturally crop out of narrow portrait views, as in the old quad.
- The budget above includes the scenery and existing curb in the saved Edit-mode scene. It does not establish a <50k-triangle limit for a runtime level populated with passengers/buses or for Complete celebration. Play and device profiling were excluded by the task.

# ZSCN-3 â€” 3D Start menu

21 November 2026. Unity **2022.3.19f1**, pinned `BusJamCase@c4dcbf17c52e6f1b`, main. **Edit mode only**. No Play, push, Blender source save or AsenOS write. The approved exports and import settings were already tracked by ZSCN-1; this package adds their scene placement and presentation.

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-3/`. Open `comparison-1080x1920.png`, the other three `comparison-<size>.png` sheets, and `comparison-M1.png` through `comparison-M3.png`. The final native readbacks are **`delivery-*.png`**, including UI, and **`delivery-world-*.png`**, with UI temporarily hidden. Raw readbacks are untouched; only derived comparison crops correct vertical readback. Other capture labels are provisional iterations, retained for traceability.

## Run 1 â€” Baseline and source extraction

Captured the existing Start screen at 580x1250, 1080x1920, 2560x1080 and 1080x1080 (`before-*`). Reopened MenuKit.blend headlessly with Blender 5.1 without saving it. `extract_menu.py` writes all **33** `Onizleme_Yerlesim` instances, mesh names, world matrices, Unity positions/quaternions/scales, bounds and camera data to `MenuKit-layout.json`. The bridge, preview bus/rig and ground are outside that collection; their source transforms are extracted separately.

Verified the exact FBX conversion **(-X, Z, -Y)** against Menu_Bridge: **1,194 source vertices** match **1,660 seam-split imported vertices**, with maximum nearest distance **0.000005275 units**. The source camera is vertical sensor fit, **52 degrees vertical**, pitch **41 degrees**, distance **32**. Its Unity target is **(0, 3.298896, -16.5)**, two metres along the road from the preview bus origin **(0, 3.298896, -14.5)**. The crest probe at **(0,4.2,-21)** projects to **39.30% from the top** in portrait.

## Run 2 â€” Menu world and camera routing

Added `MenuWorld` at **(1000,0,1000)**, retaining all extracted relative transforms. Menu_Bridge uses its three imported palette/asphalt/walk materials. The imported OtobusMenu is placed at the preview bus pose; a **1/1.3** model-parent scale cancels the established bus import scale to retain Blender dimensions. Its Animator defaults to looping Idle, including the imported Smoke_0/1/2 animation. `Menu Presentation` exposes the blue body colour **(0.035,0.59,0.86)** through a reusable material-property block on body slot 0; trim/glass/lights remain separate.

`MenuSky Background` is a camera-child quad **200 units** away, within a **250-unit** far plane, using URP Unlit and MenuSky.png. Cover-fit expands its geometry around the optical axis while retaining the image aspect; the entire camera frustum is covered at all four sizes. The old `menu-background` UXML element, its stylesheet image rule and obsolete ToolkitGameUI lookup were removed. **bg_menu.png remains on disk.** Counters, gear, level button and logo keep their existing UI hierarchy and layout.

`MenuWorldPresentation` subscribes to `GameManager.StateChanged` and the existing layout event. Start activates the menu root/camera; Ready, Playing, Complete and Fail deactivate them and restore gameplay rendering. Existing startup indicator/celebration code discovers `Camera.main`, so the gameplay camera stays enabled as an **unstacked URP Overlay** in Start; URP skips its rendering. Every other state restores its original **Base** type. Its transform, projection, culling mask, Inspector references and game-flow code are preserved. `rendered-cameras.txt` records the actual URP Game-camera callbacks for Start and Ready.

The first provisional captures (`after-1`) exposed an overextended floor hiding the skyline. `after-2` restores the distant floor edge and adds a **menu-only warm fill light**, intensity **0.85**, pitch **35**, yaw **180**, no shadows. Asen's original directional light stays exactly **0.75**. Existing global grading is retained.

## Run 3 â€” Aspect composition, batching and final captures

Scenery uses **two shared combined side meshes**; the named source transforms/meshes remain with redundant renderers disabled. Bridge, combined scenery and ground are marked batching-static, and shared materials enable GPU instancing. The bus, camera and backdrop remain dynamic. No scenery colliders or shadows were added. MenuPalette is Point/Clamp/no mipmaps/sRGB; MenuAsphalt/MenuWalk remain Repeat/Trilinear/mipmaps/sRGB. An sRGB GPU readback compared **65,536 MenuPalette texels**, with **zero changed texels** and zero maximum channel error.

`after-3`, `after-final` and `world-final` measure the combined geometry with the provisional wider/farther lenses. `aspect-refined` tightens square and landscape framing to keep the bridge approach off-screen. `final`/`final-world` and `verified`/`verified-world` expose and correct the floor's transparent queue and winding. **`delivery`/`delivery-world` supersede all of them.** The preview floor retains Y **-7**, extends sideways, and fades from Z **-20 to -32** into the skyline, removing the straight green horizon visible on wide screens. Its small shader uses the main light/ambient, vertex alpha and no textures or per-frame updates.

| Aspect band | Range | Vertical FOV | Target distance | Camera pitch |
| --- | --- | --- | --- | --- |
| Portrait | aspect < 0.8 | 52 degrees | 32 units | 41 degrees |
| Square | 0.8 through 1.25 | 54 degrees | 30 units | 41 degrees |
| Landscape | aspect > 1.25 | 58 degrees | 24 units | 41 degrees |

The final comparison sheets include the supplied portrait reference, baseline, new Start view, and unobstructed 3D world for each native size. The reference remains its supplied portrait image; separate landscape/square reference captures were not supplied. M1/M2/M3 sheets retain their original comparison images beside the Unity world crop.

UnityStats was sampled after **40 settled editor repaints** per capture, from the normal Game View final target at verified native dimensions, with UI included. These are complete **Edit-mode Start-view totals**. The baseline still renders gameplay scenery underneath its opaque UI background; it contains no old 3D menu. Small draw-call variation comes from editor/UI repaints. The two combined chunks submit their complete meshes, increasing triangles while reducing draw calls.

| Native size | Draw calls before â†’ after | Triangles before â†’ after | Batches before â†’ after | SetPass before â†’ after |
| --- | --- | --- | --- | --- |
| 580x1250 | 24 â†’ 21 | 23,812 â†’ 59,396 | 21 â†’ 19 | 6 â†’ 10 |
| 1080x1920 | 24 â†’ 19 | 23,808 â†’ 59,394 | 21 â†’ 19 | 6 â†’ 10 |
| 2560x1080 | 24 â†’ 19 | 23,810 â†’ 59,394 | 21 â†’ 19 | 6 â†’ 10 |
| 1080x1080 | 24 â†’ 19 | 23,808 â†’ 59,394 | 21 â†’ 19 | 6 â†’ 10 |

The final UI-hidden views measure **18 draw calls / 59,268 triangles / 18 batches** at all four sizes. `budget.tsv` and `stats.tsv` retain the full numbers. This package has no requested 50k-triangle cap; it does not claim the ZSCN-2 background's <50k total for this menu.

## Run 4 â€” Saved-scene verification and protection audit

**107 Edit-mode checks pass after reloading the saved scene**: unique root, every extracted instance transform, state-event routing, Camera.main identity, unchanged gameplay camera pose/projection, all aspect lenses, crest location, sky coverage/aspect, UI preservation, ground winding/queue, static flags/material instancing, texture sampling settings, default/looping Idle, actual Animator-driven exhaust and exposed bus colour. **2,000 warmed camera/sky fits allocate zero managed bytes.** Fifty menu returns retain the same transform count; ten disable/enable cycles keep event subscription counts stable. The presentation has no Update loop, per-frame spawning or per-frame material creation.

All **1,007 original serialized scene blocks remain**. Only two existing blocks change: the gameplay camera's URP render type in Start and the scene root list. The gameplay camera's exact original on-disk pose was restored before saving. An automatically serialized existing UI timeline default was excluded from the scene diff. Scene reload was guarded by `isDirty=false` so no unsaved editor work was discarded.

**35 protected file hashes are unchanged**: Blender sources, sprite atlases, ResultTimeline.asset, M_Purple, M_Yellow, LevelDatabase, Level_26 and bg_menu.png. Asen's directional light intensity remains exactly **0.75 on disk**; the existing **0.5 â†’ 0.75** change is excluded from the staged scene and remains uncommitted. Final Console: **0 errors, 0 warnings, 0 logs** after clearing a transient MCP WebSocket reconnect warning from compilation/domain reload. No source-model reimport was needed, so no new FBX import warning was introduced.

## Remaining differences and limits

- The approved Blender preview itself differs from menu_4.0: smaller/narrower bus, larger block buildings, a denser and higher distant tree line, faceted foliage and simplified roofs/windows. Exact source placement preserves those differences. The M1â€“M3 comparisons make them visible.
- Existing warm grading and the menu fill produce different lighting/colour from Blender's preview and the painted reference. The bus reads cyan-blue; roof/glass/tail details and puff shapes differ from the reference. The supplied MenuSky skyline is simpler and its cover-fit crop becomes much larger on square/wide views.
- The retained logo overlaps the crest and part of the bus in portrait/square, as its existing UI placement dictates. Landscape keeps the current logo-left/button-right layout. The reference's tabs, profile and No Ads controls are outside the existing UI scope.
- Wider aspects expose additional world area and the soft ground transition. They retain the approved instance layout rather than adding new buildings/trees. The square/landscape lens change occurs at the existing aspect-band boundaries.
- State events, URP camera callbacks and Animator evaluation were tested in Edit mode. Play-mode user interactions, device performance and long-running runtime profiling were excluded by the explicit no-Play instruction.


# ZLENS â€” Start-only lens distortion

21 November 2026. Unity **2022.3.19f1**, pinned `BusJamCase@c4dcbf17c52e6f1b`, main. Edit mode only; compilation completed. No Play, push or AsenOS write.

`MenuWorld/Menu Lens Volume` uses the dedicated **MenuPostProcessing layer (10)**, global weight **1**, priority **10**. Only MenuCamera includes that layer: volume mask **1025** (Default + MenuPostProcessing). The gameplay camera keeps mask **1**. The existing Start-state switch also deactivates the volume with MenuWorld outside Start. No runtime code or per-frame work was added.

Tune **Assets/_BusJam/Settings/MenuVolumeProfile.asset > Lens Distortion > Intensity / Scale** in the Inspector. The profile contains only Lens Distortion: **Intensity -0.4**, **Scale 1.1**, **Center (0.5, 0.5)**, X/Y multipliers **1**; all parameters override. Negative intensity bends the bridge rails and surrounding edges; scale crops the border. Existing menu framing, lighting and shared grading remain authored. This is a visual approximation of menu_4.0.png and menu_92.0.png, retaining the previously documented model/composition differences.

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/lens/`. Native GameView final-target readbacks at **580x1250, 1080x1920, 2560x1080** are `start-off-*`, `start-on-*`, `hud-off-*`, `hud-on-*`, and saved-scene reload checks `saved-start-*`. Raw PNGs retain the GPU readback orientation; `*-view.png` files are vertically corrected derivatives. `comparison-*.png` shows **reference / lens off / lens on**, without changing the supplied portrait reference aspect. The external `ZLensReview.cs` records the temporary capture fixture; it is not shipped in Assets.

- All three Start sizes were inspected: no black corners or stretched border strips. The four 12x12 corner patches contain zero near-black pixels (`pixel-checks.txt`). UI Toolkit renders after camera post-processing, keeping counters, logo and button undistorted.
- Explicit VolumeManager stack checks for Start, Ready, Playing, Complete and Fail give **menu intensity -0.4 only in Start** and **gameplay intensity 0 in every state** (`isolation.txt`). The actual Complete celebration camera was constructed and checked: **volume mask 0, post-processing disabled**, preserving its transparent RenderTexture.
- HUD lens-on/off captures match exactly below the top 9% at all sizes. The only changed pixels are in the existing animated pond water at the top left; straight road, sidewalk and floor remain unchanged. These are Edit-mode gameplay-background/HUD previews: level passengers and buses are not spawned, and the timer shows 0:00. Runtime interaction was excluded by the no-Play instruction.
- Saved-scene audit retains **all 1,249 original serialized blocks**. Only MenuWorld's child list and MenuCamera's volume mask change; three volume-object blocks are new. Original camera transforms, UI fields and the directional-light block remain identical to the initial disk snapshot. Light intensity stays exactly **0.75** in the working scene, with Asen's existing light diff excluded from the commit.
- **35 protected file hashes remain unchanged**, including Asen's uncommitted Global Volume Profile.asset (LensDistortion active: 0), ResultTimeline, ArtSource, sprite atlases, M_Purple, M_Yellow, LevelDatabase and Level_26. The new effect references only MenuVolumeProfile; the existing global profile is neither modified nor staged.

The capture fixture compiled successfully and the first Console check returned **0 errors, 0 warnings, 0 logs**. After the temporary fixture was removed, the final compilation request stalled and MCP stopped answering its editor ping. The editor log records MCP WebSocket disconnect warnings; final compilation completion and a fresh empty Console are pending. Windows automation recovery was rejected by automatic approval review ("Computer Use was not approved to use unity"); approval to inspect the window or user focus/recovery is required. Changes are staged; commit is pending that final verification. No Play or push.

# ZBGTONE - Gameplay background muting

21 November 2026. Unity **2022.3.19f1**, pinned `BusJamCase@c4dcbf17c52e6f1b`, main baseline `938eea6`. The initial live editor ping succeeded. **Edit mode only; never Play.** No AsenOS write or push.

Tune **Assets/_BusJam/Settings/BackgroundStyle.asset** in the Inspector: **Desaturation 0.30**, **Lift 0.20**, **Contrast Reduction 0.05**, **Haze sRGB (0.90, 0.85, 0.91)**. `Background > BackgroundStyleBinding` references this asset and applies its shader globals on enable; Inspector edits apply immediately. There is no Update loop, material instantiation, texture copy or per-frame managed allocation. The haze is converted to the project's working colour space. Zeroing all three sliders restores the original shader output.

`BackgroundTone.hlsl` desaturates toward Rec.709 luminance, reduces contrast toward 0.5 in working colour space, then blends toward the haze. Only the three gameplay-background shaders consume these globals. The small `Background Simple Lit` variant reuses URP 14's Simple Lit input, vertex, lighting and fragment functions, with the treatment after lighting; the stock depth passes are reused. `M_Environment` uses this variant for the EnvironmentKit trees, bushes, flowers, rocks, grass tufts, pond surround and planter. ModularGround applies the same treatment to the outer floor, sidewalk, road/markings and grass band. ScrollingWater applies it after the existing texture sample; **UV speed remains (0.015, 0.025)** and time-driven scrolling remains intact.

The sidewalk curb material is retained unchanged. Board tiles, board rim/frame, slots, tunnels, obstacles, passengers, buses, celebration, UI and the 3D menu retain their materials and shader paths. The environment material has **zero scene renderer users outside Background**. No geometry, transforms, camera framing, light or post-processing setting was changed. All background materials retain GPU instancing. Unity's SRP Batcher compatibility code is **0 (compatible)** for all three affected shaders; shader diagnostics are empty.

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/bgtone/`. Primary **before-<size>.png**, **after-<size>.png** and saved-scene **saved-after-<size>.png** are untouched native GameView final RenderTexture readbacks at **580x1250, 1080x1920, 2560x1080**. Their `-view.png` derivatives correct GPU readback orientation; `comparison-<size>.png` provides labelled side-by-side previews. No screenshot was reconstructed from a camera-only render.

The temporary external `ZBgToneReview.cs` fixture uses the existing `BoardView.Build` with **Level_05**, instantiates **three existing bus prefabs** at the authored queue positions, freezes their poses, and displays the normal Playing HUD with Level 5 / 1:15. This is a populated **Edit-mode presentation**, not a running game or gameplay-flow test. No PlayerPrefs are written. Temporary objects are DontSave and removed; the fixture is not shipped in Assets.

Measurements use fixed top-left-origin native-pixel rectangles: foliage, grass, road, sidewalk and outer floor for background; bus surfaces and passenger heads for gameplay. **Mean linear sRGB luminance** uses Rec.709 weights; **mean HSV saturation** uses encoded sRGB. Aggregate means are weighted by sampled pixel count, not whole-image semantic segmentation. Animated water and object boundaries are excluded. `regions.json`, annotated `regions-<size>.png`, `measure.py` and per-region `measurements.tsv` make the selection and calculation reviewable.

| Native size | Region | Mean linear luminance before / after | Mean HSV saturation before / after |
| --- | --- | --- | --- |
| 580x1250 | Background | 0.2815 / 0.3276 | 0.3098 / 0.1765 |
| 580x1250 | Gameplay | 0.2139 / 0.2139 | 0.6547 / 0.6547 |
| 1080x1920 | Background | 0.2990 / 0.3417 | 0.2979 / 0.1694 |
| 1080x1920 | Gameplay | 0.2208 / 0.2208 | 0.6752 / 0.6752 |
| 2560x1080 | Background | 0.2645 / 0.3101 | 0.4148 / 0.2321 |
| 2560x1080 | Gameplay | 0.2183 / 0.2183 | 0.6575 / 0.6575 |

Background sample saturation falls **43-44%**. Sampled passenger pixels are identical at 1080x1920 and 2560x1080; final post-processed bus/passenger samples differ by at most **1/255 at 580x1250**, **0 at 1080x1920**, **2/255 at 2560x1080**. These tiny final-output differences do not imply gameplay material changes. Bright grass luminance can decrease slightly toward the warm/lilac haze; the aggregate background and dark floor become lighter.

| Native size | Draw calls before / after | Triangles before / after | Batches before / after | SetPass before / after |
| --- | --- | --- | --- | --- |
| 580x1250 | 126 / 126 | 162904 / 162904 | 126 / 126 | 11 / 14 |
| 1080x1920 | 128 / 128 | 163080 / 163080 | 128 / 128 | 11 / 14 |
| 2560x1080 | 157 / 157 | 188378 / 188378 | 157 / 157 | 9 / 10 |

**Zero added draw calls or triangles.** The separate background shader increases shader-state switches (SetPass) while preserving batching and existing render submissions. These are populated Edit-mode GameView totals, not device performance measurements. The muting adds a few scalar/vector arithmetic operations per background pixel, no texture fetches or additional rendered pass.

The saved-scene audit retains every original serialized block. Only Background's component list changes, plus one new binding block; an automatically serialized unrelated UI timeline reference was removed from the diff. Asen's light block remains identical to the initial working snapshot (**0.75**). Its existing 0.5-to-0.75 hunk is excluded from the commit. All **35 protected file hashes** match the initial snapshot, including Global Volume Profile.asset, ResultTimeline, ArtSource, sprite atlases, M_Purple, M_Yellow, LevelDatabase and Level_26. No protected asset is staged.

Final verification after removing the temporary fixture: compilation finished, editor not compiling/updating, Edit mode, scene clean, exactly one style binding, no review objects/type, style globals restored to defaults, light 0.75. **Console: 0 errors, 0 warnings, 0 logs.** The live MCP editor ping succeeded after final compilation. All 1,252 original scene blocks are retained; saved-scene native captures also pass the populated-view check. See final-editor.json, final-console.json, shader-audit.json and saved-audit.json in the evidence folder.

# ZBGTONE-2 - Darker, calmer background

21 November 2026. Supersedes ZBGTONE's pale haze direction above. Baseline: `fd1009a`, main, Unity **2022.3.19f1**, pinned `BusJamCase@c4dcbf17c52e6f1b`. The initial live editor ping succeeded. **Edit mode only; never Play.** No vault writes or push.

The Inspector location remains **Assets/_BusJam/Settings/BackgroundStyle.asset**, referenced by **Background > BackgroundStyleBinding**. Defaults: **Darken 0.28**, **Desaturation 0.25**, **Contrast Reduction 0.05**. English tooltips describe each control. Lift and Haze are removed. Desaturation mixes toward Rec.709 luminance; contrast reduction gently compresses highlights by dividing by `1 + luminance * contrastReduction`, preserving black without additive lift. Finally, Darken multiplies working-space RGB by **0.72**. This is a shader brightness multiplier, not a promise of a 28% reduction in final post-processed screenshot luminance. Zeroing all controls restores the unmuted shader output.

The same three background shaders remain the only consumers. No scene, gameplay material, geometry, UI, menu, camera, light, volume, texture or render-pass changes. No Update loop or new allocation. The purple floor stays visibly purple; the board and passengers now stand out against a deeper floor. The supplied t0030/t0060 frames guide this relationship; their detailed artwork, lighting and layout remain different.

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/bgtone2/`. Fresh **before**, **after** and **unmuted** native GameView final-target readbacks cover **580x1250, 1080x1920, 2560x1080**. Raw PNGs are untouched; `-view.png` derivatives correct GPU readback orientation. `comparison-native-<size>.png` presents native-sized columns: **current ZBGTONE / corrected / t0030 reference crop / t0060 reference crop**. Smaller `comparison-reference-*` sheets aid inspection. Reference images are centre cover-cropped with their aspect preserved; the landscape reference is a crop of the supplied portrait frame, not an invented landscape view. Reference files were read only.

The external `ZBgToneReview.cs` records the temporary populated Edit-mode fixture, removed from Assets after capture. It builds **Level_05** through BoardView.Build, freezes three existing bus prefabs at authored queue positions and displays **Level 5 / 1:15** in the normal HUD. No PlayerPrefs writes or runtime gameplay test. The fixture is cleaned up and the Start presentation restored.

Measurements reuse the previous fixed native-pixel regions, excluding animated water and object edges. Luminance is mean linear-sRGB Rec.709; saturation is mean encoded-sRGB HSV. Pixel-weighted aggregates describe these samples, not whole-image segmentation. See `regions.json`, annotated region images, `measure.py`, `compare.py`, `measurements.tsv` and `calibration.md`.

| Size | Region | Luminance ZBGTONE / corrected | Saturation ZBGTONE / corrected |
| --- | --- | --- | --- |
| 580x1250 | Background | 0.3276 / 0.1342 | 0.1765 / 0.2873 |
| 580x1250 | Gameplay | 0.2139 / 0.2139 | 0.6546 / 0.6546 |
| 1080x1920 | Background | 0.3417 / 0.1435 | 0.1694 / 0.2765 |
| 1080x1920 | Gameplay | 0.2208 / 0.2208 | 0.6752 / 0.6752 |
| 2560x1080 | Background | 0.3101 / 0.1252 | 0.2321 / 0.3890 |
| 2560x1080 | Gameplay | 0.2183 / 0.2183 | 0.6575 / 0.6575 |

Sampled background luminance falls **58-60% relative to the pale ZBGTONE result**, and **52-53% relative to the unmuted output**. Saturation is higher than the pale haze but **6-7% lower than unmuted output** (0.3098 to 0.2873, 0.2979 to 0.2765, 0.4148 to 0.3890). This preserves colour identity while removing the light wash. The 1080x1920 floor sample falls from **0.2698 to 0.0960** luminance. Sampled gameplay final-output channel deltas are at most **1/255, 0/255, 2/255** respectively; passenger samples are identical. Existing final post-processing can cause tiny differences without gameplay material changes.

| Size | Draw calls before / after | Triangles before / after | Batches before / after | SetPass before / after |
| --- | --- | --- | --- | --- |
| 580x1250 | 126 / 126 | 162904 / 162904 | 126 / 126 | 14 / 14 |
| 1080x1920 | 128 / 128 | 163080 / 163080 | 128 / 128 | 14 / 14 |
| 2560x1080 | 157 / 157 | 188378 / 188378 | 157 / 157 | 10 / 10 |

All three shader diagnostic lists are empty. Draw calls, triangles, batches and SetPass are unchanged from ZBGTONE at every size; these are populated Edit-mode GameView totals, not device profiling. **37 protected file hashes match**, including the entire Game.unity working file, Asen's light 0.75 and existing Global Volume Profile changes, MenuVolumeProfile, ResultTimeline, materials, level data, sprite atlases and ArtSource. Game.unity is neither saved nor staged by this package.

Final post-cleanup live audit: compilation finished, editor not updating, Edit mode, scene clean, one style binding, defaults applied, no temporary fixture objects or compiled fixture type, light **0.75**. **Console: 0 errors, 0 warnings, 0 logs.** See `final-audit.json`, `final-editor.json` and `final-console.json`. Only BackgroundStyle.cs, BackgroundTone.hlsl, BackgroundStyle.asset and this report belong to the commit.

# ZLOGO-OFF - Start logo removal

21 November 2026. Initial live response confirmed Unity **2022.3.19f1**, pinned `BusJamCase@c4dcbf17c52e6f1b`, main. **Edit mode only; never Play.** Removed the `logo` VisualElement and all base/square/landscape `.logo` rules. Logo SVG/PNG files remain on disk. No controller queries `logo`; no controller change was needed. The Start tree and all four captures contain no BUS JAM text/logo.

Native GameView final-target captures: **580x1250, 1080x1920, 2560x1080, 1080x1080**, under `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/logo-off/`. `before-*` / `after-*` PNGs retain raw readback orientation; `*-view.png` derivatives only flip vertically for inspection. Counter and Settings bounds are identical. Level button bounds change by **(0,+7), (0,-3), (-1,0), (0,0) pixels**, respectively; sizes are unchanged. Independent button positioning removes the logo's layout dependency without a spacer or reserved logo gap, keeping the 3D menu unobstructed.

Temporary capture fixture removed after verification. **37 protected file hashes match**, including the entire Game.unity working file/light 0.75, Global Volume Profile, MenuVolumeProfile, ResultTimeline, protected materials/levels, sprite atlases and ArtSource. No scene save, AsenOS write or push. Final compilation and live Edit-mode audit passed; **Console: 0 errors, 0 warnings, 0 logs**.
