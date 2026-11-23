# Performance

Measured on 23 November 2026 in Unity 2022.3.19f1 with the Device Simulator: **iPhone X** (1125×2436) and **Galaxy S10e** (1080×2280). The simulator reproduces screen size and safe area only, not device speed. Draw calls, triangles, GC allocations and memory are device-independent and reliable. Frame times are editor CPU times and should be read as relative indicators.

## Frame cost

| Scene / moment | Batches (avg / max) | SetPass | Triangles (avg / max) | Game CPU ms (avg / p95) |
| --- | --- | --- | --- | --- |
| Main menu | 42 / 44 | 9 | 45k / 47k | 1.65 / 1.92 |
| Level 2 (3×3) | 93 / 114 | 13 | 63k / 84k | 2.04 / 2.28 |
| Level 10 (7×7, two tunnels) | 199 / 242 | 15 | 106k / 151k | 1.79 / 2.18 |
| Level 24 (tunnel + cloaks) | 233 / 245 | 16 | 136k / 153k | 2.04 / 2.36 |
| **Level 27 (9×9, 40 passengers)** | **316 / 348** | 18 | **184k / 231k** | 2.01 / 2.88 |
| Level Completed celebration | 227 / 378 | 23 / 40 | 107k / 212k | 2.06 / 2.92 |

- The frame budget at 60 FPS is 16.7 ms. Assuming mobile cores are 3–4× slower than the desktop, the busiest level estimates at 7–11 ms, so gameplay is expected to hold 60 FPS. A short dip is possible during the Complete celebration.
- Our own scripts' `Update` calls total ~0.07 ms per frame on Level 27. The rest is Unity's work: render submission ~0.9 ms, Animator ~0.5 ms, physics ~0.2 ms.

## Allocations and memory

- **0 B per frame from game code.** About 0.6 KB per frame comes from URP 14 itself (`RTHandle` name strings per camera). Small, event-time allocations come from DOTween on taps and bus moves.
- Loaded textures: 32.7 MB. Meshes: 25.9 MB. Four previously uncompressed textures now use mobile ASTC compression: **9.7 MB → 2.5 MB**, with no visible banding.
- The bus pool is pre-warmed at level load (5 buses), so no `Instantiate` happens mid-level.

## Long session (30 consecutive levels)

The run included 3 fails with Try Again, 2 pauses and 2 returns to the main menu.

- **No leaks.** Object, tween, event-subscriber and UI element counts grow only until the pools fit the largest level played so far, then stay flat. After replaying five heavy levels, object and subscriber counts were identical.
- **No slowdown.** Game CPU time and GC per frame stayed the same from level 1 to level 30 (~1.8 ms, ~624 B from URP).
- **Pause runs at 30 FPS**, which saves heat and battery on a static screen. Play returns to 60 FPS.

## Rendering setup

URP mobile asset: render scale 1.0, MSAA 2×, HDR off, no realtime shadows, SRP Batcher on, quarter-resolution Bloom and a vignette. Gameplay uses one keyword-free toon shader. The session used 25 shaders and 26 variants in total.

## Possible next steps

- **Merge board tiles into one mesh:** about −130 draw calls on Level 27.
- **Script-driven physics:** physics is only used for tap raycasts.
- **Profile on a real device:** use a Development Build with Autoconnect Profiler.
