# UI Toolkit U2

Status: finalized. After Unity restarted, the post-helper-removal compilation completed successfully in Unity 2022.3.19f1. The Console contains zero errors or warnings, and Game.unity was reopened and its UI wiring verified. No implementation fixes were required.

Game now uses one UIDocument for every screen and the tutorial. Both legacy Canvas roots (including the inactive camera-background Canvas) are removed; the active world background is unchanged. LevelEditor keeps its existing uGUI interface, CanvasArtwork and SafeArea. No gameplay system, level, model, material, camera, lighting or post-processing settings change.

| Former uGUI controller | Toolkit replacement |
| --- | --- |
| UIManager | ToolkitGameUI state/event bindings and reusable NoLives deadline |
| CompletePanel / CoinCounter | ToolkitGameUI.Results, fixed confetti/sparkle/coin pools and a reusable DOTween timeline |
| FailPanel | fail-overlay, live LilitaOne title, reason/level, existing Retry API for both buttons |
| LivesDisplay / NoLivesPanel | no-lives-overlay, LivesSystem.Changed and GameManager.PlayBlockedNoLives |
| PauseMenu confirmations | Existing U1 pause-confirmation and Restart / QuitToMenu APIs |
| TapTutorial Canvas visuals | TapTutorial.Toolkit, bound to the same UIDocument; filtering and lesson completion logic retained |

The eleven unreferenced uGUI controllers and their metadata are removed. The tutorial component moves to GameUI Toolkit with its existing GameManager, GameplayController, BusStation and camera references. EventSystem remains for runtime UI input. PanelSettings and the two TextCore font assets from U1 remain shared, with Expand scaling at 1080 x 1920. The single theme uses existing art and source sprite borders; both result titles are real outlined/shadowed LilitaOne text.

## Presentation

| Time | Complete |
| --- | --- |
| 0.3 s | Fixed pool of 54 pieces bursts from both lower corners and rains from the top |
| 0.7 s | Content scales 0.6 -> 1.08 -> 1; rotating rays and eight twinkles behind/around the title |
| 1.4 s | Coin pile drops, shakes twice and flashes |
| 1.9 s | Fourteen pooled coins follow staggered curved paths; arrivals roll up the counter and punch its plate |
| 2.6 s | Continue fades/scales in, followed by a breathing pulse |

Any press during the sequence finishes it and consumes that pointer through release, preventing accidental Continue. Continue always completes the remaining reward before calling LoadNext. One credit latch saves the reward once, including skip/disable and repeated display paths. AudioManager retains its existing start/win/lose event sounds; Toolkit buttons request the same Button sound.

The hand presses to 0.85 at its fingertip with a glow ring and eight sparkle dots over 0.35 seconds. Retargeting holds the previous fingertip during the burst, then glides along a small arc for 0.5 seconds. Distances above 40% of full panel height fade through an invisible position change. Initial placement is immediate; there is no visible retarget teleport. The Level 1 sequence, rope neighbours, green-bus/tunnel gating and unsafe-target release keep their existing logic.

Callbacks are paired on enable/disable; elements and effect pools are cached per document. Timelines are reused and paused/rewound on disable, then killed on destruction. Frame-driven animation uses value types and cached references; counter strings change only on discrete arrivals/events using the shared StringBuilder. All animated scales keep Z at 1 to avoid Unity 2022 UI Toolkit warnings.

## Verification

Unity 2022.3.19f1, Edit mode only. No Play mode or builds were used. Runtime gameplay, audible playback and device touch behavior were not executed.

- Offscreen runtime-panel captures at 1080 x 1920, 1080 x 2400, 1080 x 1080 and 1920 x 1080: Complete, all three failure reasons, NoLives, both pause confirmations, rope/tunnel hints and the hand glide.
- Six DOTween frames at 0.30, 0.85, 1.55, 2.15, 2.55 and 2.95 seconds, stepped manually in Edit mode, plus a combined celebration strip.
- 281 assertions: control containment, centered modals within 92%, safe-area padding, fourteen-coin final total, Continue gating, failure content, NoLives reuse/passthrough/regeneration, confirmation cancel/close, hand press/arc/far fade, skip/reward latch and repeated enable/disable subscriptions. Review leaves saved coins unchanged.
- Scene inspection: one UIDocument, zero Canvas components, zero missing scripts, all ten controller object references assigned. Non-UI retained scene blocks match U1 exactly; there are no dangling local scene references. LevelEditor is unchanged.
- Finalization after restart: compilation and asset updates are idle, `EditorUtility.scriptCompilationFailed` is false, and the Console has zero errors/warnings after reopening Game.unity. The scene has one UIDocument with GamePanelSettings and GameUI.uxml assigned, two UI controllers with all ten object references assigned, zero Canvas components and zero missing scripts. No temporary review helper, metadata, capture folder or verification menu remains in the repository. The earlier 281 assertions were retained without rerunning the U2 work.

Captures and detailed check results live only in `C:/Users/Kullanici/Documents/BusJamCase_UITK_Review/U2/`. They use an offscreen UI panel with a flat clear color behind transparent gameplay areas; world objects and camera/layout listeners are not driven by the review resolutions. The interrupted draft remains in its original temporary directory.
