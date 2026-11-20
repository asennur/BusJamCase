# ZLIGHT — Warm tune, second pass

20 November 2026. Unity **2022.3.19f1**, `main`, following `16fd9f6`. **Final live validation completed; Console 0.** Only `Assets/_BusJam/Settings/PP_Game.asset` changes among runtime assets. No scripts, scenes or materials changed by this pass. No player/APK build and no push.

[Three-column comparison](ZLIGHT-Compare.png): **before ZLIGHT / 16fd9f6 / tuned**, each with Start, Mid (Level_07 fixture) and Complete. [Colour sheet](ZLIGHT-Colours.png), [NoSpecular control](ZLIGHT-NoSpecular.png), [measurements](ZLIGHT-Evidence.json), [audit](ZLIGHT-Audit.json).

## Saved values

Game and LevelEditor retain their shared Global Volume: Default layer, priority **0**, weight **1**, `PP_Game.asset`.

| Setting | 16fd9f6 | Tuned |
| --- | --- | --- |
| Color Adjustments saturation | +20 | **+8** |
| White Balance temperature / tint | +10 / +2 | **+18 / +2** |
| Exposure / contrast / hue shift | +0.15 / +12 / 0 | Unchanged |
| SMH shadows, RGBA trackball | Disabled | **1.015 / 1 / 0.975 / 0** |
| SMH midtones, RGBA trackball | Disabled | **1.035 / 1.005 / 0.965 / 0** |
| SMH highlights, RGBA trackball | Disabled | **1.035 / 1.015 / 1.005 / 0** |
| Shadow / highlight ranges | Disabled | **0–0.3 / 0.55–1** |
| Hue vs Saturation | Default | **(0,.5), (.12,.5), (.20,.36), (.28,.39), (.38,.46), (.46,.5), (.75,.5), (1,.5)** |
| Hue vs Hue | Default | **(0,.5), (.14,.5), (.24,.49), (.34,.485), (.44,.5), (.75,.5), (1,.5)** |

Curve coordinates are normalized hue and curve value; **0.5 is neutral**. All keys have zero in/out tangents and both hue curves loop. Only these two Color Curves overrides are enabled. The selective reduction removes acid yellow-green from the background without desaturating red, pink, purple or blue. The small negative green hue offset moves greens toward warm yellow-green. SMH warms shaded surfaces and highlights; enough highlight blue is retained to keep white geometry neutral.

Unchanged baseline: one key light **255/240/215**, intensity **1.15**, Euler **42/-20/0**; Gradient ambient intensity **1**, sky **.62/.59/.53**, equator **.48/.46/.42**, ground **.30/.275/.24**. All real/fake shadows remain disabled. HDR off, **2x MSAA**, render scale **1**, SRP Batcher on, additional lights disabled. Tonemapping **None**; Bloom threshold **.9**, intensity **.35**, scatter **.6**, Quarter, three iterations, high-quality filtering off; Vignette **.14/.075/.045**, intensity **.18**, smoothness **.4**. Camera post on, post AA None, dithering off, Stop NaN off, volume mask Default. No rim light, DOF, motion blur, chromatic aberration, film grain, SSAO or new renderer feature.

## Screens and colour

All new captures use existing **ZLightPreview**, static **Edit mode**, **1080×1920**, separate prepare/capture editor ticks. The Level_07 asset supplies the board and bus order. Complete is the existing posed overlay, not a played win. The helper preserves authored HUD numbers, so the historical badge says Level 1 although the fixture is Level_07. Captures verify lighting and material readability, not runtime animation. Before and middle columns use the archived original captures; the tuned column is newly rendered after the final NoSpecular correction.

**Start is pixel-identical to both earlier columns: 0 changed pixels.** Its full-screen overlay artwork intentionally bypasses scene grading. The gameplay background uses Screen Space Camera and receives the grade. Mid changes **1,819,360** pixels versus 16fd9f6 (mean absolute channel delta **27.27/255**); Complete changes **1,765,403** (mean **10.06/255**). Overlay artwork stays clean.

Grass, 5×5 median at **900/1600**: **150/215/21 → 155/236/0 → 191/231/102**. Sampled HSV saturation falls from **1.0 to .558** versus 16fd9f6. White passenger, bus and open slot all remain **254/254/254**.

