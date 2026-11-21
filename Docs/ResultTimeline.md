# Result timeline

Select `Assets/_BusJam/Data/ResultTimeline.asset` in the Project window. Expand **Celebration** to manage the bus choreography; every control has an English tooltip. Expand a named step in the Inspector and edit **Enabled, Start, Duration, Ease, Overshoot** (OutBack only) or **Stagger** (confetti/coin flight). Times are unscaled seconds. Change Start to change appearance order; dragging list entries only organizes the Inspector. Keep one entry per name. For CelebrationBus, only Enabled is authored in the named step; Start and Duration are derived from Celebration.

Open **Bus Jam > Result Timeline Preview**. **Play Complete** and **Play Fail** animate in Edit mode; **Pause** and the **Time** slider allow scrubbing. **Coin Flight** previews Continue's collection; **Retry Fade** previews Retry. **Edit Timings** selects the asset. Choose 580×1250, 1080×1920 or 2560×1080. The preview uses a disposable scene and never saves Game.unity or player progress.

Tracks and named steps:

| Track | Steps |
| --- | --- |
| Complete | Confetti, Overlay, CelebrationBus, Title, Rays, CoinPile, CoinCount, ContinueButton, CoinPlate, RewardFlash, IntroHold |
| Collection (on Continue) | CoinFlight, CollectionGlow, CollectionPulse, CoinCounterPunch, CollectionFade |
| Fail | BoardHold, FailOverlay, Panel, LifeLoss |
| Retry (on Retry) | RetryFade |

Lengths are derived from enabled steps, including particle/coin stagger. Defaults preserve Complete **3.45 s**, Collection **1.8 s**, Fail **1.3 s**, Retry **0.4 s**. IntroHold retains the original final 0.05-second settle. BoardHold sets the earliest allowed Fail entry; later steps keep their explicit Start values. CollectionGlow and the fade steps use Start for the beginning of their fade; CoinCounterPunch's Start is relative to each coin arrival.

Asset edits apply on the next runtime frame, preserving elapsed seconds and resuming a lengthened active result. The asset is already referenced by the shared CelebrationSettings; existing scenes need no rebinding. Runtime sampling uses cached lookups, pooled effects and reusable clocks. Disabling CoinFlight suppresses the effect; the reward still settles once when collection ends. Disabled presentation steps do not alter gameplay life/reward rules.

Validation: 650 Edit-mode assertions cover default poses, custom ordering, disabled steps, zero duration, BoardHold and live retiming. Fifty result cycles and twelve preview lifecycle cycles showed zero retained growth; 200 warmed result-sampler frames allocated 0 bytes. Play was not entered. Evidence: `BusJamCase_ZVEC_Review/aa/timeline-validation.txt`, `raster-validation.txt`, `preview-lifecycle.txt` and `timeline-*.png`.


The saved celebration keeps Asen's `db881ae` tuning: entry starts at **1.05 s**, takes **0.55 s**, stops at normalized **(0.5, 0.2)** until **2.25 s** (a **0.65 s** hold), then exits over **0.65 s**, hiding at **2.90 s**. Other result steps, including the **3.45 s** Complete track, are unchanged. Camera yaw/pitch/distance/FOV remain **0 / 10 / 18 / 35**, Bus Scale **0.8**, hop height/frequency/stagger **0.25 / 1.666667 / 0.2**, and moving bounce **0.0973**. All existing ease keys and step values are preserved.

The bus enters along a quadratic Bezier arc from offscreen bottom-right, settles flat at the same stop, and leaves along another arc toward offscreen bottom-left. Coordinates use bottom-left (0,0), top-right (1,1); values outside that range place the origin offscreen. The preview's **Show celebration path** toggle overlays cyan entry and yellow exit lines without changing the captured image or scene.

| New Celebration field | Saved default | Meaning |
| --- | --- | --- |
| Path Mode | Arc | Curved entry/exit; Straight retains legacy Enter Side and Exit Direction |
| Enter Start | (1.15, -0.35) | Entry origin just beyond bottom-right |
| Enter Arc Control | (0.85, 0.2) | Upward entry curve, flattening toward the stop |
| Exit End | (-0.15, -0.35) | Destination beyond bottom-left |
| Exit Arc Control | (0.15, 0.2) | Exit starts horizontally, then curves downward |
| Path Tilt | 5 degrees | Maximum roll toward the screen-space tangent, fading to zero at the stop; 0 disables it |
| Wave Arm Raise | 24 degrees | Upper-arm elevation above the sideward shoulder line; range 0-30 |
| Wave Speed | 1 | Existing Wave clip playback multiplier; 0 freezes it |
| Wave Amplitude | 25 degrees | Forearm swing around its bent midpoint; 0 holds it still |

Enter Ease and Exit Ease control travel along the curve. Arc mode maps each curve's authored values at time 0 and 1 to progress 0 and 1, so Asen's descending exit curve traverses forward without changing any keys. Endpoints are fixed, intermediate progress is clamped, empty or equal-endpoint curves use linear progress. Straight mode retains the previous curve sampling and side/direction behavior. Keep flat entry-end and exit-start tangents for a smooth stop. Moving suspension bounce remains independent of the path and disappears during the hold.

The original imported Wave already bends the forearm upward beside the head. Celebration uses its arm rotations over the standing Idle pose, mirrors the arm rotations for alternating **right / left / right** riders, and leaves the other arm relaxed. There are no celebration bone-scale overrides: arm and head proportions match the grid passenger. Existing overall rider size, hop timing, root squash and supporting-foot placement remain intact; feet return to the deck before exit. Last-bus colours, pooled cast, transparent 2x MSAA compositor and tap-to-skip remain in use.

The phase sum determines CelebrationBus duration and extends Complete when necessary. Other result steps keep independent timings. Changes in the Inspector resample a paused preview immediately, including camera changes at unchanged viewport size. No scene rebinding is required. ZCELEB-5 native Game-view evidence and verification are recorded in `ZCELEB-Report.md`.
