# ZM3 — Lives system

2026-11-19 · Unity **2022.3.19f1** · Edit mode only. Play mode was never entered through MCP. No push.

## Result and evidence

- **62 ZM3 Edit checks passed**, plus **77 ZM2b regression checks**. Compile finished; Console clean.
- Verified before implementation: saved level index **49**, permanent database **10 levels**, no `_Test` directory or `Test_CloakStuck` entry. No level/database asset was changed.
- Five lives maximum; production scene regeneration interval **20 minutes**. Persistent offline recovery, resume refresh, one-second event updates, zero-life start gating, Fail/Restart accounting and all serialized UI references are implemented.
- Allocation measurements: **0 B for 1,000 unchanged-second polls**; **0 B for 1,000 warmed countdown updates on each of the three displays**. Initialization, persistence and initial TMP capacity growth are outside those measurements.
- HUD, Start and NoLives screenshots were rendered from cloned scene canvases by Unity in an Edit-mode preview scene. HUD and NoLives were also checked at **1242 × 2688**. These captures show the actual serialized UI and existing background, with demonstration values **3 / 12:34** or **0 / 12:34**. Gameplay objects are not spawned and these are not Play-test evidence.

| Capture | Evidence |
| --- | --- |
| HUD hearts and Restart, 1080 × 1920 | [HUD](ZM3-Captures/HudPanel-1080x1920.png) |
| Start hearts, 1080 × 1920 | [Start](ZM3-Captures/StartPanel-1080x1920.png) |
| No lives, 1080 × 1920 | [No lives](ZM3-Captures/NoLivesPanel-1080x1920.png) |
| HUD, tall portrait | [1242 × 2688 HUD](ZM3-Captures/HudPanel-1242x2688.png) |
| No lives, tall portrait | [1242 × 2688 modal](ZM3-Captures/NoLivesPanel-1242x2688.png) |

## Changed files

| File under `Assets/_BusJam/`, unless noted | Change and reason |
| --- | --- |
| `Scripts/Core/LivesSystem.cs` + meta | Serialized cap/interval, UTC tick arithmetic, PlayerPrefs persistence and rollback protection. `InvokeRepeating` avoids an idle Update and publishes only changed counts/seconds. |
| `Scripts/Core/GameManager.cs` | Inspector reference to LivesSystem; guarded Fail consumes once, Restart consumes then retries, StartPlaying refreshes and rejects zero lives. Retry itself never consumes a life. |
| `Scripts/UI/LivesDisplay.cs` + meta | Shared display for HUD, Start and NoLives. Reused character buffers with TMP `SetCharArray` avoid recurring string formatting allocations. |
| `Scripts/UI/UIManager.cs` | Uses state and life events to select StartPanel or NoLivesPanel. The Fail panel remains visible after a Fail; its Retry opens NoLives when the count is zero. Recovery returns automatically to Start. |
| `Scripts/UI/HudPanel.cs` | Restart Button listener calls GameManager.Restart; HUD is enabled only while Playing. |
| `Scenes/Game.unity` | LivesSystem child, counters, Restart, NoLivesPanel, layout and all Inspector references. Existing SafeArea and 1080 × 1920 CanvasScaler / match 0.5 are retained. |
| `Scripts/Editor/ZM3Setup.cs` + meta | Repeatable Edit-mode setup and screenshot renderer, manual test menu and production restore. No runtime UI construction or art lookup. |
| `Scripts/Editor/ZM3Verification.cs` + meta | Disposable preview-scene tests; restores all touched PlayerPrefs in `finally`. Tests call the production methods with controlled UTC values. |
| `Scripts/Editor/ZM2bVerification.cs` | Updated the old Fail fixture for the required LivesSystem reference; restores life preferences after regression checks. |
| `Docs/MIMARI.md` | Lives rule in §3 and LivesSystem row in §5, attributed to Asen's 19 November rule. |
| `Docs/ZM3-Report.md`, `Docs/ZM3-Captures/` | Evidence and manual acceptance steps. |

## Persistence and clock behavior

`Lives` is an int. `LastRegenUtc` is an invariant-culture UTC tick string. Missing/malformed timestamps fall back to current UTC and the count is clamped. A partial regeneration interval survives additional losses; time at full capacity is not banked. Offline gains are capped at five.

An additional tick string, **`LivesUtcHighWater`**, persists the last observed UTC boundary. When time moves backward, no life is awarded and LastRegenUtc is reset to the new current time. Regeneration waits until the prior boundary has been reached; advancing back to that boundary grants no rebound lives and starts a fresh interval. The displayed countdown stays bounded to one interval while guarded. The guard survives restart, pause and quit. This prevents the specified backward/forward exploit but cannot authenticate arbitrary forward-only clock changes; a real game uses server time.

## Saved hierarchy and bindings

