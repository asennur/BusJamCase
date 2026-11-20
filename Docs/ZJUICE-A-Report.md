# ZJUICE-A — Continuous bus flow

Completed A, F (bus handoff only) and G on `main`, based on `16fd9f6`. Unity **2022.3.19f1**, Edit mode throughout. No Play, player/APK build, push, branch creation or vault access. C/D/E are outside this run; B was cancelled.

## Cleanup and implementation

- Deleted the untracked `RunnerTraffic.cs` and its `.meta`. Restored `Passenger.cs` exactly to HEAD. Removed the traffic update loop, movement API changes, departure stagger, shared-corridor control, extra slot-reservation guard and traffic-only approach waypoint. Existing gameplay seat/slot reservations remain unchanged.
- Kept and completed the partial A work in `Bus.cs` and `BusStation.cs`. Nonfinal buses squash to 95% height and recover over 0.08 s, close their doors in that interval, then accelerate out with InQuad. Door-close playback speed resets when Drive starts.
- Arrival uses OutCubic with no positional overshoot. The next bus overlaps 60% of departure; queued buses start 0.04 s apart. Model-child pitch reaches 3 degrees, with one suspension bob of +/-0.025 world units in the last 0.09 s. Root transforms retain the authored lane and destinations.
- Bound `Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR` on `Bus.prefab`. Rotation follows actual distance / 0.24 world-unit radius around the imported bones' local Y axle. LateUpdate reapplies the pose after the Animator. No imported model, material or controller was changed.
- `BusApproaching` opens doors and dispatches matching **InSlot** passengers at 85% arrival distance. Their destination is the final stationary door position. Early runners are buffered per reserved seat; `EnterBus` starts only after physical arrival. The normal arrival event catches any remaining matching slot passengers. Grid taps keep the existing `IsAtDoor` requirement.
- Anticipation is one cached, non-autokilled sequence per pooled bus; movement tweens use DOTween recycling. Motion/pose updates use value types and cached arrays, with no per-frame collection, delegate or string creation. No particles were added. The Editor-only verification fixture intentionally allocates disposable capture/test objects.

## Timing

All times below are seconds after physical fill, for a nonfinal bus. Baseline is the HEAD implementation plus existing serialized values: 0.166667 door close + 0.3 departure + 0.3 next arrival + 0.3 brake wait.

| Milestone | Before | After (nominal) | Edit-mode measurement |
| --- | ---: | ---: | ---: |
| Anticipation / door close ends | 0.166667 | 0.080 | 0.081 departure start |
| Next bus starts rolling | 0.466667 | 0.200 | 0.200 |
| First / second follower starts | 0.466667 / 0.466667 | 0.240 / 0.280 | 0.240 / 0.281 |
| Next bus reaches 85%; doors open / slot approach starts | Not available | 0.3406 | 0.341 at 85.1124% |
| Departing bus finishes travel | 0.466667 | 0.380 | 0.381 |
| Next bus physically reaches the door | 0.766667 | 0.500 | 0.501 |
| Next bus boarding-ready event | 1.066667 | 0.500 | 0.501 |
| Departure/arrival movement overlap | 0% | 60% (0.180 / 0.300) | 60.33% |

The nominal cycle is **1.066667 -> 0.500 s**, a **53.125% reduction**. Measurements use 0.001 s manual ticks; callback boundaries can differ by one tick. The incoming bus starts during departure after sufficient body clearance, rather than waiting for the outgoing bus to finish.

The final bus retains the existing exception: waving and `AllBusesDone` happen immediately at fill; travel starts immediately and remains silent. Its squash overlaps departure so completion timing is not delayed. Passenger speeds, boarding animation durations, win/fail conditions, timer behavior and settled-state failure guards were not edited.

## Edit-mode verification

`BusJam.EditorTools.ZJuiceAVerification.Run()` passed in the open Editor. It clones production station settings and prefabs into disposable preview scenes, calls the production methods and drives their sequences with **DOTween.ManualUpdate**. Animators are explicitly sampled; preview objects are destroyed and the prior DOTween update mode / random state restored. Production scenes are never saved or rearranged.

| Check | Result |
| --- | --- |
| One approach, one arrival, one departure event for the handoff | Pass |
| Matching slot runner starts at >=85%; bus still not at door | Pass; 1 runner |
| Forced early runner reaches door buffer | Pass; 0 seat arrivals before physical arrival |
| Physical arrival flushes buffer exactly once | Pass; 1 seat callback afterward; repeated SetAtDoor does not duplicate it |
| Reset during buffered boarding | Pass; old callback cancelled and buffer cleared |
| Reset during anticipation/departure | Pass; no stale departure or occupied-seat state |
| Coarse update crossing approach and arrival together | Pass; 0.35 s tick |
| Final completion timing and silent departure | Pass; immediate single completion event, no departure sound event |
| Empty order / pool reuse | Pass |
| Squash / child pitch / suspension | 0.950 height ratio / 2.9997 degrees / +/-0.024985 units |
| Incoming positional overshoot | None at sampled ticks |
| Minimum outgoing/incoming root separation | 4.5587 units; exceeds 4.12-unit body length |
| Wheel integration | 2291.83 degrees over 9.6 units (initial arrival + handoff) |
| Compilation / Console | Clean; 0 errors, 0 warnings, 0 logs |

Raw output: [Verification.json](ZJUICE-A-Captures/Verification.json). This is production-sequence verification in Edit mode; runtime device performance and live input were not exercised. No build-size or device-cost claim is made.

## Five-frame handoff strip

[Open the strip](ZJUICE-A-Captures/Bus-Handoff-5-Frames.png). One 780x1500 PNG contains five 780x300 frames, top to bottom: **0.04, 0.22, 0.32, 0.40, 0.50 s**. It uses a close fixture camera to show the authored production bus spacing and movement; it does not alter Asen's game camera or layout. Departure completion returns the bus to the pool as the production gameplay listener does. Visual inspection confirmed the squash, departure lift, concurrent travel, incoming nose dip/settle and body clearance. No win/tutorial captures were produced.

## Scope and preservation

- Runner-distance table: **not applicable — B cancelled by Asen**. Passengers may pass through each other; no spacing guarantee or traffic simulation was added.
- `CompletePanel.cs`, `TapTutorial.cs`, the scenes and all UI layout objects are unchanged.
- Protected pre-existing dirty/untracked files (`M_Purple`, `M_Yellow`, `LevelDatabase`, `Level_26`, `ArtSource`, Otobus Materials) match their initial SHA-256 hashes and are excluded from the commit. `URP-Mobile` is unchanged. No global SaveAssets was used.
- `Docs/MIMARI.md` records the new bus presentation behavior and preserves the ZFEEL table as its baseline. No AsenOS files were read or written.
- One English commit contains only the A implementation, wheel bindings, verification helper/evidence and documentation. No push.
