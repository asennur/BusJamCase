# Result timeline

Select `Assets/_BusJam/Data/ResultTimeline.asset` in the Project window. Expand a named step in the Inspector and edit **Enabled, Start, Duration, Ease, Overshoot** (OutBack only) or **Stagger** (confetti/coin flight). Times are unscaled seconds. Change Start to change appearance order; dragging list entries only organizes the Inspector. Keep one entry per name.

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
