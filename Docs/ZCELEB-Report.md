# ZCELEB — Complete celebration bus

21 November 2026. `main`, Unity **2022.3.19f1**, MCP pinned to `BusJamCase@c4dcbf17c52e6f1b`. All verification and rendering ran in **Edit mode**. No Play, scene save, build or push. AsenOS access was limited to the supplied read-only `Referans` folder.

Implementation commit: `965a72c` — Add pooled 3D Complete celebration.

## Presentation and integration

The Complete screen now shows one 3D bus driving right to left, with **three passengers**, one on each existing seat bone. `CurrentLevel.busOrder` supplies the **last** bus colour; the same existing shared material colours all three passengers. This reads the completed level directly, even though GameManager has already advanced its saved next-level index.

`ToolkitGameUI.Results.cs` samples `CompleteCelebration` from the existing unscaled ZWIN DOTween driver. The existing **Drive**, **ExhaustLoop** and **Wave** clips are sampled directly; no clips, models or material instances were added. Each rider has a staggered 0.43-second procedural hop and a 10% landing squash. Hop displacement is converted into the seat bone's coordinates so it remains vertical. Riders turn slightly towards the viewer so their existing Wave is visible.

| Event | Time after Complete | Time after overlay starts |
| --- | --- | --- |
| Dark purple overlay fades in | 1.05–1.25 s | 0–0.20 s |
| Celebration pass | **1.20–2.25 s** | **0.15–1.20 s** |
| Existing title/ribbon scale-in | 1.75–2.05 s | 0.70–1.00 s |

The 1.05-second pass overlaps the title reveal. At either endpoint the whole cast is beyond the viewport. Intro skip uses the existing pointer capture/release path and immediately hides the bus; Continue, leaving Complete, Fail and disabling the document also stop its presentation. Reward and level progression logic are unchanged.

## Rendering and lifetime

- `CelebrationSettings.asset` contains Inspector-assigned prefab, clip and material references. A single Resources load at UI initialization avoids modifying the protected Game scene.
- Initialization clones the existing Bus and Passenger prefabs once beneath an inactive pool. The presentation removes their gameplay scripts before activation, preventing gameplay Awake methods, events and feedback tweens from running. Colliders, outlines, hidden marks, ropes and cloaks are disabled.
- `CelebrationCamera` culls only the new **Celebration layer (9)**. It uses transparent clear, FOV 35, a 14-degree side view and a warm directional key. The runtime world camera excludes this layer; its original mask is restored on destruction. The key and celebration camera are enabled only during the pass.
- The full-screen UITK Image is the first child of Complete: above the purple background, below the title/ribbon, coin HUD, rewards and controls. URP post processing is disabled on this camera because its opaque output would destroy transparent compositing; existing materials and the warm key preserve the art's lighting. The underlying game retains its existing post processing.
- A screen-sized ARGB32 target uses **2× MSAA and 16-bit depth**. The same target survives repeated celebrations and disable/enable cycles; only resolution changes replace it. Replacement and destruction release and destroy it. The cast remains pooled and inactive between passes.
- Screen width controls both path endpoints. Bus size is capped by screen height in landscape, keeping the complete pass in the lower band. No per-play Instantiate, new VisualElement, material instance or celebration-owned tween handle is needed. The hop shares ZWIN's existing reusable clock; disable rewinds/pauses that clock, and destruction kills the existing drivers.

## Verification and evidence

Evidence directory: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/celeb/`.

- `timeline-reference-vs-ours.png`: reference versus ours at **0, 0.3, 0.6, 0.9, 1.2 s after overlay onset**, with 1080×1920 cells. Reference frames were extracted independently with ffmpeg at 83.05, 83.35, 83.65, 83.95 and 84.25 seconds; their original aspect is preserved and letterboxed. `timeline-preview.jpg` is a smaller review copy.
- `ours/mid-*` and `ours/end-*`: **18 captures**, mid-pass and settled Complete, at **1080×1920, 1080×2400, 1170×2532, 1536×2048, 1080×1080, 1768×2208, 2208×1768, 1920×1080 and 2560×1080**. Both nine-resolution contact sheets are included.
- `complete-edit-mode.mp4`: the full 3.5-second intro, 30 fps, encoded with ffmpeg from 106 Unity-rendered frames. Original frames remain in `frames/`.
- `validation.txt`: **20 Complete runs**, then **50 Complete/disable/enable cycles**. Pool transforms **119→119**, UI elements **351→351**, existing active DOTween handles **5→5**, celebration targets **1→1**, loaded shared materials **191→191**. Zero retained growth; the target's instance ID stays unchanged. All seven last-bus colours, three matching rider materials, actual pointer skip/release, mid-pass disable, Fail cleanup and target replacement checks passed. Saved coins were unchanged.
- **0 managed bytes** over 200 warmed active production sampler frames. `lifecycle.txt` separately records **0 managed bytes over 200 Prepare/Sample/Hide runs**, and confirms destruction of both target and pool.
- `evidence-verification.json`: image dimensions/nonblank content, frame count and **17 protected files hash-identical**. Protected changes remain Asen's original uncommitted work. `final-unity.json` records the final compilation, Console and Edit-mode state.

`CelebrationReview` reuses ZWIN's isolated preview-scene/PanelSettings fixture and drives the production sampler. The source Game scene is only read by the existing world-camera capture adapter. These are Edit-mode presentation/lifetime checks, not a Play-mode gameplay or device GPU test.

## GPU cost

The visible celebration submits **14 mesh draws**: the bus's 11 existing submeshes, including exhaust, plus one per rider. It adds one UITK textured image and an MSAA resolve. There are no celebration shadow, depth-copy, opaque-copy or post-processing passes; the current renderer has no renderer features. Outside the pass its camera submits **zero** draws.

Unity reports **33,178,112 bytes (~31.6 MiB)** for its retained target at 1080×1920. A measured 4× MSAA/24-bit prototype used 107,827,712 bytes; 2×/16-bit was selected to reduce the transient rendering cost and retained pool footprint. Memory scales with pixel count. The draw figure is the inspected visible submesh submission count; no device GPU milliseconds are claimed. See `gpu-cost.txt` and `lifecycle.txt`.
