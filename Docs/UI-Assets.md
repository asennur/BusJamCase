# UI image staging report

Scope: PNG files modified on 2026-11-19 after 14:00 local time, Downloads. Read the requested 2D-Asset-Listesi.md. Every source was viewed directly with view_image; APNG frames were additionally reviewed on a dark background. No repository or Unity operations were performed.

23 source files → 22 unique staged targets + 1 alternate sparkle. Source pixels were never rescaled. Native crop plus 8 transparent pixels on every side may make a canvas slightly larger than the source; this is padding, not upscaling. All output PNGs are static RGBA with all four corner alphas 0 and eight fully transparent rows/columns at each edge.

## Source → target mapping

| Source file | Target | Confidence | Output / note |
| --- | --- | --- | --- |
| Beyaz Radyal Işık Patlaması.png | `glow_rays` | High (0.99) | [stage\glow_rays.png](../ArtSource/UI/glow_rays.png) — White radial burst; already true alpha. Broad ray fade reaches top/bottom source boundary (clipped halo). Source 1536×1024; staged 1470×1040. |
| parilti_saydam_animasyon.png | `fx_sparkle` | High (0.99) | [duplicates\fx_sparkle.png](duplicates\fx_sparkle.png) — DUPLICATE: 23-frame APNG showing a pulsing four-point sparkle, not a round glow. Peak frame 12 staged under duplicates/fx_sparkle.png; canonical sparkle uses the larger static source. Source 512×512; staged 528×528. |
| Parlak 3D “Seviye Başarısız” Yazısı.png | `title_failed` | High (0.99) | [stage\title_failed.png](../ArtSource/UI/title_failed.png) — Text verified: LEVEL FAILED (English, correct). Source 1774×887; staged 1504×854. |
| Parlak Altın Kronometre İkonu.png | `icon_clock` | High (0.99) | [stage\icon_clock.png](../ArtSource/UI/icon_clock.png) — Gold stopwatch with red hand; no unwanted letters/numbers. Source 1261×1247; staged 975×1247. |
| Parlak Altın Otobüs Madalyonu.png | `icon_coin` | High (0.99) | [stage\icon_coin.png](../ArtSource/UI/icon_coin.png) — Single gold coin with side-view bus emblem; isolated rim specks cleaned. Source 1263×1246; staged 1182×1234. |
| Parlak Altın Otobüs Simgeli Madeni Para Yığını.png | `coin_pile` | High (0.99) | [stage\coin_pile.png](../ArtSource/UI/coin_pile.png) — Gold coin pile uses FRONT-view buses, unlike the side-view bus on icon_coin; emblem inconsistency. Contact shadow/halo partly meets source boundary. Source 1536×1024; staged 1361×998. |
| Parlak Açık Gri Kart UI Elementi.png | `panel_inner` | High (0.99) | [stage\panel_inner.png](../ArtSource/UI/panel_inner.png) — Off-white 3:2 content card. Strong upper-left gloss and gradient make unrestricted two-axis slicing imperfect. Source 1536×1024; staged 1552×1040. |
| Parlak Dört Uçlu Işık Parıltısı.png | `fx_sparkle` | High (0.99) | [stage\fx_sparkle.png](../ArtSource/UI/fx_sparkle.png) — Four-point white sparkle, canonical static version. Faint halo meets source top/bottom boundary. Source 1536×1024; staged 1137×966. |
| Parlak Kırmızı Altın Çerçeveli Oyun Butonu.png | `btn_red` | High (0.99) | [stage\btn_red.png](../ArtSource/UI/btn_red.png) — Empty red/gold pill. Bottom extrusion is thicker than top bezel. Source 2172×724; staged 2161×727. |
| Parlak Kırmızı Kalp İkonu.png | `icon_heart` | High (0.99) | [stage\icon_heart.png](../ArtSource/UI/icon_heart.png) — Intact red heart, correct. Source 1294×1216; staged 1252×1169. |
| Parlak Kırmızı X Düğmesi.png | `icon_close` | High (0.99) | [stage\icon_close.png](../ArtSource/UI/icon_close.png) — Cream X on red circle; X is intentional icon artwork, not unwanted text. Source 1265×1243; staged 1061×1175. |
| Parlak Lacivert Yuvarlak Banner.png | `panel_header` | Medium (0.78) | [stage\panel_header.png](../ArtSource/UI/panel_header.png) — MEDIUM match: empty navy plate with blue/cyan rim. Could also serve counter_plate; assigned to header only. Actual silhouette is about 3.5:1, rather than requested 4:1; counter_plate remains missing. Source 2172×724; staged 2188×702. |
| Parlak Mavi 3D Düğme.png | `btn_round_blue` | High (0.99) | [stage\btn_round_blue.png](../ArtSource/UI/btn_round_blue.png) — Empty blue circular button. No flat rectangular middle: use uniformly scaled Simple sprite, NOT normal 9-slice. Source 1261×1247; staged 1211×1239. |
| Parlak Mavi Yuvarlatılmış Panel.png | `panel_modal` | High (0.99) | [stage\panel_modal.png](../ArtSource/UI/panel_modal.png) — Vertical blue popup. Source has irregular white/blue edge debris; detached specks cleaned. Thick bottom depth and upper-left gloss prevent perfectly uniform bezel slicing. Source 1122×1402; staged 1138×1410. |
| Parlak Mor Kenarlıklı Rozet.png | `badge_level` | High (0.99) | [stage\badge_level.png](../ArtSource/UI/badge_level.png) — White square badge with purple bezel. Detached magenta specks cleaned; lower extrusion is heavier. Source 1261×1247; staged 1229×1255. |
| Parlak Mor Kurdele Banner.png | `ribbon_purple` | High (0.99) | [stage\ribbon_purple.png](../ArtSource/UI/ribbon_purple.png) — Empty arched purple folded ribbon; broad outer purple glow. No baked text. Source 1536×1024; staged 1525×652. |
| Parlak Mor Yuvarlak Banner.png | `panel_tip` | High (0.99) | [stage\panel_tip.png](../ArtSource/UI/panel_tip.png) — Empty dark grey-purple tip plate. More pronounced glossy highlight than requested; silhouette about 4:1. Source 2172×724; staged 2188×695. |
| Parlak Sarı 3B Hap Düğme.png | `btn_yellow` | High (0.99) | [stage\btn_yellow.png](../ArtSource/UI/btn_yellow.png) — Yellow/gold pill had baked pale/checker-like fringe outside the outline. Removed using colored silhouette and antialiased edge reconstruction; no white halo retained. Source 2172×724; staged 2153×682. |
| Parlak Yeşil Altın Çerçeveli Buton.png | `btn_green` | High (0.99) | [stage\btn_green.png](../ArtSource/UI/btn_green.png) — Empty green/gold pill; reference kit appearance. Lower gold extrusion thicker than top. Source 2172×724; staged 2179×719. |
| Parlak Yeşil Artı Butonu İkonu.png | `icon_plus` | High (0.99) | [stage\icon_plus.png](../ArtSource/UI/icon_plus.png) — White plus on green rounded square; plus is intentional icon artwork. Detached green specks cleaned. Source 1261×1247; staged 1120×1162. |
| Parçalanmış Parlak Kırmızı Kalp.png | `icon_heart_broken` | High (0.99) | [stage\icon_heart_broken.png](../ArtSource/UI/icon_heart_broken.png) — Split red heart, two separated halves. Left silhouette reaches the source boundary: possible minor clipped edge; missing pixels cannot be recovered. Source 1302×1208; staged 1293×1151. |
| Seviye Tamamlandı!.png | `title_complete` | High (0.99) | [stage\title_complete.png](../ArtSource/UI/title_complete.png) — Text verified: LEVEL COMPLETED! (English, correct). Decorative stars/strokes preserved. Source 1774×887; staged 1737×815. |
| चमकीला नीला रिस्टार्ट तीर आइकन.png | `icon_restart` | High (0.99) | [stage\icon_restart.png](../ArtSource/UI/icon_restart.png) — Baked RGB checkerboard removed by blue outer silhouette segmentation, hole fill and edge-color reconstruction. Blue circular backing remains part of the image; requested cream arrow was standalone. Background shadow omitted because it contains checkerboard. Source 1254×1254; staged 1165×1178. |

