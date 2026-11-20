# UI Toolkit U1

The Game scene uses one runtime UIDocument for Start, HUD, Ready and settings. The existing Canvas remains responsible for Complete, Fail, NoLives, reward coins and the tutorial. LevelEditor is unchanged.

| uGUI object / script | UXML element / controller |
| --- | --- |
| Canvas/SafeArea / SafeArea | safe-area / ToolkitGameUI (LayoutModeController.Changed and geometry changes) |
| StartPanel/bg_menu, Logo | menu-background, logo / shared theme USS |
| StartPanel/StartButton / StartPanel | start-button / ToolkitGameUI -> GameManager.PrepareLevel |
| StartPanel/UpUI/LivesCounter / LivesDisplay | lives-count, life-countdown / LivesSystem.Changed |
| StartPanel/UpUI/MoneyCounter / CoinCounter | coin-count / SaveSystem.CoinsChanged |
| StartPanel/SettingsButton / SettingsPopup | start-settings, settings-overlay, reset-confirmation / ToolkitGameUI |
| HudPanel/LevelBadge, TimerCounter / HudPanel | hud, level-number, timer-text / LevelLoaded, LevelTimer.TimeChanged |
| ReadyPanel/Prompt / ReadyPanel | ready-overlay, ready-prompt / StateChanged; existing screen input starts play |
| HudPanel/SettingsButton, SettingsPanel / PauseMenu | pause-settings, pause-overlay, sound-toggle, restart-button, menu-button / ToolkitGameUI |
| HudPanel/ConfirmPanel / PauseMenu | pause-confirmation, confirm-text, confirm-yes, confirm-no / existing Restart and QuitToMenu APIs |
| UIManager | Keeps legacy state selection and NoLives timing; skips migrated panels when toolkitScreens is enabled |

## Integration

- Legacy Start/HUD/Ready/Settings objects and their references are retained, inactive. SettingsPopup is disabled. UIManager's serialized migration switch prevents reactivation.
- The retained Canvas sorts above the UIDocument so the existing NoLives warning and remaining screens still overlay correctly. The UIDocument sorts above the world.
- PanelSettings uses Scale With Screen Size, 1080 x 1920 and Expand. The root receives normalized safe viewport padding in panel units. Aspect classes use the existing LayoutModeController event.
- The single theme uses existing source art and sprite border values. TextCore SDF assets are separate from the existing TMP assets. Future Complete/Fail title styling is supplied without migrating those screens in U1.
- Callbacks are paired in OnEnable/OnDisable. Elements are queried once per document lifetime. Idle/urgent tweens are reused; displayed numbers change only on events, using a reused StringBuilder.

## Verification

Edit mode only; no player builds or Play mode. Review captures and verification results are stored outside the repository under `C:/Users/Kullanici/Documents/BusJamCase_UITK_Review/U1/`.

- Unity 2022.3.19f1 compiled the controller and imported UXML/USS and both baked TextCore atlases.
- Final compilation is idle and Console contains 0 entries. Both persisted TextCore font assets contain 95 ASCII glyphs with valid atlas/material subassets.
- Captured eight UI states at each of 1080 x 1920, 1080 x 2400, 1080 x 1080 and 1920 x 1080. These are offscreen runtime-panel previews in Edit mode; transparent HUD areas do not include the world. Existing camera/layout listeners were not driven by the review viewport.
- Checked control containment, centred modals within 92% of the safe area, settings/reset cancel/close callbacks and both pause confirmation texts. Complete/Fail hide migrated screens for the legacy Canvas handoff.
- Simulated asymmetric screen insets and verified exact safe padding, Ready gear visibility, urgent timer, level/lives/coin formatting and five enable/disable cycles without duplicate layout subscriptions.
- Runtime gameplay and life spending were not executed: the requested Edit mode restriction remains in force. Existing GameManager APIs own those rules; the UI never modifies saved progress during review.
