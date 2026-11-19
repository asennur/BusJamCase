# ZBUS — New bus model

## Inspection after reimport

| Item | New Otobus.fbx | Previous model |
| --- | --- | --- |
| Idle | 1.000 s, loop | Idle |
| Drive | 0.666667 s, loop | Drive |
| Brake | 0.600 s, no loop | 0.600 s |
| DoorOpen | 0.333333 s, no loop | DoorOpen |
| DoorClose | 0.333333 s, no loop | 0.333333 s |
| Hierarchy | Otobus mesh; OtobusRig/Root/Body | Generic Root |
| Seats | Body/Seat_0, Seat_1, Seat_2 | Three seats |
| Door parts | Body/Door_Front, Door_Rear | Animated door |
| Other bones | Smoke_0..2, Wheel_FL/FR/RL/RR | Wheels |
| Material slots | 0 Body, 1 Glass, 2 GlassHi, 3 Light, 4 Tail, 5 Trim, 6 Tire, 7 Hub, 8 Seat, 9 Rail, 10 Smoke (all M_Bus prefix) | Ten slots, slot 0 M_BusBody |
| Imported renderer bounds | Approximately 5.32 × 2.76 × 1.62, including smoke/animation envelope; mesh measurements pending | Body approximately 4.12 × 1.62 × 1.62 |

All five required clips exist with the expected names. Unity 2022.3.19f1; Edit mode only. The ZM3 one-minute scene setting was restored through the production menu before model changes.

## Wiring and validation

- Generic rig, Root node Root, External materials. Slots 0–9 retain the existing bus materials; slot 10 Smoke maps to existing M_BusTire. No required clip is missing. The controller already maps all five clips correctly, defaults to Idle and has no transitions; its content therefore remains unchanged.
- Replaced the model instance and rebound body, Animator and Seat_0..2. Uniform model scale **0.814229** (4.12 / 5.06); yaw **180 degrees**: front +X, doors toward slots (-Z). New unscaled body is approximately **5.06 × 1.66 × 1.62**; taller seats and smoke enlarge the full bounds.
- DoorEntry local position **(-0.12, 0.08, -0.82)**, forward +Z into the doorway. Brake **0.6 s**, DoorClose **0.333333 s**.
- Passenger.seatRotation unchanged: **(270.02, 180, 0)**. All three seat transforms yield passenger world forward +X. EnterBus continues to move inward from DoorEntry. DoorOpen is non-looping with no outgoing transition; ZF code holds the end pose until CloseDoor.
- Game BusStation queueSpacing **4.8**. Sampled all five clips at 31 points each: combined X envelope **[-2.733962, 1.712526]**, length **4.446487**, minimum queue gap **0.353513**. Existing departure delay remains door-close delay + full 0.6 s departure before the queue moves; 6-unit exit displacement exceeds the envelope by 1.5535 units.
- [Edit-mode capture, 1080×1920](ZBUS-Captures/DoorOpen-1080x1920.png): DoorOpen sampled at its end, one queued bus. One screenshot produced. Preview objects were discarded; original scene layout preserved.
- Unity 2022.3.19f1 compilation completed. Final Console check: 0 entries. No Play-mode execution or runtime test. Refresh reported a disconnect and recovered on its internal retry; subsequent editor queries succeeded. Two diagnostic-code errors were corrected on one retry each.
- Production restoration succeeded after an initial approval-review rejection. Optional controller tags and deletion of the auto-extracted smoke material were rejected by approval review and omitted. That temporary material is untracked and unused; it is excluded from the commit.
- Existing scene layout edits, M_Purple, M_Yellow, Yolcu.fbx and ArtSource are excluded. AsenOS was neither read nor written. No push.
- After committing, reapply the requested one-life Edit test setup and save Game.unity; that scene test setting stays uncommitted.
