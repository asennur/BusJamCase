# ZCELEB-2 — Complete celebration framing and motion

21 November 2026. Branch `main`, Unity **2022.3.19f1**, MCP pinned to **BusJamCase@c4dcbf17c52e6f1b**. This replaces the framing and passenger presentation from `965a72c` / `057237f`.

All rendering and verification used **Edit mode**. No Play, scene save, build or push. No AsenOS vault files were accessed in this revision; the existing extracted reference frames were reused. Protected files, including Asen's uncommitted `Game.unity`, remain byte-identical to the initial snapshot.

Implementation: **`c97d47f` — Fix celebration framing and standing passenger motion**.

## Presentation

- **Low front-side perspective:** bus yaw 135 degrees, camera FOV 35 and pitch -5 degrees. The upward camera pitch compensates for the cast's lower-screen position, keeping the effective viewpoint near roof height instead of looking down into the seats. Portrait scale follows screen width; wider viewports cap scale by screen height.
- **Readable standing riders:** three pooled passengers, 1.65 times the previous celebration scale, turned toward the viewer. Existing **Idle** supplies the standing body; the existing **Wave** supplies the raised right arm. No Sit sampling, new clips, models or material assets. Procedural arm lift/extension and a 0.85 head scale make the raised arm visible above the existing model's large head. The front rider leans 38 degrees over the railing; the others tilt with their staggered hops and bend one knee.
- **Hops:** 0.43-second cycles, phase-offset by 0.24 cycle per rider, peaking at half the cached standing height after cast scaling. Landings squash vertically by up to 14%, with compensating width/depth stretch. Hops follow the bus's up axis and retain their height ratio across resolutions.
- **Suspension:** a small 0.06-unit bob before cast scaling, plus a 3.5-degree roll. Existing Drive and ExhaustLoop continue to animate the vehicle.
- **Timing:** the 1.05-second right-to-left pass now starts with the overlay at **1.05 s after Complete**, reaches the central framing quickly, drifts through the readable portion, then accelerates fully off the left edge. It ends at **2.10 s after Complete**, overlapping the unchanged **1.75–2.05 s** title reveal. This puts the bus centrally in the reference's +0.3/+0.6 s frames and farther left at +0.9 s.

The last colour still comes from the completed level's final `busOrder` entry, and all three passengers use its existing shared material. The existing RenderTexture/UITK compositing path, reusable result clock, pooled objects, skip-on-tap and transition cleanup remain in use. Bone references and standing heights are cached at initialization; the active sampler creates no objects or collections.

## Framing evidence

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/celeb2/`.

- `timeline-reference-vs-ours.png`: reference versus the final production sampler at **0, 0.3, 0.6, 0.9 and 1.2 seconds after overlay onset**, in 1080×1920 cells. The original reference aspect is preserved and letterboxed, exactly as in the previous comparison. `timeline-preview.jpg` is the smaller review copy. The reference frames remain those extracted from 83.05/83.35/83.65/83.95/84.25 seconds of the supplied recording.
- `mid-nine-resolutions.jpg` / `ours/mid-*`: final mid-pass captures at **1080×1920, 1080×2400, 1170×2532, 1536×2048, 1080×1080, 1768×2208, 2208×1768, 1920×1080 and 2560×1080**. Settled Complete captures and a second contact sheet are also included.
- `ours/bus-mask-*` and `framing-metrics.json`: transparent bus-only renders at the same nine mid-pass samples. The alpha bounds measure bus size and wheel-bottom position independently of the passengers and background. Width at 1080×1920 is **63.9%**; landscape bus height is **31.4%**; wheel bottoms are at **84–85%** of screen height. These are measured bounding boxes, including seats/wheels and existing exhaust, rather than estimates from the UI screenshot.
- `complete-edit-mode.mp4`: a new full **1080×1920**, 30 fps, 3.53-second Complete intro, encoded from **106** final Unity-rendered frames. Originals remain in `frames/`.

The supplied assets have different body/window/seat proportions from the reference. The comparison matches the requested size, lower-screen placement, front-side perspective and celebration motion within those existing assets; it does not claim identical artwork.

## Verification

`CelebrationReview` drives the production sampler inside the existing isolated preview-scene/UI fixture. It reads the source world's camera for the background and never saves the Game scene.

- `validation.txt`: **20 Complete runs**, followed by **50 Complete/disable/enable cycles**. Pool transforms **119→119**, UI elements **351→351**, active DOTween handles **5→5**, owned render targets **1→1**, and shared material counts unchanged. The same RenderTexture survives repeated runs.
- **0 managed bytes over 200 warmed active sampler frames.** `lifecycle.txt` separately checks **0 bytes over 200 warmed Prepare/Sample/Hide runs**, and confirms destruction of the target and pool.
- All seven last-bus colours and three matching passenger materials pass. Actual pointer skip/release, mid-pass disable, Fail cleanup and old-target destruction on resolution changes pass. Saved coins remain unchanged.
- `endpoint-start.png` / `endpoint-exit.png`: transparent target checks at the active pass's first and last sampled instants; **zero visible pixels** verify that the cast starts and finishes completely beyond the viewport.
- `evidence-verification.json`: dimensions/nonblank checks for 18 resolution captures, 106 video frames, and **17 protected files hash-identical**. `final-unity.json` records compilation, Console and final Edit-mode state. One MCP WebSocket reconnect warning may be retained in the Console; no project compilation or presentation errors were found.

These are deterministic Edit-mode presentation and lifetime checks. No Play-mode gameplay or device GPU timing is claimed.

## Rendering cost

The presentation retains one screen-sized ARGB32 RenderTexture with **2× MSAA / 16-bit depth**, a transparent URP camera without post processing or shadows, one warm key and one UITK Image. The visible cast still submits the bus's 11 existing submeshes plus three passenger meshes: **14 mesh draws**. Outside the pass its camera is disabled. The prior measured retained target footprint at 1080×1920 was approximately **31.6 MiB**; this revision does not change the target format, resolution policy or draw count. Sampling standing Idle adds three existing-clip samples per active frame; no device GPU milliseconds are asserted.
