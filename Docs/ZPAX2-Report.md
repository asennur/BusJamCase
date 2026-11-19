# ZPAX2 — Passengers, counters and bus life

Unity **2022.3.19f1**, all implementation and simulations in Edit mode. No Play initiated by this task; no push. AsenOS was neither read nor written.

## Lives: evidence and fix

The old `LivesSystem.RefreshAt` rebased `lastRegenUtc = now` on **every refresh while `rollbackUntilUtc > 0`**. A future persisted `LivesUtcHighWater` therefore prevented elapsed regeneration time from accumulating. Its countdown also omitted that guard wait, so the displayed interval did not describe the real next award. `lastObservedUtc` was initialized from the persisted guard, conflating a live clock observation with an old high-water value.

Before changing the class, an Edit-mode simulation invoked its actual private `Initialize` / `RefreshAt` methods:

| Save / simulation | Original result |
| --- | --- |
| Asen's actual current keys, shown below | 5 lives, 0 seconds; +1 second remained FULL |
| Controlled reproduction: Lives=4, LastRegenUtc=now, guard=now+1 day; refresh every second through +1,201 seconds | **Still 4 lives / 1,200 seconds** |

The future-guard stall is proven. **The exact historical “4 / 145 s” state was not present in Asen's current keys and cannot be attributed to a particular earlier seed from this evidence.** ZM3's existing seed already wrote matching timestamps; its production restore already deleted all three keys. GameManager's fail/restart accounting was inspected and did not cause the stall.

The fix separates the live observation from the persisted guard. A real backwards step rebases once; ordinary subsequent seconds accumulate normally. A forward crossing of the old high-water value rebases once to prevent a rebound award. A future regeneration timestamp is repaired once, while valid partial progress survives startup even if the saved guard is future-dated. Offline awards remain capped, retain partial intervals, and earn one life per serialized `regenMinutes`.

`LivesDisplay.OnEnable` now refreshes the source before presenting its values. Counts and mm:ss update through the existing one-second Changed event; FULL appears at capacity. There is no frame polling.

ZM3 seed/restore now also enforce `maxLives=5`. The one-life seed writes a single shared UTC timestamp to both time keys. Added **Bus Jam > Lives > Clear Lives Data (Edit)**. The older ZM3 verification fixture was brought up to date with Ready routing and the current two lives displays; its restart/guard checks remain isolated and restore saves after destroying fixtures.

### PlayerPrefs

Read directly inside the connected Unity editor before any simulation:

| Key | Before | Final production state |
| --- | --- | --- |
| `Lives` | Absent | Absent — defaults to 5 |
| `LastRegenUtc` | Absent | Absent |
| `LivesUtcHighWater` | Absent | Absent |
| `Coins` | 100 | 100 |
| Scene maxLives / regenMinutes | 5 / 20 | 5 / 20 |

No ZM3 test seed was active at arrival, so its conditional production-restore menu was unnecessary. All simulations restored the exact saved keys in `finally`. After the commit, clear only lives data again, save Game.unity, and leave Play off; **do not reapply the one-life seed**.

## Coins and authored layout

StartPanel's **UpUI/MoneyCounter** had a copied `LivesDisplay`: countText was null and countdownText referenced its money number. Removed that timer component and bound its existing `CoinCounter` to the existing text. Its position, dimensions, icon, art and hierarchy are preserved. Complete's counter uses the same saved total.

`SaveSystem.CoinsChanged(int)` publishes after a saved coin addition or reset. Enabled counters subscribe; disabled counters unsubscribe and read the latest total when shown again. No coin timer, Invoke or per-frame polling was added. Reward credit remains once, after the final coin arrival. A transform/hierarchy snapshot comparison passed during setup, and the pre-existing Game.unity layout diff remains included unchanged.

## Replacement model

Read ZE, ZMN, ZM1, ZM2b and ZART reports and inspected Passenger.cs / Passenger.prefab. Compared the leftover old imported FBX before deleting it, then force-imported the replacement at its existing GUID.

| Property | Old | Replacement / result |
| --- | --- | --- |
| Source FBX size | 576,076 bytes | 733,820 bytes |
| Mesh vertices | 2,073 | 2,073 |
| Skinned bones / root | 14 / Root | 14 / Root |
| Material slots | 1 | 1, external M_Yolcu import remap; runtime color renderer rebound |
| Bind mesh bounds, center XYZ | (0, 0, 0.43) | (0, 0, 0.43), rounded |
| Bind mesh bounds, extents XYZ | (0.31, 0.15, 0.42) | (0.31, 0.15, 0.42), rounded |
| Authored takes | Idle, Run, Sit, Wave | Same four plus SlotIdle source take; SlotIdle is not imported into the game controller |
| Game clips, seconds | Idle 2; Run 0.5333; Sit 0.6667; Wave 0.8 | Same ZE ranges; Idle/Run/Wave loop, Sit does not |
| Import / controller | Generic, Root; Idle/Run/Sit/Wave | Preserved and revalidated; existing .meta and passenger controller need no content diff |

