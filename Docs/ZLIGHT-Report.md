# ZLIGHT — Warm, shadow-free mobile look

20 November 2026. **Unity 2022.3.19f1**, Android, `main`. Edit mode only; Play was never entered. [Comparison](ZLIGHT-Compare.png), [colour sheet](ZLIGHT-Colours.png), [measurements](ZLIGHT-Evidence.json), [renderer audit](ZLIGHT-Audit.json).

## Saved values

Game and LevelEditor each have one Global Volume sharing `Assets/_BusJam/Settings/PP_Game.asset`: Default layer, priority **0**, weight **1**.

| Setting | Value / reason |
| --- | --- |
| Directional key | RGB **255/240/215**, intensity **1.15**, Euler **42/-20/0 degrees**. Bright tops and camera-facing fronts; no additional lights. |
| Gradient ambient | Intensity **1**. Sky **0.62/0.59/0.53**, equator **0.48/0.46/0.42**, ground **0.30/0.275/0.24**; warm fill replaces cool faces. |
| Shadows | Light **None**; URP main/additional/any/soft shadows **off**, distance **0**. All MeshRenderer/SkinnedMeshRenderer Cast **Off**, Receive **false**, including inactive prefab children and procedural BoardBase. |
| Mobile pipeline | Pipeline/camera **HDR off**, existing **2x MSAA**, render scale **1**, SRP Batcher **on**, additional lights **disabled**; no renderer features added. |
| Camera | Post **on**, post AA **None**, dithering **off**, Stop NaN **off**, volume mask **Default (1)**. Framing unchanged. |
| Tonemapping | **None**. Neutral reduced the controlled white sample from **254/254/254** to **234/226/218**; None preserves bright whites. |
| Color Adjustments | Exposure **+0.15**, contrast **+12**, saturation **+20**, white filter, hue shift **0**. |
| White Balance | Temperature **+10**, tint **+2**. |
| Bloom | Threshold **0.9**, intensity **0.35**, scatter **0.6**, **Quarter** downscale, **3** iterations, high-quality filtering **off**, white tint, clamp **65472**, no dirt texture/intensity **0**. |
| Vignette | RGB **0.14/0.075/0.045**, intensity **0.18**, smoothness **0.4**, centre **0.5/0.5**, rounded **false**. |
| Matte materials | Red/Pink/Blue/Green/White and M_BusBody retain Simple Lit: smoothness **0.08**, specular mode **NoSpecular** (`_SpecularHighlights = 1` in Simple Lit), specular RGBA **0/0/0/1**; specular/gloss keywords disabled. Only Pink's base green changes: **0.5 -> 0.478**. |

No extra rim light, DOF, motion blur, chromatic aberration, film grain, SSAO or Lift/Gamma/Gain. No blob/fake-shadow renderers found or introduced. Background/UI artwork retains its painted shading.

## UI and colour

Both UI canvases already used **Screen Space Overlay** and remain unchanged. Only the background uses Screen Space Camera, existing distance **99**. Frame Debugger places `UGUI.Rendering.RenderOverlays` after post. The entire **1080x1920 Start image is byte-identical: 0 changed pixels**.

Controlled substitutions sample the same passenger/bus surface: **5x5 median sRGB bytes**. Pixel centres, top-left origin: passenger **345/858**, bus **650/438**, open slot **540/700**, locked slot **245/675**.

| Colour | Passenger before -> after | Bus before -> after |
| --- | --- | --- |
| Red | 255/104/107 -> 254/91/68 | 255/82/85 -> 254/70/50 |
| Pink | 255/199/255 -> 254/197/254 | 255/158/246 -> 254/166/251 |
| Purple | 239/139/255 -> 254/133/254 | 189/111/255 -> 249/114/254 |
| Blue | 96/199/255 -> 83/214/254 | 76/158/255 -> 69/184/254 |
| Green | 120/255/148 -> 107/254/79 | 95/253/118 -> 74/254/37 |
| Yellow | 255/255/82 -> 254/254/0 | 255/255/66 -> 254/254/0 |
| White | 255/255/255 -> 254/254/254 | 255/255/255 -> 254/254/254 |
| Open slot | 255/255/255 -> 254/254/254 | - |
| Locked slot | 153/82/239 -> 168/69/213 | - |

**21/21 passenger pair distances retained/increased** (Euclidean sampled sRGB); minimum **56 -> 57**. Pink's material correction prevents White separation shrinking. Bus samples remain separate, minimum **52.33**; four bus-pair distances tighten under the warm grade, recorded in the JSON. Purple/Yellow are untouched.

