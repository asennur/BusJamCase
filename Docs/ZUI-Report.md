# ZUI - UI art import

Date: 2026-11-19. Unity: **2022.3.19f1**. Edit mode only; Play mode was never entered.

## Result

| Step | Status | Evidence |
| --- | --- | --- |
| ZM2b cleanup | Complete | Removed `Test_CloakStuck` from the database, deleted the temporary Levels tree, restored SaveSystem index **49**. Database and scene diffs are empty. |
| Staged artwork | Complete | Copied the original **22 PNGs** byte-for-byte to `Assets/_BusJam/Art/UI/`. Mapping tables preserved in [UI-Assets.md](UI-Assets.md). |
| Import settings | Complete | **25/25** importers verified; **50/50** Android/iOS ASTC 6x6 overrides verified. |
| Borders | Complete | **8/8** measured border definitions verified in native source pixels, mapped to Left/Bottom/Right/Top. Blue round button has zero borders. |
| Procedural effects | Complete | Three 256 x 256 RGBA PNGs with transparent corners, generated with Python/Pillow. |
| Shared references | Complete | `Assets/_BusJam/UI/UIArt.asset`: **25/25** named Sprite fields assigned. |
| Fonts | **Blocked; step stopped** | Shell could not connect to `raw.githubusercontent.com`. No TTF, OFL, or TMP font assets were created. |
| Compile / Console | Complete | Compilation finished; Console **0 errors, 0 warnings, 0 logs** after clearing the MCP transport warning and rerunning validation. |
| Contact sheet | Complete | [Edit-mode preview screenshot](ZUI-ContactSheet.png), captured from Unity's editor window. |

## Import details

All sprites use Sprite (2D and UI), Single, Full Rect, alpha transparency, no mipmaps, Normal compression (Compressed, quality 50), and no NPOT rescaling. Default Max Size is 1024; icons and `fx_sparkle` use 512. All three generated effects use 256. Android and iPhone overrides use the same size limits and ASTC 6x6. Importer borders stay in native pixels; Unity scales sprite borders when reducing the texture resolution.

`glow_soft` has a smooth radial falloff with no arms. `fx_confetti_star` has five points, white fill, and the requested 6 px grey outline. `fx_confetti_rect` has a centered 96 x 192 rounded silhouette with white RGB and transparency. Reproduce them with [GenerateUIEffects.py](../Tools/GenerateUIEffects.py) and Pillow.

[ZUI-Validation.md](ZUI-Validation.md) records every sprite's size limit, border, and assigned reference. `Bus Jam > UI Art Preview` opens a reusable editor-only contact sheet; it creates no scene objects. No runtime UI wiring or scene changes were made.

## Open work and boundaries

- Download [Fredoka](https://github.com/google/fonts/tree/main/ofl/fredoka) and [Lilita One](https://github.com/google/fonts/tree/main/ofl/lilitaone) with their OFL licenses when network access is available; generate `Fredoka-Bold SDF` and `LilitaOne SDF`. This package does not claim step 6 is complete.
- `counter_plate.png` appeared in the external staging directory after the original 22 files were copied. It is outside this package's requested 22-source snapshot and is not imported or referenced.
- Original non-English source filenames remain as source identifiers in the copied mapping table; target names, new code, comments, and report prose are English.
- `M_Purple`, `M_Yellow`, and `ArtSource` were left untouched and excluded from the commit. No push.

## MCP handling

The initial cleanup command hit the tool's deletion guard; its single retry succeeded with the explicitly requested deletion authorized. Refresh requests recovered their connection internally and returned success. Opening the preview first failed because the executor did not reference the editor assembly; its single retry used the loaded editor type and succeeded. No operation failed on its retry, so the MCP stop condition was not reached. The final editor state is idle, not compiling, Play off, and the active Game scene is clean.