## Missing targets

- 08 `counter_plate` — no separate source; navy header is ambiguous and was assigned once only.
- 23 `glow_soft` — no round, arm-free soft glow; animated PNG is a sparkle.
- 25 `fx_confetti_star` — no standalone flat five-point confetti star.
- 27 `fx_confetti_rect` — no standalone confetti rectangle.

26 and 28 intentionally excluded. No substitutes were fabricated.

## 9-slice measurements (native pixels)

Values are visual measurements, approximately ±10–20 px, not exact vector geometry. Bezel thickness is measured at the middle of each side from visible outline to the face; it excludes empty padding. Suggested protected borders are measured from final PNG edges and include padding, rounding, extrusion/shadow and upper-left gloss. Order is **left / right / top / bottom**; Unity Sprite Editor fields are Left, Bottom, Right, Top, so enter by field name. Gradients are intentional; “flat middle” means no corner/bezel geometry, not constant RGB.

| Target | Mid-side bezel L/R/T/B (px) | Suggested sprite borders L/R/T/B (px) | Suitability |
| --- | --- | --- | --- |
| `btn_green` | 72 / 77 / 69 / 146 | 425 / 370 / 300 / 350 | Conservative starting values; glossy gradients and unequal bottom depth distort under large vertical changes. Prefer modest resizing. |
| `btn_yellow` | 65 / 72 / 65 / 134 | 425 / 370 / 300 / 340 | Conservative starting values; glossy gradients and unequal bottom depth distort under large vertical changes. Prefer modest resizing. |
| `btn_red` | 73 / 75 / 67 / 145 | 425 / 370 / 300 / 350 | Conservative starting values; glossy gradients and unequal bottom depth distort under large vertical changes. Prefer modest resizing. |
| `btn_round_blue` | 72 / 77 / 64 / 151 | N/A — curvature reaches center on every side | NOT a 9-slice asset: circular silhouette has no flat middle. |
| `panel_modal` | 74 / 75 / 64 / 105 | 330 / 250 / 340 / 230 | Conservative starting values; glossy gradients and unequal bottom depth distort under large vertical changes. Prefer modest resizing. |
| `panel_header` | 61 / 64 / 61 / 86 | 370 / 260 / 250 / 260 | Conservative starting values; glossy gradients and unequal bottom depth distort under large vertical changes. Prefer modest resizing. |
| `panel_inner` | 28 / 29 / 29 / 51 | 330 / 190 / 235 / 175 | Conservative starting values; glossy gradients and unequal bottom depth distort under large vertical changes. Prefer modest resizing. |
| `badge_level` | 93 / 95 / 83 / 126 | 410 / 270 / 350 / 270 | Conservative starting values; glossy gradients and unequal bottom depth distort under large vertical changes. Prefer modest resizing. |
| `panel_tip` | 27 / 28 / 24 / 52 | 405 / 245 / 250 / 190 | Conservative starting values; glossy gradients and unequal bottom depth distort under large vertical changes. Prefer modest resizing. |
| `counter_plate` | N/A | N/A | Missing source. |