Controlled material substitutions use the same surface and pose, **5×5 median sRGB bytes**, top-left origin: passenger **345/858**, bus **650/438**, open slot **540/700**, locked slot **245/675**.

| Colour | Passenger: before / 16fd9f6 / tuned | Bus: before / 16fd9f6 / tuned |
| --- | --- | --- |
| Red | 255,104,107 / 254,91,68 / 254,100,73 | 255,82,85 / 254,70,50 / 254,84,62 |
| Pink | 255,199,255 / 254,197,254 / 254,202,254 | 255,158,246 / 254,166,251 / 254,171,232 |
| Purple | 239,139,255 / 254,133,254 / 254,141,254 | 189,111,255 / 249,114,254 / 253,122,254 |
| Blue | 96,199,255 / 83,214,254 / 114,214,254 | 76,158,255 / 69,184,254 / 99,184,254 |
| Green | 120,255,148 / 107,254,79 / 184,254,148 | 95,253,118 / 74,254,37 / 172,254,135 |
| Yellow | 255,255,82 / 254,254,0 / 254,254,29 | 255,255,66 / 254,254,0 / 254,254,1 |
| White | 255,255,255 / 254,254,254 / 254,254,254 | 255,255,255 / 254,254,254 / 254,254,254 |

All **21** colour pairs remain distinct. Minimum sampled passenger distance is **52** (Pink/White), bus **53.72** (Pink/Purple), Euclidean sRGB. Some pair distances narrow as saturation is reduced; the previous “all distances retained/increased” claim applies only to the first pass.

## Final NoSpecular validation

Live audit confirms all six affected Simple Lit materials (Red, Pink, Blue, Green, White, BusBody): **_SpecularHighlights = 1 (NoSpecular)**, smoothness **.08**, specular RGB **0/0/0**, no specular/gloss keywords. Local URP 14.0.10 `SimpleLitGUI.SpecularSource` verifies this enum mapping. Purple and Yellow retain their existing user-authored materials.

For each of the seven colours, the same tuned fixture was captured with the previous flag **0** and saved flag **1**, preserving black specular and disabled keywords. MSAA was temporarily disabled only for these paired controls, then restored. Pink, Purple, Blue and Green are **full-frame pixel-identical**; Yellow differs by at most **1/255**, Red and White by at most **2/255**. No new highlight appears. Contact sheet and full-frame difference counts are linked above. The normal acceptance captures use the production **2x MSAA**.

## Editor cost estimate

SMH and both hue curves are evaluated while generating the **existing LDR grading LUT** (`ColorGradingLutPass` / `LutBuilderLdr.shader` in local URP 14.0.10). They add LUT work, **no additional fullscreen post pass, light, shadow pass or renderer feature**. Quarter-resolution Bloom settings and screen pixel workload are unchanged from 16fd9f6.

The serialized profile grows from **4,313 to 13,198 bytes (+8,885 bytes)**. This is an asset-on-disk measurement, **not an APK-size estimate**. No runtime texture/model assets were added. Historical first-pass Mid counts were **300 → 169 draws**, shadow casters **139 → 0**. No fresh per-frame draw/GPU timing is claimed: this helper's repeated manual renders accumulate UnityStats, and Frame Debugger did not return a fresh event frame in this session. Device GPU milliseconds and player size were not measured. **No build was started.**

## Completed verification and cleanup

- Unity **2022.3.19f1**, Edit mode; compilation finished; final Console **0 entries**, including warnings/errors.
- **11 prefabs / 26 renderers**, **2 scenes**, **354 fixture renderers**: no shadow flag failures, **0 missing scripts**. Both scenes still reference the saved profile and retain HDR off / post on / MSAA on.
- Disposable previews removed; saved **Game** scene restored, **clean**, Edit mode, **0 preview roots**. No scene or prefab was saved by the tune.
- **13 protected files checked, 0 changed**, including pre-existing M_Purple/M_Yellow, LevelDatabase, Level_26, ArtSource and Otobus materials. Only this pass's profile and documentation/evidence are committed. No push.
- `ZLightSetup.Apply()` still represents the first-pass preset and would reset saturation/temperature; it was deliberately not run. The saved `PP_Game.asset` is the final tuning source. Source scripts remain outside this pass's scope.

Decision: Use +8 saturation, +18 temperature, warm SMH and selective hue curves to remove acid greens while keeping 3D whites neutral (20 November 2026, ZLIGHT TUNE).
