# ZWIN — Complete and Fail

21 November 2026. `main`, Unity **2022.3.19f1**, MCP pinned to `BusJamCase@c4dcbf17c52e6f1b`. Edit mode throughout; no Play, scene saving, build or push. AsenOS access was restricted to the supplied read-only reference folder.

Constructed `title_complete.svg` and `title_failed.svg` from the real **Fredoka Bold** glyph outlines using fontTools. Fredoka's rounded terminals were closer than Lilita One in the font comparison. Both preserve the PNG's two-line hierarchy and tracking/size proportions, with individually fanned letters, layered purple/navy extrusion, rounded outlines, vertical face gradients and clipped white gloss. Complete's stars/bursts and the folded gradient ribbon are hand-authored paths. All six added SVGs import as **UI Toolkit Vector Images**; no tracing, embedded raster images or SVG text. The replaced text-title elements are removed. The existing coin pile and controls are retained.

## Timing

Reference frames were independently extracted with ffmpeg at **0.05-second intervals**: 82.00–88.30 and 102.00–107.30. The final extra 0.05-second endpoint covers the last frame. The tables in Video-Envanteri and measured frame bounds informed the timings below; reference uncertainty is about ±0.15 s. `t=0` is the existing final bus departure/Complete event, or Fail event. The final bus already departs over 0.30 s; gameplay/bus code is unchanged.

| Complete step | Reference video | Ours after event |
| --- | --- | --- |
| Last bus leaves | 82.00–82.30 | 0.00–0.30 s, existing departure |
| Stars, hearts, discs and paper burst; rotate/fall | 82.30–83.05 | 0.30–1.095 s including 0.045 s stagger |
| Dark purple overlay | 83.05–83.20/83.35 | 1.05–1.25 s, linear alpha |
| Title/ribbon and purple halo open | ~83.75–84.10 in extracted frames | 1.75–2.05 s, OutBack with 10% overshoot |
| Coin pile grows and amount counts | ~84.50–85.45 | 2.50–3.40 s, pile OutBack; cached 0→25 OutQuad values |
| Continue grows, overlapping count | ~84.55–85.00 | 2.60–2.90 s, OutBack |
| Idle | 85.45 until user input | 5°/s rays, staggered title twinkles and pile glints |
| Continue glow and coin collection | 86.50–87.70 | Click +0.00–1.20 s; 14 staggered curved paths |
| Screen exit | 87.85–88.30 | Click +1.35–1.80 s, then LoadNext |

| Fail step | Reference video | Ours after event |
| --- | --- | --- |
| Board remains visible | 102.00–102.60 | 0.00–0.60 s |
| Actual Fail panel/overlay settle | 105.70–105.90 | 0.60–0.80 s; 0.86→1 scale with small overshoot |
| Life HUD 5 FULL→4/countdown and rising heart | 105.80–106.40 | 0.70–1.30 s; existing 20-minute life rule gives 19:59 |
| Try Again exit | 106.90–107.30 | Click +0.00–0.40 s, then Retry |

The unsupported Out Of Tiles / Keep Playing offer is omitted. Fail comparison time jumps from the initial board hold to 105.70 at our 0.60 s; the reference's final 107.30 frame is held for the requested 2.50-second comparison. The Try Again comparison click is at our 1.80 s. The raw reference frames remain available separately.

## Behavior and hygiene

Continue starts collection instead of loading immediately. The display advances per landed coin; the final arrival calls the existing save once behind `rewardCredited`. A repeated click cannot restart collection. Tapping during intro finishes the reveal and consumes that pointer; tapping during collection finishes collection/exit. Leaving or disabling an unfinished Complete credits the pending reward once. Fail always shows the broken heart and -1, preserves the existing single life spend, and never plays confetti; Retry and red X use the same exit fade.

The pools are built once per document: 54 confetti, 14 coins, 8 title twinkles, 4 pile glints and one floating heart. Five reusable DOTween drivers use the same deterministic samplers as the Edit-mode review. Disable rewinds/pauses them; destruction kills them. Count/arrival text is cached outside animation frames. No new runtime VisualElements per play.

`ResultScreenReview` uses isolated preview scenes, cloned PanelSettings, 4x-MSAA render targets and a temporary camera reading the existing scene. It never saves the Game scene or changes progression/lives/coins. DOTween skips initialization in Edit mode, so the review adapter explicitly disposes only its own tween handles through the pinned package's disposal path; runtime code uses the normal Kill/Rewind API.

## Evidence and verification

Evidence: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/win/`.

- `complete-intro-timeline.png` and `fail-intro-timeline.png`: side-by-side reference/ours at **0, 0.3, 0.6, 1.0, 1.5, 2.0, 2.5 s**. Every frame cell is 1080×1920; reference 1640×2360 content is aspect-preserved and letterboxed. Smaller `*-preview.jpg` versions and individual full-size pairs are included.
- `complete-reward-timeline.png` and `complete-collect-timeline.png`: later reveal/count and the complete Continue/flight/fade sequence.
- `ours/`: **18 end-state captures** across 1080×1920, 1080×2400, 1170×2532, 1536×2048, 1080×1080, 1768×2208, 2208×1768, 1920×1080 and 2560×1080; both nine-resolution contact sheets are included.
- Six `*-compare-1x/2x.png` files compare original PNGs with Unity's actual VectorImage rendering at on-screen size and 2x. These are constructed reinterpretations of the PNGs, with inspectable glyph paths and clean scalable edges.
- `reference-measurements.json`, `reference-*-steps.jpg` and `bounds.tsv`: measured source colour bounds, scale progression and our resolved element positions. Stable source yellow title bounds are normalized **x=.2671, y=.1068, w=.4793, h=.1182**. Our title element is **x=268, y=106, w=544, h=396 px** at 1080×1920; its centered artwork occupies only part of that element. Existing assets and portrait adaptation explain layout differences.
- `validation.txt`: Complete **20 runs**, Fail **20 runs**, then **50 full Complete/Fail/disable/enable cycles** sampled at 0.05 s. Elements **350→350**, callbacks **338→338**, active tweens **5→5**: zero retained pool growth. All five samplers together measured **0 allocated bytes over 200 warmed frames**. Reward guard, duplicate clicks, skip endpoints, 5→4 life HUD, retry exit and disable rewind checks passed; saved coins unchanged.
- `evidence-verification.json`: all 18 end-state files have correct dimensions/nonblank content; **15 protected files hash-identical**. Final compilation/Console evidence is in `final-unity.json`. The Game scene's pre-existing on-disk changes remain uncommitted.

These checks drive the actual UI samplers in Edit mode. They do not claim a Play-mode gameplay, PlayerPrefs-write or level-rebuild execution test; runtime progression uses the existing GameManager calls after the tested fade endpoints.

Not implemented, as requested: **x2/ad button, progress bar/unlock badge, CONGRATS unlock screen, 3D celebration bus/jumping passengers, Out Of Tiles/Keep Playing offer**.
