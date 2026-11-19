# ZFIX - UI art fixes

## Before-art-edit git diff summary

- Initial Game.unity diff reported 1,031 changed lines. M_Purple and M_Yellow also have existing modifications; ArtSource and the menu logo PNG/meta are untracked.
- Added GameObjects: icon_coin, UpUI, LevelCompleted, MoneyCounter, MoneyText, LevelCompleted, Logo.
- Existing Image sprite assignments, TMP text/color/font sizing, hierarchy and local TMP material changes are preserved.
- Restore Production (Edit) ran first. Its code changes only regeneration and PlayerPrefs; the saved scene now has regeneration 20 minutes. The git diff still contains the layout changes listed below. Unity also normalized serialization on save.
- Full serialized scene block baseline captured after production restore and before art changes for final preservation verification.
- M_Purple, M_Yellow and ArtSource excluded. No AsenOS access. Unity 2022.3.19f1, Edit mode only.

| Object | Asen layout changes still present after restore |
| --- | --- |
| `Canvas/SafeArea/StartPanel/UpUI/MoneyCounter/icon_coin` | m_Father: (new) -> {fileID: 908028202}; m_AnchorMin: (new) -> {x: 0, y: 0.5}; m_AnchorMax: (new) -> {x: 0, y: 0.5}; m_AnchoredPosition: (new) -> {x: 2, y: -0.0000038146973}; m_SizeDelta: (new) -> {x: 126.5441, y: 190.8557}; m_Pivot: (new) -> {x: 0.5, y: 0.5}; m_LocalScale: (new) -> {x: 1, y: 1, z: 1} |
| `Canvas/SafeArea/NoLivesPanel/panel_modal/Countdown/icon_clock` | m_AnchoredPosition: {x: -244, y: 0} -> {x: -183, y: 0} |
| `Canvas/SafeArea/StartPanel/UpUI` | m_Father: (new) -> {fileID: 386287758}; m_AnchorMin: (new) -> {x: 0, y: 1}; m_AnchorMax: (new) -> {x: 1, y: 1}; m_AnchoredPosition: (new) -> {x: 0, y: -103}; m_SizeDelta: (new) -> {x: 0, y: 116}; m_Pivot: (new) -> {x: 0.5, y: 0.5}; m_LocalScale: (new) -> {x: 1, y: 1, z: 1} |
| `Canvas/SafeArea/HudPanel/TimerCounter` | m_AnchorMin: {x: 1, y: 1} -> {x: 0.5, y: 1}; m_AnchorMax: {x: 1, y: 1} -> {x: 0.5, y: 1}; m_AnchoredPosition: {x: -190, y: -110} -> {x: 0, y: -58} |
| `Canvas/SafeArea` | m_AnchoredPosition: {x: 0, y: 0} -> {x: 0, y: -0.9346924}; m_SizeDelta: {x: 0, y: 0} -> {x: 0, y: -307.991} |
| `Canvas/SafeArea/StartPanel/bg_menu` | m_AnchoredPosition: {x: 0, y: 0} -> {x: 0, y: 0.9346924} |
| `Canvas/SafeArea/CompletePanel/NextButton` | m_AnchorMin: {x: 0.5, y: 0.5} -> {x: 0.5, y: 0.22}; m_AnchorMax: {x: 0.5, y: 0.5} -> {x: 0.5, y: 0.22}; m_SizeDelta: {x: 360, y: 110} -> {x: 594, y: 190} |
| `Canvas/SafeArea/FailPanel/LevelCompleted` | m_Father: (new) -> {fileID: 1165738346}; m_AnchorMin: (new) -> {x: 0.5, y: 0.5}; m_AnchorMax: (new) -> {x: 0.5, y: 0.5}; m_AnchoredPosition: (new) -> {x: 0, y: 391}; m_SizeDelta: (new) -> {x: 790, y: 474.1223}; m_Pivot: (new) -> {x: 0.5, y: 0.5}; m_LocalScale: (new) -> {x: 1, y: 1, z: 1} |
| `Canvas/SafeArea/HudPanel/btn_round_blue` | m_AnchoredPosition: {x: -102, y: 160} -> {x: -117, y: 70} |
| `Canvas/SafeArea/StartPanel/UpUI/MoneyCounter` | m_Father: (new) -> {fileID: 186164104}; m_AnchorMin: (new) -> {x: 0, y: 0}; m_AnchorMax: (new) -> {x: 0, y: 0}; m_AnchoredPosition: (new) -> {x: 0, y: 0}; m_SizeDelta: (new) -> {x: 225.4093, y: 116}; m_Pivot: (new) -> {x: 0.5, y: 0.5}; m_LocalScale: (new) -> {x: 1, y: 1, z: 1} |
| `Canvas/SafeArea/StartPanel/UpUI/LivesCounter` | m_Father: {fileID: 386287758} -> {fileID: 186164104}; m_AnchorMin: {x: 0.5, y: 1} -> {x: 0, y: 0}; m_AnchorMax: {x: 0.5, y: 1} -> {x: 0, y: 0}; m_AnchoredPosition: {x: 0, y: -106} -> {x: 0, y: 0}; m_SizeDelta: {x: 440, y: 116} -> {x: 225.4093, y: 116} |
| `Canvas/SafeArea/NoLivesPanel/panel_modal/Countdown/CountdownText` | m_AnchorMin: {x: 0.5, y: 0.5} -> {x: 1, y: 0.5}; m_AnchorMax: {x: 0.5, y: 0.5} -> {x: 1, y: 0.5}; m_AnchoredPosition: {x: 90, y: 0} -> {x: -99, y: 0} |
| `Canvas/SafeArea/StartPanel/UpUI/MoneyCounter/MoneyText` | m_Father: (new) -> {fileID: 908028202}; m_AnchorMin: (new) -> {x: 1, y: 0.5}; m_AnchorMax: (new) -> {x: 1, y: 0.5}; m_AnchoredPosition: (new) -> {x: -99, y: 0}; m_SizeDelta: (new) -> {x: 198, y: 80}; m_Pivot: (new) -> {x: 0.5, y: 0.5}; m_LocalScale: (new) -> {x: 1, y: 1, z: 1} |
| `Canvas/SafeArea/HudPanel/TimerCounter/icon_clock` | m_AnchoredPosition: {x: -111, y: 0} -> {x: -124.2, y: 17.4}; m_SizeDelta: {x: 108, y: 116} -> {x: 130, y: 185} |
| `Canvas/SafeArea/CompletePanel/LevelCompleted` | m_Father: (new) -> {fileID: 458848231}; m_AnchorMin: (new) -> {x: 0.5, y: 0.5}; m_AnchorMax: (new) -> {x: 0.5, y: 0.5}; m_AnchoredPosition: (new) -> {x: 0, y: 391}; m_SizeDelta: (new) -> {x: 790, y: 474.1223}; m_Pivot: (new) -> {x: 0.5, y: 0.5}; m_LocalScale: (new) -> {x: 1, y: 1, z: 1} |
| `Canvas/SafeArea/NoLivesPanel` | m_AnchoredPosition: {x: 0, y: 0} -> {x: 0, y: -34.892883}; m_SizeDelta: {x: 0, y: 0} -> {x: 0, y: 395.546} |
| `Canvas/SafeArea/HudPanel/TimerCounter/TimerText` | m_AnchoredPosition: {x: 44, y: 0} -> {x: 24.4, y: 0} |
| `Canvas/SafeArea/FailPanel/RetryButton` | m_AnchorMin: {x: 0.5, y: 0.5} -> {x: 0.5, y: 0.22}; m_AnchorMax: {x: 0.5, y: 0.5} -> {x: 0.5, y: 0.22}; m_SizeDelta: {x: 360, y: 110} -> {x: 594, y: 190} |
| `Canvas/SafeArea/StartPanel/Logo` | m_Father: (new) -> {fileID: 386287758}; m_AnchorMin: (new) -> {x: 0.5, y: 0.5}; m_AnchorMax: (new) -> {x: 0.5, y: 0.5}; m_AnchoredPosition: (new) -> {x: 0, y: 309}; m_SizeDelta: (new) -> {x: 1000, y: 550}; m_Pivot: (new) -> {x: 0.5, y: 0.5}; m_LocalScale: (new) -> {x: 1, y: 1, z: 1} |
| `Canvas/SafeArea/HudPanel/LevelBadge/LevelNumber` | m_AnchoredPosition: {x: 0, y: -26} -> {x: 0, y: -11.8} |
| `Canvas/SafeArea/NoLivesPanel/panel_modal` | m_AnchoredPosition: {x: 0, y: 0} -> {x: 0, y: 5} |
| `Canvas/SafeArea/HudPanel/LevelBadge` | m_AnchoredPosition: {x: 145, y: -142} -> {x: 125.9, y: -79.2061}; m_SizeDelta: {x: 220, y: 220} -> {x: 174.6642, y: 158.4122} |
| `Canvas/SafeArea/HudPanel/LevelBadge/LevelTitle` | m_AnchoredPosition: {x: 0, y: 50} -> {x: 0, y: 36.8} |
| `Canvas/SafeArea/StartPanel/StartButton` | m_AnchoredPosition: {x: 0, y: 0} -> {x: 0, y: -45} |
| `Canvas/SafeArea/NoLivesPanel/panel_modal/LivesCount` | m_Father: {fileID: 1047558538} -> {fileID: 2146875437}; m_AnchoredPosition: {x: -66, y: 0} -> {x: 0, y: 0} |
| `Canvas/SafeArea/NoLivesPanel/panel_modal/icon_heart` | m_AnchorMin: {x: 0.5, y: 0.5} -> {x: 0, y: 0.5}; m_AnchorMax: {x: 0.5, y: 0.5} -> {x: 0, y: 0.5}; m_AnchoredPosition: {x: -168, y: 0} -> {x: 6, y: -0.0000038146973}; m_SizeDelta: {x: 114, y: 106} -> {x: 126.5441, y: 190.8557} |