```text
Systems                         GameManager.livesSystem -> child LivesSystem
  LivesSystem                   LivesSystem (maxLives 5, regenMinutes 20)
Canvas (1080 × 1920, match 0.5)
  SafeArea                      UIManager.livesSystem / noLivesPanel assigned
    StartPanel
      LivesCounter              LivesDisplay -> LivesSystem + count/time/group
        panel_header            fallback plate
        icon_heart / LivesCount
        Countdown
          panel_header / icon_clock / CountdownText
    HudPanel                    HudPanel.restartButton -> btn_round_blue
      LevelText / HudTimeText
      LivesCounter              same bindings as Start
      btn_round_blue            Button -> GameManager.Restart at Awake
        icon_restart / RestartLabel
    NoLivesPanel                blocking dim Image + LivesDisplay
      panel_modal
        panel_header / Title    "No lives"
        icon_heart / LivesCount
        Countdown
          icon_clock / CountdownText  "Next life in mm:ss"
        Hint                    "Lives refill automatically."
```

Sprites come from the assigned references in `UI/UIArt.asset`: `icon_heart`, `panel_header`, `btn_round_blue`, `icon_restart`, `panel_modal`, `icon_clock`. `counter_plate` is absent, so `panel_header` is the authorized fallback. All new sprite Image object names match the art names; the untextured modal blocker is named NoLivesPanel. The existing Fail Retry button is preserved. Fredoka was not created by ZUI, so the current **LiberationSans SDF** font is used. No font download or unrelated UI package was attempted.

## Edit tests

The 62 checks cover fresh install, first loss, exact interval boundary, partial intervals, offline gains/cap, no banked full-cap time, no underflow, malformed preferences, resume, changed-second events, rollback/rebound/restart guard, five Fail/Retry cycles, duplicate Fail, zero-life start rejection, start without charge, Restart/state guard, zero-life UI, recovery events, scene bindings, mm:ss rendering, full-life countdown hiding and allocation measurements. The 77 prior checks verify solver/gameplay/NoMoves behavior after adding the required life fixture.

Re-run in Edit mode through MCP `execute_code` using reflection on `Assembly-CSharp-Editor` and `BusJam.Editor.ZM3Verification.Run()` / `BusJam.Editor.ZM2bVerification.Run()`; the executor does not directly reference the editor assembly. Tests leave saved preferences unchanged. Run the ZM3 suite with the production interval **20**, before preparing the temporary test.

The first script-only refresh compiled the changed callers before importing new scripts; a full asset refresh imported them and compiled cleanly. The first regression attempt exposed the old fixture's missing life reference; the fixture was updated and the suite passed. Reflection returned this test exception; MCP transport remained available. Refresh/domain reload requests recovered internally and returned success; no repeated connection failure occurred.

## Manual Play acceptance — prepared after the production commit

The final uncommitted setup is **regenMinutes 1**, PlayerPrefs **Lives 1**, fresh UTC anchors, **LevelIndex 0 / Level_01**, a six-passenger level with a **30-second** timer. Game.unity is saved, Play is off. Only the scene interval differs from the committed production scene. PlayerPrefs changes are local and do not appear in Git. Do not commit this setup.

1. Enter Play manually and tap to start. Wait for the 30-second timeout, then click **Retry**. The loss consumes the only life; Retry consumes none. **No lives** replaces Start, taps cannot start a run, and the countdown advances once per second. At the first one-minute boundary measured from the seeded/loss regeneration anchor, one life appears and Start returns; tap to begin.
2. Stop Play and use **Bus Jam > ZM3 > Prepare One-Life Test (Edit)**. Enter Play, start, and press **Restart**. It consumes the last life and immediately opens NoLives after Retry. Wait for recovery and start normally.
3. For a rapid five-Fail sweep, stop Play, use **Reset To Five Lives (Edit)**, then enter Play. Start each run and choose **Bus Jam > ZM3 > Fail Current Run (Play)**; click Retry. Complete all five cycles within one minute to avoid legitimate regeneration. Counts must be 4, 3, 2, 1, 0; the fifth Retry opens NoLives. The menu calls the real GameManager.Fail method. It does not enter Play.
4. At zero lives with a fresh one-minute interval, close Unity and wait two minutes, then reopen with **2022.3.19f1** and enter Play manually. Expect exactly **two** recovered lives if reopening before a third full interval has elapsed (cap five). This checks real application exit/reopen; it was simulated deterministically in Edit tests, not performed here.
5. With fewer than five lives, move Windows UTC back one hour while playing. Expect no award, a bounded countdown, and no exceptions. Restore the correct clock: no rebound award; recovery starts a new one-minute interval. Return Windows time to automatic/correct time when finished. The implementation was tested with injected timestamps; this task did not change Windows time.

**Revert:** Stop Play and choose **Bus Jam > ZM3 > Restore Production (Edit)**. It restores scene regeneration to **20**, saves Game.unity, restores level index **49**, and removes only `Lives`, `LastRegenUtc`, `LivesUtcHighWater` so the next run defaults to five. It clears any rollback guard from the manual clock test. Check the Game.unity diff is empty; retain unrelated material/ArtSource work.

## Boundaries

`M_Purple.mat`, `M_Yellow.mat` and `ArtSource/` were preserved and excluded from the commit. No database changes, Play entry, system-clock changes or push. Real-time Play interaction, actual application restart and OS-clock acceptance remain for Asen using the prepared setup above.