Bloom works with HDR off: disabling it on the same white fixture changes **1,731,438 pixels**, mean absolute channel difference **4.56/255**. Controlled Bloom-off and None/Neutral captures are included.

## Cost

Frame Debugger hierarchies and UnityStats counters from the same fixed-resolution Game view frame:

| Screen | Draw calls before -> after | SetPass before -> after | Shadow casters before -> after |
| --- | --- | --- | --- |
| Start | 13 -> 21 | 7 -> 15 | 0 -> 0 |
| Mid, Level 07 | **300 -> 169 (-43.7%)** | 13 -> 21 | **139 -> 0** |
| Complete | 79 -> 78 | 14 -> 21 | 9 -> 0 |

`MainLightShadow/Shadows.DrawSRPBatcher` is removed; no after-frame has a shadow pass. Post adds one LUT draw and seven small Bloom draws; Uber replaces FinalBlit. Estimate: approximately **0.4 million extra LUT/Bloom pixels/frame**, plus Uber's grading samples, while removing 139 caster draws. Bloom sizes: **270x480 -> 135x240 -> 67x120**. No HDR buffer. Device GPU milliseconds were not measured in Edit mode.

Last verified paired Android APKs: **28,385,859 -> 28,380,159 bytes**, **5,700 bytes smaller (-0.020%)**, both **27.07 MiB**. `Temp/ZLIGHT-Final.apk` built with **0 errors / 0 warnings**, before the final material-mode correction. No runtime textures/models added. Guarding the Editor-only solver setting with `UNITY_EDITOR` removes its existing Android CS0414 warning without changing Editor behavior.

## Final validation attempt (resume, 20 November 2026)

**Live validation remains pending.** The initial editor-state request and six retries, each after at least 20 seconds, failed. Editor-state and Console requests returned `Unity session not ready ... ping not answered`; the direct status `execute_code` request returned `success: false`. The server still lists BusJamCase on Unity 2022.3.19f1, and the Unity process is running, but neither proves the current scene or compilation state. No domain reload, script edit, Play entry or setup reapplication was triggered during this resume.

- Disk verification passed for all six mutable Simple Lit materials: `_SpecularHighlights = 1` (NoSpecular), `_Smoothness = 0.08`, black specular RGB and disabled specular/gloss keywords. `git diff --check` passed.
- Current Edit mode, completed compilation, clean Game scene, zero missing scripts and zero disposable preview objects could **not** be confirmed. No preview mutation was attempted during this resume; any preview left by the earlier failed run remains unverified.
- Start, Mid Level 7, Complete and seven colour captures could **not** be recaptured. Existing After captures, colour sheet and comparison remain the earlier evidence, before the final NoSpecular correction; no new-highlight or colour-equivalence claim is made for the last change. The comparison was not regenerated from stale inputs.
- Final Console inspection could **not** be completed; zero package errors/warnings is not newly certified.
- **APK rebuild deliberately skipped as requested. Asen's final Android build is pending** and must include the final NoSpecular correction. Earlier APK sizes below are historical measurements only.
- All **13 protected files** still match the earlier SHA-256 manifest. Protected assets are excluded from the ZLIGHT commit. AsenOS remains untouched by this resume-only scope. No push.
- Computer Use could not bring Unity forward: access was rejected with `Computer Use was not approved to use unity`.

## Earlier verification (before final NoSpecular correction)

- Earlier compilation was clean and Console **0**, before the final material-mode correction. **11 prefabs / 26 renderers**, **2 scenes**, **354 fixture renderers** including BoardBase: no shadow-flag failures or missing scripts. The final live revalidation attempt is documented above.
- **336 existing Transform/RectTransform blocks** checked against main: only the two directional-light rotations change. Layout preserved.
- Captures use static Edit-mode Start, actual Level_07 fixtures with all seven passenger colours, and a posed Complete overlay. Serialized HUD numbers remain as authored. These validate lighting, not runtime animation. Previews were removed and saved Game confirmed clean before the failed retry; current editor-memory state remains unconfirmed after the six resume retries.
- **13 protected files checked, 0 changed**. M_Purple, M_Yellow, LevelDatabase, Level_26, ArtSource and Otobus Materials were not edited by ZLIGHT and are excluded from its commit; their pre-existing local changes are preserved. AsenOS untouched. No branch changes or push.
- `ZLightSetup` reapplies settings; `ZLightPreview` captures on separate prepare/capture editor ticks using Unity's [2022.3 Game view rendering](https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/Editor/Mono/PlayModeView/PlayModeView.cs).

Computer Use rejected Unity access ("Computer Use was not approved to use unity"); completed editor work and verification used Unity MCP.