## Element fixes

Image layout is unchanged. Multiplier formula: sprite height * Canvas reference PPU / (sprite PPU * rect height). This maps source height to the existing element height, keeps both round caps intact, and stretches only the slice center. Existing sprite borders already isolate the bezel; no import-border edits were needed.

| Element | Cause | Fix / verified setting |
| --- | --- | --- |
| `Canvas/SafeArea/HudPanel/btn_round_blue` | Already Simple + Preserve Aspect; no clipping cause found | Simple / 1 / aspect True (unchanged) |
| `Canvas/SafeArea/HudPanel/btn_round_blue/icon_restart` | Already Simple + Preserve Aspect; no clipping cause found | Simple / 1 / aspect True (unchanged) |
| `Canvas/SafeArea/HudPanel/LevelBadge` | Already Simple + Preserve Aspect; no clipping cause found | Simple / 1 / aspect True (unchanged) |
| `Canvas/SafeArea/HudPanel/TimerCounter` | Already safe at 6.25; slight vertical scaling mismatch | Sliced / 6.060176 / aspect False |
| `Canvas/SafeArea/HudPanel/TimerCounter/icon_clock` | Already Simple + Preserve Aspect; no clipping cause found | Simple / 1 / aspect True (unchanged) |
| `Canvas/SafeArea/CompletePanel/LevelCompleted` | Simple stretched to an aspect ratio different from the sprite | Simple / 1 / aspect True |
| `Canvas/SafeArea/CompletePanel/NextButton` | Sliced multiplier 1: vertical caps total 650.2 units in a 190-unit rect | Sliced / 3.785475 / aspect False |
| `Canvas/SafeArea/FailPanel/LevelCompleted` | Simple stretched to an aspect ratio different from the sprite | Simple / 1 / aspect True |
| `Canvas/SafeArea/FailPanel/RetryButton` | Sliced multiplier 1: vertical caps total 650 units in a 190-unit rect | Sliced / 3.826316 / aspect False |
| `Canvas/SafeArea/NoLivesPanel/panel_modal` | Sliced multiplier 1: native height 1410.8 units squeezed into 740 | Sliced / 1.906472 / aspect False |
| `Canvas/SafeArea/NoLivesPanel/panel_modal/panel_header` | Sliced multiplier 1: vertical borders total 510.7 units in a 144-unit rect | Sliced / 4.881809 / aspect False |
| `Canvas/SafeArea/NoLivesPanel/panel_modal/icon_heart` | Already Simple + Preserve Aspect; no clipping cause found | Simple / 1 / aspect True (unchanged) |
| `Canvas/SafeArea/NoLivesPanel/panel_modal/Countdown/icon_clock` | Already Simple + Preserve Aspect; no clipping cause found | Simple / 1 / aspect True (unchanged) |

