# UI Toolkit comparison report

Unity 2022.3.19f1, branch `ui-toolkit`. Game and LevelEditor now each use a runtime UIDocument, UXML and the shared `Theme.uss` / `GamePanelSettings`. Existing artwork, English text, colours, screen flow and gameplay APIs are retained. Complete/Fail titles are real outlined, shadowed LilitaOne text; Fredoka and LilitaOne have separate TextCore SDF assets.

| Former uGUI interface | Toolkit replacement / existing API |
| --- | --- |
| UIManager, StartPanel | ToolkitGameUI / StateChanged, PrepareLevel |
| HudPanel, ReadyPanel | hud, ready-overlay / LevelLoaded, TimeChanged |
| LivesDisplay, CoinCounter | Labels / LivesSystem.Changed, CoinsChanged |
| SettingsPopup, PauseMenu | Settings/reset/pause confirmations / ResetProgress, Restart, QuitToMenu |
| CompletePanel | ToolkitGameUI.Results / pooled celebration, one reward credit, LoadNext |
| FailPanel, NoLivesPanel | fail-overlay, no-lives-overlay / Retry, PlayBlockedNoLives |
| TapTutorial visuals | TapTutorial.Toolkit / existing guidance and input filter |
| LevelEditor UI and popups | LevelEditorPanel.Toolkit / existing draft, save, solver and editing APIs |
| SafeArea, CanvasArtwork | Root padding and USS backgrounds |

## Layout and verification

One PanelSettings uses Scale With Screen Size, reference 1080x1920, Expand, sorting order 10. Flexbox lays out content; overlays and effects use absolute positioning. The existing LayoutModeController event supplies normalized safe-area padding and classes: portrait below 0.8, square from 0.8 through 1.25, landscape above 1.25, using the safe viewport aspect. Top bars remain at the safe top edge. Modals stay centered and bounded to 92% of the safe viewport. ScrollViews contain editor overflow.

U4 checked Start, HUD, Settings, Complete end state, Fail, NoLives and LevelEditor at **1080x1920, 1080x2400, 1170x2532, 1536x2048, 1080x1080, 1768x2208, 2208x1768, 1920x1080 and 2560x1080**. Ready, reset/pause confirmations and both editor popups were also checked. Each viewport was repeated with asymmetric simulated safe insets (3.5% left, 4.5% right, 5% top, 4% bottom).

234 offscreen runtime-panel captures; checks covered control containment, unwrapped text width, sibling overlap, modal centering and the 92% limit. Final checks found zero violations. Fixed portrait editor fields refusing to shrink, which pushed Auto Grid beyond the safe right edge. Captures show UI over a flat clear colour, without rendering or changing the world.

Evidence is outside the repository: `C:/Users/Kullanici/Documents/BusJamCase_UITK_Review/U4/`. `ContactSheet.png` and `ContactSheet-safe.png` contain the requested 63-view matrix each; individual images include supplemental states.

## Leak check

Temporary Edit-mode harness: ten warm-up cycles, then each game/editor screen opened and closed 50 times, including controller disable/enable. Complete ran 20 full 181-step timelines, checking Continue gating and the reward latch. Tutorial lifecycle methods were dispatched manually 50 times in Edit mode.

| Warmed measurement | Game | LevelEditor |
| --- | ---: | ---: |
| GC.GetTotalMemory(true), before / after | 574271488 / 574271488 B | 574861312 / 574861312 B |
| Managed delta | 0 B | 0 B |
| Registered callbacks, before / after | 306 / 306 | 703 / 703 |
| Callbacks while disabled | 286 | 628 |
| Visual elements, before / after | 174 / 174 | 249 / 249 |
| Layout subscriptions, enabled / disabled | 1 / 0 | 1 / 0 |

Callback totals include native Toolkit callbacks and Button.clicked delegates. Tutorial's five event subscriptions stayed at one each, returning to zero on disable. Element/tween counts did not grow. Earlier editor samples showed +4–12 KB while caches settled; these are editor-wide managed heap snapshots, not a runtime allocation profile or proof of zero allocation. No accumulating UI registrations were found. The temporary harness and its metadata were removed.

## Comparison and remaining limits

USS and Flexbox make shared styling, aspect changes and real text easier to maintain. Inspector wiring is replaced by named queries; panel picking, pointer capture, native dropdown styling and coordinate conversion make world-linked UI and animation more involved than uGUI.

UI Toolkit still renders the **same raster sprites**. Sharpness comes from source resolution/import settings and real SDF text, not from switching UI frameworks. Nine-slice values retain the existing artwork borders.

Removed the unused UIArt reference asset/class and the obsolete uGUI button-sound scan. World TMP labels on slots/tunnels remain required; EventSystem and its input module remain required for Toolkit. Gameplay, level assets, 3D layout, lighting, post-processing and materials are unchanged by U4.

Open issues: runtime gameplay, saved reward/life effects, audible playback, device touch/drag and world/UI composition were not exercised under the Edit-mode-only restriction. Wide-screen sizing and the overall visual comparison remain Asen's design decision. No Play mode or builds were used. Final script compilation passed; Console: zero entries.