The skeleton names remain Root, Hips, Spine, Head, paired UpperArm/LowerArm, Thigh/Shin/Foot. The prefab now instantiates the replacement afresh, rebinds Animator and color renderer, and creates its outline from the **same mesh, bones and rootBone**, using M_OutlineHull.

VisualRoot remains 1.4, model child 1.3: combined standing factor **1.82**. Rope layers are at Y .40/.50/.60/.70 with scale (.50,.026,.43). Cloak is at Y .66; mantle (.51,.32,.38), gold hem Y -.45 and (.515,.012,.385). Hidden mark is (0,1.25,.12), angry icon (0,1.40,0). The captures show the fitted silhouette and trim.

## Seated size and bus life

Removed VisualRoot's seated reset to 1. Seat bones inherit bus scale **0.8142**, which caused another size loss; seated root now cancels this parent scale. Standing and seated VisualRoot world scales both measure **(1.4000,1.4000,1.4000)**. EnterBus still shrinks at the door and grows to that full world size. seatRotation is unchanged and faces bus-forward (+X); seating remains at the existing seat anchor.

Standing envelope remains **0.8378 × 0.6020** in 1-unit cells; slot width **0.9886** fits **1.0030** spacing. The seated head has the same visible size as a grid passenger; the lower body may overlap the seat back slightly, as authorized.

Bus model child gently shakes at **13.5 Hz**, **0.003 units vertical** and **0.12° rotation**, serialized in Bus.prefab. Brake/Drive restore the resting model transform and suppress this extra shake. Queue Idle and door states retain it. Bus root and DoorEntry remain fixed; seat bones and their passengers follow the model together. The update allocates zero bytes in the warmed check.

The FBX already animates Smoke_0–2. Extracted those curves into **ExhaustLoop.anim**, played continuously on an **Exhaust** Animator layer with a smoke-only AvatarMask and Write Defaults disabled. It runs during driving and held door poses. Puffs scale from near-zero, drift and shrink in a loop. A tracked light-grey **M_ExhaustSmoke** replaces the dependency on the protected untracked material; that folder was untouched.

Real Animator evaluation in Edit mode advanced smoke scale **0 → 1.33** over .2 seconds while DoorOpen's position/rotation and Seat_1's position each changed by **0**.

## Validation and delivery

- **1,243 ZPAX2 Edit checks passed**, including 1,199 individual countdown-second assertions, exact refill, a saved 145-second partial interval, future timestamps, offline awards, rollback/rebound recovery, events, coin subscriptions, zero-allocation steady refresh/shake, outline bindings, EnterBus final scale and exhaust masking.
- **60 updated ZM3 checks passed**, including real GameManager fail/restart accounting, zero-life gating, UI text and rollback restart behavior.
- ZMN size validation passed: 12 packed horizontal neighbor pairs, unchanged cell/slot sizes.
- Compile clean; final Console **0 entries**. Refresh/domain reload produced MCP transport warnings that recovered on the tool's retry; cleared before the final Console check. One setup invocation reached the editor before its new class was loaded; the retry after import succeeded. No unresolved MCP failure.
- During the final check the editor was externally switched to LevelEditor / Play. The Edit-mode guard rejected the save without mutation. Stop reported it had already stopped; the clean LevelEditor scene was then left intact and Game.unity reopened/saved. Final Game scene: missing scripts 0, temporary verification objects 0, dirty=false, Play=false.
- Exactly two 1080×1920 Edit renders: [grid, passenger variants and full-size seated passenger](ZPAX2-Captures/HudPanel.png), [Start: 5 / FULL and 100 coins](ZPAX2-Captures/StartPanel.png). Runtime Play/device behavior was not tested.
- Deleted Assets/ZPAX_OldYolcu.fbx and its .meta. Initial status identified no other previous-run leftovers outside protected paths. M_Purple, M_Yellow, ArtSource and untracked Otobus/Materials remain untouched and excluded from the commit. No test level/assets or preview objects remain in Game.unity.
- One English commit; no push. Production scene saved, Play off, lives keys cleared for FULL startup. The final clear is local PlayerPrefs state and is not committed.
