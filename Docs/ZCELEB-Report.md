# ZCELEB-3 — Final celebration tuning

21 November 2026. Implementation: **`10440bd`**, following `c97d47f` / `ad4ad1e`. Unity **2022.3.19f1**, MCP pinned to **BusJamCase@c4dcbf17c52e6f1b** throughout.

**Edit mode only; never Play.** No scene save, build or push. No AsenOS vault access; the previously extracted reference frames were reused. All **17 protected files**, including Asen's uncommitted `Game.unity`, remain SHA-256 identical to this session's initial snapshot. Existing unrelated changes remain unstaged.

## Changes

- Each rider now has a cached supporting sole from the existing foot's skinned vertices. After posing, its height is anchored to the existing bus deck at bus-local Y **2.04**, inside the rail and forward of the seat back. Lean and landing squash retain foot contact instead of raising the rider from the seat anchor. Mesh reads occur once during initialization.
- Hops use a **0.60 s cycle**, with one-third-cycle spacing. Air time is **36%**, grounded time **64%**. The dense 600-sample check measured **64.17% grounded** per rider and at most **two airborne**. Portrait hop peaks are **0.40 times cached standing height**; landscape peaks are **0.24** to keep heads below the title. A separate sole-to-head mesh measurement puts the portrait peak at **0.438 times actual standing body height**, below the 0.50 limit.
- Raised waving arms remain; the two upright riders lean forward slightly, and the front rider leans **38 degrees toward the camera** over the railing. Existing Idle/Wave, Drive and ExhaustLoop assets remain in use.
- Framing now scales by screen height. Camera pitch is **-7 degrees**, FOV remains **35**. Bus yaw is **115 degrees**, easing to **105** on narrow portrait viewports so the larger bus fits. The pass sits lower; landscape is slightly lower again for title clearance. Title layout and assets are unchanged.
- The existing **1.05 s right-to-left pass** remains at **1.05–2.10 s after Complete**, overlapping the unchanged **1.75–2.05 s** title reveal. Pooling, last-bus colour, shared materials, skip and transition cleanup remain unchanged.

## Measured framing

Evidence folder: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/celeb3/`.

The table uses the mid-pass sample at **1.725 s after Complete**. Height is the projected **roof rail top to wheel bottom**, measured from the animated mesh vertices; seats and exhaust are excluded. This corrects the earlier report's bus-alpha measurement, which included seat backs. Wheel clearance is measured upward from the bottom edge.

| Resolution | Bus height | Wheel clearance |
| --- | ---: | ---: |
| 1080×1920 | 33.45% | 3.27% |
| 1080×2400 | 33.51% | 3.49% |
| 1170×2532 | 33.52% | 3.46% |
| 1536×2048 | 33.47% | 3.27% |
| 1080×1080 | 33.47% | 3.27% |
| 1768×2208 | 33.46% | 3.27% |
| 2208×1768 | 38.90% | 2.09% |
| 1920×1080 | 38.90% | 2.10% |
| 2560×1080 | 38.90% | 2.09% |

`framing-metrics.json` contains the unrounded values and transparent bus-mask bounds. `mid-nine-resolutions.jpg` shows all nine captures. `overlap-nine-resolutions.jpg` adds the same nine at **1.95 s**, when bus and title are both visible. All **27 head/title checks pass**, with a minimum **15.63 px** gap between head mesh tops and the conservative full heading rectangle. The title was not moved.

## Timeline and foot heights

`timeline-reference-vs-ours.png` compares **0, 0.3, 0.6, 0.9 and 1.2 s after overlay onset**, in 1080×1920 cells. `timeline-preview.jpg` is the smaller review copy. Original reference aspect and letterboxing are preserved; the source reference frames remain 83.05/83.35/83.65/83.95/84.25 s.

Foot lifts below are measured along the bus's up axis above the deck, in world units; parentheses give the fraction of cached standing height H. Riders are **R0 rear, R1 middle, R2 front/leaning**. Grounded residuals below 0.0004 units are floating-point precision at the isolated stage's distant origin.

| Offset | Bus height | R0 foot lift | R1 foot lift | R2 foot lift |
| --- | ---: | ---: | ---: | ---: |
| 0.0 s | Offscreen | — | — | — |
| 0.3 s | 34.36% | 0.000 (0H) | 0.000 (0H) | 1.105 (0.397H) |
| 0.6 s | 33.35% | 0.000 (0H) | 0.256 (0.092H) | 0.000 (0H) |
| 0.9 s | 34.51% | 0.000 (0H) | 0.000 (0H) | 1.106 (0.397H) |
| 1.2 s | Pass finished | — | — | — |

`pose-measurements.tsv` also records each supporting sole's screen Y, deck Y and head top in pixels. At +0.3 s the sole screen Ys are **1315.61 / 1306.89 / 1081.81 px**; at +0.6 s **1299.15 / 1260.82 / 1312.80 px**; at +0.9 s **1320.83 / 1311.95 / 1086.86 px** (origin at screen top).

`complete-edit-mode.mp4` is a new **1080×1920, 30 fps, 3.53 s** Complete intro encoded from **106 final Unity-rendered frames**. Originals remain in `frames/`; `video-metadata.json` records the encoded stream. The supplied models differ from the reference artwork; no new model, animation or material assets were introduced.

## Verification

- `validation.txt`: **20 Complete runs**, then **50 Complete/disable/enable cycles**. Pool transforms **119→119**, UI elements **351→351**, DOTween handles **5→5**, owned render targets **1→1**, loaded shared materials **209→209**, same target identity. **Zero retained growth**.
- **0 managed bytes over 200 warmed active production-sampler frames**. `lifecycle.txt` separately records **0 bytes over 200 warmed Prepare/Sample/Hide runs** and the standing-mesh height measurement. Final cleanup leaves **zero celebration stages**.
- All seven last-bus colours and all three matching rider materials pass. Pointer skip/release, mid-pass disable, Fail cleanup, target destruction on resize and unchanged saved coins pass.
- `motion-validation.txt`: full cycle sampled every millisecond; **64.17% grounded**, peak **0.4001H** including numerical tolerance, maximum **two airborne**.
- `endpoint-start.png` / `endpoint-exit.png`: **zero visible pixels** at both active-pass endpoints.
- `evidence-verification.json`: **27 resolution captures**, **106 video frames**, **27 title/head checks**, and **17 protected files hash-identical**. Before/after hashes are retained separately.
- Compilation succeeded. Final **Console: 0 entries**, no project errors or warnings. An MCP WebSocket reconnect warning was recorded in `console-before-clear.json`, then cleared before the final zero-entry check. `final-unity.json` records the final compiled Edit-mode state and unchanged scene state.

These are deterministic Edit-mode presentation/lifetime checks, not Play-mode gameplay or device GPU measurements. The existing one-target ARGB32 / 2× MSAA / 16-bit-depth compositor and **14 mesh draws** remain unchanged; the larger framing does not add render targets or draws.
