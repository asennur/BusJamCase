# Result timeline

Select `Assets/_BusJam/Data/ResultTimeline.asset` in the Project window. Expand **Celebration** to manage the bus choreography; every control has an English tooltip. Expand a named step in the Inspector and edit **Enabled, Start, Duration, Ease, Overshoot** (OutBack only) or **Stagger** (confetti/coin flight). Times are unscaled seconds. Change Start to change appearance order; dragging list entries only organizes the Inspector. Keep one entry per name. For CelebrationBus, only Enabled is authored in the named step; Start and Duration are derived from Celebration.

Open **Bus Jam > Result Timeline Preview**. **Play Complete** and **Play Fail** animate in Edit mode; **Pause** and the **Time** slider allow scrubbing. **Coin Flight** previews Continue's collection; **Retry Fade** previews Retry. **Edit Timings** selects the asset. Choose 580Ã—1250, 1080Ã—1920 or 2560Ã—1080. The preview uses a disposable scene and never saves Game.unity or player progress.

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


The celebration defaults show the existing open-top bus in side profile, with a 10-degree downward camera angle. It enters from the right at 1.05 s, slows to a full stop at 1.60 s, holds until 2.80 s while the three riders Wave and hop on the deck, then accelerates downward and disappears at 3.45 s. Feet return to the deck before exit. Existing last-bus colours, clips, pooled cast, transparent 2x MSAA RenderTexture and tap-to-skip remain in use.

| Celebration field | Default | Meaning |
| --- | --- | --- |
| Start Time | 1.05 s | Time after Complete before entry starts |
| Enter Duration | 0.55 s | Travel and deceleration to the stop |
| Enter Side | Right | Left or right screen edge |
| Stop Position | (0.5, 0.2) | Bus origin in normalized screen coordinates: bottom-left (0,0), top-right (1,1) |
| Hold Duration | 1.2 s | Stationary celebration interval |
| Exit Duration | 0.65 s | Acceleration from stop to offscreen |
| Exit Direction | Down | Down, Left or Right in screen space |
| Camera Yaw | 0 degrees | Orbit around the side profile |
| Camera Pitch | 10 degrees | Positive tilts downward to reveal the deck |
| Camera Distance | 18 | Camera distance in world units; increasing it makes the bus smaller |
| Camera Fov | 35 degrees | Vertical field of view; increasing it makes the bus smaller |
| Bus Scale | 1 | Multiplies the aspect-fitted bus and riders |
| Rider Hop Height | 0.25 | Fraction of measured standing body height; clamped to 0–0.4 |
| Rider Hop Frequency | 1.666667 Hz | Hop cycles per second; zero disables hops |
| Rider Hop Stagger | 0.2 s | Delay between neighbouring riders' hop cycles |
| Enter Ease | OutCubic curve | Normalized entry progress, ending at rest |
| Exit Ease | InCubic curve | Normalized exit progress, starting at rest |
| Bus Bounce Amplitude | 0.04 | Suspension travel in unscaled bus units while moving; hold stays still |

The phase sum determines CelebrationBus duration and extends the Complete track when needed. Other result steps retain their independent timings. To delay rewards or buttons after a longer celebration, edit their named steps too. Curves are sampled without allocations; endpoints are fixed at 0/1 and intermediate values are clamped to 0–1. Empty curves fall back to linear. Keep flat entry-end and exit-start tangents for a smooth stop.

Use **Bus Jam > Result Timeline Preview**, **Play Complete**, **Pause** and **Time** to inspect any pose in Edit mode. Change the asset in the Inspector while the preview is paused: its current time is sampled again without restarting. Camera controls apply on the next sample, including at unchanged viewport dimensions. Preview uses a disposable scene and never saves Game.unity. ZCELEB-4 verification and native Game View evidence are recorded in `ZCELEB-Report.md`.
