# ZUIR reference UI redesign

## ZUIR-1 — HUD and buttons — 21 November 2026

Constructed the HUD and button families from the video reference rather than the previous painted PNGs: UI-01, UI-02, UI-27–34, UI-75 and an adapted clock. Lives/coins remain in their existing screen contexts; gameplay retains its live timer. The coin plus is non-pickable decoration. All existing named UXML elements, controller classes, bindings and `VectorButton` code are preserved. Two non-pickable text wrappers fit the controller-sized coin labels without changing their punch animation.

`Tools/GenerateHudArt.py` follows the constructed SVG/ZAA path used by `GenerateResultArt.py` and `RedrawSvgArt.py`: explicit silhouette/rim/gloss paths, multi-stop gradients and nested translucent shadow layers. It does not trace the video, embed bitmaps or bake live text. The 24 affected SVGs import through `AntialiasedSvgImporter`, with complete mip chains and trilinear filtering. CTA slices are 280/280/310/310 texture pixels at scale .22; counter slices are 85/85/88/88 at .35. These margins contain the complete corner, rim and shadow. Old badge art is no longer referenced by Theme.uss; its file remains.

Measured control widths at 1080×1920: level badge 9.81% W (painted face ≈9.2%), gear 7.22% (painted disk ≈6.4%), lives/coins/timer groups 20%, plus 3.15%, CTA/Start 31.30% (painted width ≈30%). Gameplay badge starts at x=265; gear at x=737; timer remains between them. Start counters start at x=318/548 and gear at x=964. Landscape keeps its existing row layout and 32%/700px Start-button rule; square retains its own layout rules. Captions remain Fredoka SDF UI Toolkit text, with white fill, dark outline and drop shadow. The badge's small Level label is dark, as in the reference.

Evidence root: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/uir/`.

- `reference/`: nine full-resolution 1640×2360 ffmpeg extractions, including t0030/t0060; each requested element has a source crop and 4× crop. `Element-Checklists.md` records silhouette, radius, rim colours, depth, gloss, inner/cast shadows, icon position and text treatment before construction. `elements.json` records source boxes.
- `before/`, `after/`: 72 native Game View final-target readbacks: Start, HUD, Pause and Settings × normal/hover/pressed × 580×1250, 1080×1920, 2560×1080. No offscreen-only UI fixture is used. `native-path.txt` records each capture with `panel.targetTexture == null`, MSAA 1 and Play false. Raw full screens are unmodified; Unity's Edit-mode overlay readback is vertically inverted. **Only detail crops are vertically corrected.**
- `sheets-after/`: full-screen before/after pairs for all four screens at all three sizes; per-element reference | before | after sheets in each state/size and at 4×. `equal-*` sheets preserve aspect and fit all three elements into the native after-element box for equal-size inspection; the other sheets retain actual size changes. `inspection.jpg` summarizes the portrait comparison. The clock sheet explicitly compares material family, because no clock exists in the recording.
- `candidate/` and `sheets-candidate/`: first inspection run, retained honestly. It exposed an oversized controller-driven coin label and undersized corner scale. The final run uses the visual wrapper and larger corner scale.
- `import-audit.tsv`, `verification.json`, `validation.txt`: all 24 imports passed; all 72 requested captures are nonblank/native size; all controller element names are preserved; 12 native CTA press-depth checks passed (5 logical pixels, scaled by PanelSettings), with visible hover changes. Fifty warmed Settings/Pause/confirmation open-close cycles retained **231→231 elements, 339→339 callbacks, 0→0 active tweens**. Native Restart/Cancel bindings passed. This measures retained UI objects/callbacks/tweens, not a claim about managed heap size or mobile GPU performance.

Remaining reference differences are deliberate adaptations or drawing limits, not a claim of pixel parity:

| Element | Remaining difference / reason |
|---|---|
| UI-01 | Live Level 1 is captured rather than reference 29; Fredoka glyph proportions and thin lower grey bevel differ slightly. Only the purple reference variant is implemented. |
| UI-02 | Constructed teeth and hub are slightly more angular; the purple variant is also used on Start, where the recording uses blue. |
| UI-28 | The navy body is darker than the menu's blue-translucent reference. Timer has an overlapping clock absent from the reference; its countdown plate supplies the surface reference. |
| UI-29 | Heart lobe shape and layered soft highlight approximate the reference's smoother painted highlight. Live count is 5 rather than reference 0. |
| UI-30 | Front-view bus emblem is reconstructed with simpler bevels; rim reflection and inset disk remain less softly painted. Saved coin values are preserved, so captured numbers differ. |
| UI-31 | Plus badge bevel is slightly broader; it has no purchase action. |
| Clock | New family adaptation, not an observed reference asset. Gold rim, cream dial and red hands share the coin/heart shading language. |
| UI-27 | Cream X is slimmer and its lower bevel simpler than the reference. |
| UI-32–34 | Rounded silhouettes, double gold bezels, orange bases and palette are matched; upper white glint, lower band and font weight are approximate. Existing Restart/Main Menu/Reset Progress labels are retained, without reference ad/purchase icons. |
| UI-75 | Separate purple/lilac frame and dark bottom are present. Upper glint is brighter and lower purple band thinner; existing Level-only caption remains, because no Very Hard binding exists. Its position follows the existing menu body around the protected logo/background. |
| States/screens | Hover/pressed are derived from the observed normal material, because the recording does not resolve those states. Existing press depth remains. Settings/Pause popup bodies are not shown in the source video and are retained; only their shared buttons/close controls are redesigned. |

Protected-byte verification covers 36 existing files, including Game.unity, ResultTimeline.asset, the listed materials/data/models, ArtSource, Otobus Materials, spriteatlases, logo and menu background: **0 changed**. Game.unity was never saved or staged. No Play, vault write or push was performed.

Implementation commits: `0674ce9` (HUD), `2ff5807` (CTA/close), `cd47db5` (Start button), `486e59a` (coin text).

Final run: native 1080×1080 square smoke captures of all four screens show no clipped required controls; all 12 relevant control bounds are inside the viewport (`square/`, `square-validation.json`). Temporary harness source is retained only in the external evidence root and removed from Assets. Unity 2022.3.19f1 finished compilation after cleanup; Console **0 entries**, after clearing the known MCP WebSocket reconnect warning and rechecking. Final state: Edit mode, no compilation, Game scene clean, native panel target null, temporary harness type absent (`final-unity-check.json`). The protected-byte check passed again after cleanup. Device interaction and runtime rendering remain untested under the explicit never-Play constraint.