- StartPanel artwork, including the Level button recipe, is unchanged. Its button is Simple + Preserve Aspect.
- All inspected UIArt sprites use full texture import rectangles; no trimmed sprite rects, Filled Images, or UIArt parent masks were found.
- LevelEditor has no UIArt Images. Its built-in sprites are audited below and left unchanged.
- Ribbon, coin pile, yellow button, tip and extra plates are available in UIArt but have no scene/prefab Image consumers; no layout objects were added.

## Tunnel label

`Tunnel/Badge/WhiteBackground` is a Quad/MeshRenderer, not an Image. The existing object was disabled. `Tunnel/Badge/RemainingText` now uses Fredoka Bold, white, a dedicated M_TunnelCount material with dark outline width 0.3 and a small underlay (offset +0.15/-0.15, dilate 0.04, softness 0.08). Every tunnel Transform/RectTransform and the existing camera-facing logic are unchanged.

## Missing visual asset audit (list only)

All proposed assets below are transparent PNGs. Priorities are art-production suggestions. Ordinary dynamic text and intentional solid-color input hit areas are not missing artwork. FBX buses, passengers and BoardKit models already exist.

| Object path | Current visual | Proposed asset (pixels) | Priority |
| --- | --- | --- |
| `Passenger.prefab/Passenger/VisualRoot/AngryIcon` | Flat red primitive Quad, not an angry face | icon_angry_face (256x256) | must |
| `Passenger.prefab/Passenger/VisualRoot/HiddenMark` | LiberationSans TMP question mark | icon_hidden_question (256x256) | must |
| `Passenger.prefab/Passenger/VisualRoot/Rope_1 ... Rope_4` | Four smooth primitive cylinder bands | rope_wrap_1 ... rope_wrap_4 (512x512 each) | must |
| `Passenger.prefab/Passenger/VisualRoot/Cloak/{Mantle,Hood,Collar,HoodOpening,GoldHem}` | Capsule, spheres and cylinders form the cloak | cloak_closed_overlay, cloak_open_overlay (512x768 each) | nice |
| `SlotTile.prefab/SlotTile/Kit_SlotLocked/UnlockLevel` | TMP LVL 50 / LVL 100 on the modeled locked slot; no separate art plate | slot_unlock_plate (512x256; leave number dynamic) | must |
| `SlotTile.prefab/SlotTile/Kit_SlotLocked` | Existing FBX locked-slot mesh; no separate 2D lock icon | icon_slot_lock (256x256) | nice |
| `LockIcon.prefab/LockIcon` | Legacy primitive Quad; WaitingArea lockIconPrefab is null, so unused | icon_slot_lock (256x256; reuse) | nice |
| `Game.unity/Canvas/SafeArea (no tutorial-hand object)` | Not implemented; no current glyph/primitive | tutorial_hand (256x320) | nice |
| `Game.unity/Canvas/SafeArea (no settings-gear object)` | Not implemented; no current glyph/primitive | icon_settings_gear (256x256) | nice |
| `Bus.prefab/Bus (no boarding-sparkle object)` | No boarding effect is wired; fx_sparkle.png already exists in UIArt | fx_sparkle (existing 512x435; reuse, no new asset) | nice |
| `Game.unity/Canvas/SafeArea/CompletePanel (no confetti emitter)` | No confetti emitter wired; confetti PNGs already exist | fx_confetti_rect, fx_confetti_star (existing 256x256 each; reuse) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls` | Flat solid rectangle control panel | editor_controls_panel (1024x640) | nice |

### Exact Level Editor default-sprite inventory

These are Unity stock controls, not cropped UIArt consumers. Dropdown viewport masks are intentional scroll clipping. Every affected Image path is enumerated here.

| Object path | Current visual | Proposed asset (pixels) | Priority |
| --- | --- | --- |
| `LevelEditor.unity/Canvas/SafeArea/Controls/levelList` | Unity stock dropdown field | editor_dropdown_field (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/levelList/Arrow` | Unity stock dropdown arrow | icon_dropdown_arrow (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/levelList/Template` | Unity stock dropdown list panel | editor_dropdown_panel (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/levelList/Template/Viewport` | Unity stock mask sprite; intentional scroll clip | editor_list_viewport (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/levelList/Template/Viewport/Content/Item/Item Checkmark` | Unity stock checkmark | icon_checkmark (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/levelList/Template/Scrollbar` | Unity stock scrollbar track | editor_scroll_track (64x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/levelList/Template/Scrollbar/Sliding Area/Handle` | Unity stock scrollbar handle | editor_scroll_handle (64x128) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/YÃ¼kle` | Unity stock button | editor_button (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/Yeni` | Unity stock button | editor_button (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/Kaydet` | Unity stock button | editor_button (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/width` | Unity stock input field | editor_input_field (512x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/height` | Unity stock input field | editor_input_field (512x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/Boyutu uygula` | Unity stock button | editor_button (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/seconds` | Unity stock input field | editor_input_field (512x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/slots` | Unity stock input field | editor_input_field (512x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/brush` | Unity stock dropdown field | editor_dropdown_field (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/brush/Arrow` | Unity stock dropdown arrow | icon_dropdown_arrow (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/brush/Template` | Unity stock dropdown list panel | editor_dropdown_panel (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/brush/Template/Viewport` | Unity stock mask sprite; intentional scroll clip | editor_list_viewport (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/brush/Template/Viewport/Content/Item/Item Checkmark` | Unity stock checkmark | icon_checkmark (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/brush/Template/Scrollbar` | Unity stock scrollbar track | editor_scroll_track (64x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/brush/Template/Scrollbar/Sliding Area/Handle` | Unity stock scrollbar handle | editor_scroll_handle (64x128) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/color` | Unity stock dropdown field | editor_dropdown_field (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/color/Arrow` | Unity stock dropdown arrow | icon_dropdown_arrow (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/color/Template` | Unity stock dropdown list panel | editor_dropdown_panel (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/color/Template/Viewport` | Unity stock mask sprite; intentional scroll clip | editor_list_viewport (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/color/Template/Viewport/Content/Item/Item Checkmark` | Unity stock checkmark | icon_checkmark (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/color/Template/Scrollbar` | Unity stock scrollbar track | editor_scroll_track (64x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/color/Template/Scrollbar/Sliding Area/Handle` | Unity stock scrollbar handle | editor_scroll_handle (64x128) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/facing` | Unity stock dropdown field | editor_dropdown_field (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/facing/Arrow` | Unity stock dropdown arrow | icon_dropdown_arrow (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/facing/Template` | Unity stock dropdown list panel | editor_dropdown_panel (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/facing/Template/Viewport` | Unity stock mask sprite; intentional scroll clip | editor_list_viewport (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/facing/Template/Viewport/Content/Item/Item Checkmark` | Unity stock checkmark | icon_checkmark (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/facing/Template/Scrollbar` | Unity stock scrollbar track | editor_scroll_track (64x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/facing/Template/Scrollbar/Sliding Area/Handle` | Unity stock scrollbar handle | editor_scroll_handle (64x128) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/Toggle/Background` | Unity stock checkbox background | editor_checkbox (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/Toggle/Background/Checkmark` | Unity stock checkmark | icon_checkmark (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/buses` | Unity stock input field | editor_input_field (512x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/tunnelQueue` | Unity stock input field | editor_input_field (512x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/ropes` | Unity stock dropdown field | editor_dropdown_field (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/ropes/Arrow` | Unity stock dropdown arrow | icon_dropdown_arrow (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/ropes/Template` | Unity stock dropdown list panel | editor_dropdown_panel (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/ropes/Template/Viewport` | Unity stock mask sprite; intentional scroll clip | editor_list_viewport (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/ropes/Template/Viewport/Content/Item/Item Checkmark` | Unity stock checkmark | icon_checkmark (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/ropes/Template/Scrollbar` | Unity stock scrollbar track | editor_scroll_track (64x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/ropes/Template/Scrollbar/Sliding Area/Handle` | Unity stock scrollbar handle | editor_scroll_handle (64x128) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/cloak` | Unity stock dropdown field | editor_dropdown_field (256x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/cloak/Arrow` | Unity stock dropdown arrow | icon_dropdown_arrow (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/cloak/Template` | Unity stock dropdown list panel | editor_dropdown_panel (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/cloak/Template/Viewport` | Unity stock mask sprite; intentional scroll clip | editor_list_viewport (256x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/cloak/Template/Viewport/Content/Item/Item Checkmark` | Unity stock checkmark | icon_checkmark (64x64) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/cloak/Template/Scrollbar` | Unity stock scrollbar track | editor_scroll_track (64x256) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/cloak/Template/Scrollbar/Sliding Area/Handle` | Unity stock scrollbar handle | editor_scroll_handle (64x128) | nice |
| `LevelEditor.unity/Canvas/SafeArea/Controls/Ã‡Ã¶z` | Unity stock button | editor_button (256x64) | nice |

## Verification

- 45 Game.unity RectTransform blocks are byte-for-byte identical to the pre-art baseline; all 227 original serialized blocks remain. The seven added objects and their hierarchy are preserved.
- Scene changes after baseline are limited to seven Image component blocks. No runtime scripts were changed.
- Three Edit-mode captures: UI-art-gallery.png, HUD-1080x1920.png and Tunnel-label.png under Docs/ZFIX-Captures. Gallery and HUD show intact caps/bezel; tunnel close-up shows the outlined numeral without a white plate.
- Script compilation requested through Unity MCP. Refresh completed and recovered one transport reconnect automatically. Final Console/readiness result recorded below.
- Stored runtime exception found: FailPanel.OnEnable, reasonText is unassigned in Asen's scene (fileID 0). This existing issue is outside the permitted Image/tunnel edits; no Play test or reference fix was performed. Console was cleared before compilation.
- Two in-memory inspection/preview snippets initially failed compilation (TextureImporter API and private helper access); each succeeded on its single corrected retry. No project scripts were created.
- No AsenOS access, Play entry, push, M_Purple/M_Yellow/ArtSource edits, or extra screenshots.

- Final pre-commit verification: script compilation completed, editor ready, Play off, Game.unity saved, Console 0 entries (errors/warnings/logs).
- Production scene regeneration is 20 minutes for the commit. After committing, Prepare One-Life Test (Edit) will restore the uncommitted one-life test setup.
- git diff --check reports only Unity-generated blank YAML field trailing spaces in Asen-added scene objects; serialization was left intact to preserve his edits.
