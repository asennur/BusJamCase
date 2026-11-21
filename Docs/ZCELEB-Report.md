# ZCELEB-5 - Arc path and natural side wave

21 November 2026. Baseline **db881ae**, implementation **269c21e**, branch **main**. Unity **2022.3.19f1**, MCP pinned to **BusJamCase@c4dcbf17c52e6f1b**. Edit mode only; never Play. No scene save, push or AsenOS access.

## Result

Quadratic screen-space arcs bring the bus from bottom-right to Asen's existing stop, then take it toward bottom-left. Enter/exit duration, stop, hold, camera, scale, suspension, hop controls and all named-step values are unchanged. The ResultTimeline asset diff adds only nine new fields. A Path Mode enum defaults to Arc; Straight keeps the legacy Enter Side/Exit Direction behavior. Path Tilt is a small camera-axis roll following the projected tangent, settling flat during the hold. The isolated Result Timeline Preview can overlay the entry/exit paths in cyan/yellow.

Asen's saved exit curve descends from approximately 0.995 to 0.007. Arc sampling normalizes the curve's authored endpoint values to forward progress, preserving every key, tangent and weight while avoiding an exit-start teleport. Straight retains the previous sampling. Existing moving suspension bounce is added after path evaluation.

The imported Yolcu.fbx Wave was inspected before implementation: its upper arm points outward (about 24 degrees above horizontal) and its bent forearm oscillates upward beside the head. That existing clip supplies the wave rotations. Idle supplies the standing/relaxed pose; the active arm alternates right/left/right via mirrored rotations. Wave Arm Raise, Wave Speed and Wave Amplitude have English tooltips. The forced overhead alignment and all arm/head bone-scale overrides are removed. Existing whole-rider size, hop root squash, timing and deck anchoring remain.

Saved timing: entry **1.05-1.60 s**, hold **1.60-2.25 s**, exit **2.25-2.90 s**; Complete still ends at **3.45 s**. Stop **(0.5, 0.2)**, camera **0/10/18/35**, Bus Scale **0.8**, hop **0.25 / 1.666667 / 0.2**, bounce **0.0973**.

## Evidence

Folder: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/celeb5/`.

- 24 raw Game-view PNGs: enter, arc-mid, stop, hold, exit-arc-mid, exit, plus early/late hold at **580x1250, 1080x1920, 2560x1080**.
- `phase-strip-<resolution>.jpg`: six phases side by side at each resolution.
- `upright/riders-3x-hold*.png`: exact 3x crops of native hold captures, at three hold times per resolution. No rerender, altered rig or camera zoom supplies the close-ups.
- `arc.mp4`: **1080x1920, 30 fps, 108 frames / 3.60 s**. `arc-frames/` retains the native source frames.
- `native-path.tsv`: every capture uses normal visible GameView repaint and its final native render target, with the actual screen UIDocument (`PanelSettings.targetTexture == null`). Existing imported raster artwork and the normal transparent 2x MSAA bus compositor are used. No isolated offscreen UI fixture supplies visual evidence.
- Native Edit-mode readback vertically inverts the overlay, as in earlier reviews. Raw PNGs remain intact; `upright/`, strips and MP4 apply a vertical flip. These are deterministic Edit-mode presentation recordings, not device/Play performance measurements.
- `arc-asset.json`, `timeline-before.asset`, `protected-before.json`, `evidence-verification.json`: settings and file-integrity evidence.

## Verification

`CelebrationArcReview.Verify()` uses an isolated UI fixture and a cloned timeline; it never saves the scene or source timeline.

- **1,953 hold samples** across three resolutions: zero bus drift, maximum local bone-position/scale error **2.98e-8** against the grid passenger at the same Idle phase. No celebration bone scaling.
- `bone-lengths.tsv`: upper-arm length **0.2275** and forearm mesh extent **0.1625418** in rider units match the grid reference for all three riders/resolutions. Measurements use rig-local matrices to avoid float cancellation at the remote pooled stage.
- Every sampled waving hand stays below the head top; minimum clearance **0.3210841 rider units**. Shoulder stays sideward, forearm points upward with a bent elbow, R/L/R arms alternate. Native 3x crops show the result.
- Peak hop **0.2499524 standing heights**, at most two riders airborne. Supporting feet stay in the deck footprint, never below it beyond numerical tolerance, and land at hold boundaries.
- Entry/exit are measurably curved, reach normalized endpoints with the full cast offscreen, preserve the stop and have continuous exit-start progress. Tilt off/on, wave raise/speed/amplitude controls, legacy straight entry/exit and English tooltips pass.
- **20 warmup plus 50 Complete/disable/enable cycles:** zero retained growth; 119 transforms, 302 UI elements, stable texture/material/tween counts, same render-target identity.
- **400 warmed production sampler frames: 0 managed bytes.** Source timeline and saved coins are unchanged.
- `preview-validation.txt`: twelve open/sample/reverse-scrub/resize/close cycles, all four tracks and three sizes; zero owned texture/object/preview-scene growth. Paused preview opens and draws the new path overlay without Console errors.
- Final compilation/Console and protected-file comparisons are recorded in `evidence-verification.json`. Existing protected changes remain uncommitted. Game.unity is never saved or committed.

A transient MCP WebSocket warning occurred during compilation reload. After clearing it and sampling/repainting the paused preview, the final Console contains 0 errors, 0 warnings and 0 logs.
