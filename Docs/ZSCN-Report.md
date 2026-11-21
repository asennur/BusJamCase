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