The pill buttons are best stretched horizontally; keep height close to native aspect. The popup, badge and inner card can be sliced conservatively, but their highlights and deeper lower bevels mean the source kit does not meet the strictly uniform bezel requirement. The circle must be scaled uniformly. Border-guide previews are in `review/`; they show the suggested cut positions.

## Transparency and quality checks

- Most sources already contain real alpha; the black shown by raw viewers is transparent, not a black matte. Existing alpha/soft shading was retained. Tiny detached components below 180 pixels were removed; legitimate title decorations remain.
- The restart source alone was RGB with an opaque baked checkerboard. Its outer blue silhouette was isolated, with a soft contour and colors reconstructed from nearby interior pixels to prevent grey/white edge halos. Its checkerboard-contaminated outside shadow was removed.
- The yellow pill had a pale outer fringe. Its colored outline was isolated and its edge colors reconstructed; compare `review/staged_dark.jpg` and `review/staged_light.jpg`.
- FX outputs use neutral white RGB and the supplied alpha, removing slight source color/dark contamination without changing opacity. No colored confetti sources exist.
- Faint ray/sparkle halos and some object shadows reach source bounds; added padding cannot restore already clipped information. `icon_heart_broken` has possible left-edge clipping.
- Visual style is broadly consistent glossy casual UI. Mismatches: standalone coin versus pile bus emblems; restart has an extra blue circular backing; strongly asymmetric top gloss/bottom depth across 9-slice candidates.
- Both title strings are correct English. **WRONG TEXT: none.** No unwanted baked words, letters or numbers were observed. X and + are required icon symbols.

## Duplicates

`parilti_saydam_animasyon.png` and `Parlak Dört Uçlu Işık Parıltısı.png` both depict four-point sparkles. Larger static source supplies `stage/fx_sparkle.png`. APNG peak frame 12 supplies `duplicates/fx_sparkle.png`; animation was not silently treated as a round glow. Original APNG remains unchanged in Downloads.

## Reproducibility

`sources.json` records source dimensions/alpha/frame count; `verification.json` records exact crop rectangles, final dimensions, padding, corner alpha and source SHA-256. `stage_assets.py` performs processing and assertions; local dependencies are in `.packages/`.

## Unity import (ZUI, 2026-11-19)

The tables above preserve the staging measurements and original source filenames. ZUI copied all 22 canonical staged PNGs without changing their bytes and added `glow_soft`, `fx_confetti_star`, and `fx_confetti_rect` using `Tools/GenerateUIEffects.py`. `counter_plate` was missing in this source snapshot; a new file appeared in staging after the initial copy and is excluded from the requested 22-source package. No field or substitute was added for it. The alternate sparkle is intentionally excluded.

All 25 assets are Single sprites, Full Rect, with alpha transparency, no mipmaps, Normal compression (quality 50), and Android/iOS ASTC 6x6 overrides. Max Size is 1024 for UI artwork (including `glow_rays`), 512 for icons and `fx_sparkle`, and 256 for all three generated effects. Suggested borders were transferred by field name from native L/R/T/B measurements to Unity L/B/R/T. Unity scales the runtime sprite borders with the imported resolution; importer borders remain in source pixels. `btn_round_blue` has zero borders and is intended for Simple images.

`UIArt.asset` contains one assigned Sprite field for each of the 25 names. Open **Bus Jam > UI Art Preview** to view the imported sprites without modifying a scene.

The generated effects have white fills and alpha; the star also has the explicitly requested neutral grey (RGB 185) 6 px outline. The rectangle silhouette is 96 x 192 px inside a transparent 256 x 256 canvas. The glow uses a smooth radial falloff without rays or arms.

Fonts are incomplete: the official Google Fonts download was blocked by the shell network policy, so step 6 stopped as instructed. No TTF, license, or TMP font assets are included. Official sources: [Fredoka](https://github.com/google/fonts/tree/main/ofl/fredoka), [Lilita One](https://github.com/google/fonts/tree/main/ofl/lilitaone).