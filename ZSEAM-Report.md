# ZSEAM — continuous HUD surface rendering

Validated on 21 November 2026 with Unity 2022.3.19f1, pinned instance `BusJamCase@c4dcbf17c52e6f1b`. Work began at `b2d5372` on main. The editor was initially in Play; it was stopped before inspection, and Play was never entered during this work. No scene was saved. AsenOS was not accessed.

## Cause

`surface_white.svg` imports as a 1536×1029 Texture2D with 11 mip levels, Trilinear filtering and Clamp wrapping. Its old borders were L327/R188/T233/B173, with an inline slice scale of 0.2. The 290×116 counter's inset artwork is 277×97. This leaves only 15.8 UI units for the 623-source-pixel center strip: approximately 39.4:1 vertical minification, versus 5:1 in the corner tiles. Different tile derivatives select different mip footprints along a shared edge. The center's coarse filtering spreads translucent edge/shadow texels into the visible rectangular right-middle patch.

A controlled Edit-mode test kept the original borders and texture but temporarily forced mip zero using a -10 mip bias. The rectangular patch disappeared, while the bottom contour step remained. The bias and inline diagnostic styles were restored afterward. See `diagnostic-mip0-start-1080x1920.png` beside the normal `before-start-1080x1920.png`.

There is also a geometric mismatch: the bottom cut at raster row 856 intersects the curved painted corner. At alpha >=128 the right contour is x1499 near the top/center cut but x1489 at row 856. The right cut at x1348 likewise intersects the bottom contour at y975 rather than the center's y978. These nonuniform painted contours and shadow layers are unsuitable for independent tile scaling. Increasing padding or enabling Clamp cannot repair their internal slice joins; Clamp was already enabled.

## Change

Remove the USS slice borders from white, header, modal and badge surfaces and button art, plus their inline slice scales in `VectorArt`. Each painted background now renders as one continuous stretched image. Shared white decoration covers the lives, coin, result coin, sound toggle and confirmation surfaces. Green/red/yellow button states and editor buttons share the unsliced art rule. Tip and blue surfaces were already unsliced.

Existing raster sizes, mipmaps, Trilinear filtering, Clamp, SVG source files, layouts and hit targets are retained. The whole-image mapping preserves the painted contours' continuity; corner proportions now follow the destination aspect instead of retaining separate fixed-size corners. No runtime polling, allocations or new components were added.

## Evidence and checks

Evidence folder: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/seam/`.

- Nine before and nine after native Game View final-target captures: Start, HUD and a surface audit gallery, each at 580×1250, 1080×1920 and 2560×1080. `PanelSettings.targetTexture` remained null. The saved Game View buffer's inverted Y orientation was corrected during readback; native images were not resized.
- `compare-start-{lives-plate,coins-plate}-{resolution}-4x.png`: side-by-side nearest-neighbor 4× crops of both counters. Matching individual crops and physical bounds are included. HUD badge/timer crops and full native-size comparisons are also included.
- `compare-surfaces-{resolution}-native.png`: modal, header, badge, white plate, tip panel and all three button colors. Visual inspection found continuous contours after the fix at all three sizes.
- The current UXML places lives/coins on Start and the coin counter on Complete; its gameplay HUD contains level and timer. The shared decoration fix applies to all these uses without moving or adding counters.
- `texture-audit.txt`, source raster exports, `raster-game-view-path.txt` and the temporary `SeamUiReview.cs` capture harness are retained in the evidence folder. The harness was removed from Assets after capture.
- Final script refresh/compilation completed in Edit mode; final Console and editor-state evidence are saved separately. No Play validation was performed, as requested.
- Protected-file hashes were checked against the session baseline. Game.unity, protected tracked assets and spriteatlases were unchanged. Four protected source files changed outside this task's writes during the session: `ArtSource/Otobus.blend`, `Otobus.blend1`, `Yolcu.blend`, `Yolcu.blend1`. They were left untouched and excluded from the commit. See `protected-check.json`.

No push.
