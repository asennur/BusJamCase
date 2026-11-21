# ZSEAT — Passenger world size

21 November 2026. Verified in Unity **2022.3.19f1**, pinned instance `BusJamCase@c4dcbf17c52e6f1b`, **Edit mode only**.

## Result

`standingVisualScale` remains **1.61**, exactly **1.15 × 1.4**. `seatedScale` is now explicitly serialized as **1** in `Passenger.prefab`, with a matching C# default. All three real seats and the completed `EnterBus` path pass the seated/ground height target **1.00 ± 0.03**.

World-space body `Renderer.bounds.size`, in Unity units:

| Measurement | Height Y | Width X | Depth Z | Height / current ground |
| --- | ---: | ---: | ---: | ---: |
| Old ground reference, visual scale 1.4 | 2.345209 | 1.256646 | 0.819000 | 0.869565 |
| Current ground, visual scale 1.61 | 2.696991 | 1.445143 | 0.941850 | 1.000000 |
| Before: real SetSeated, loaded scale 1.3 | 3.506511 | 1.225615 | 1.878687 | **1.300157** |
| Before: EnterBus completed, loaded scale 1.3 | 3.506511 | 1.225615 | 1.878687 | **1.300157** |
| After: real SetSeated, scale 1 | 2.697316 | 0.942781 | 1.445144 | **1.000121** |
| After: Sit sampled at 0 and 0.5 seconds | 2.697316 | 0.942781 | 1.445144 | **1.000121** |
| After: EnterBus completed, callback received | 2.697316 | 0.942781 | 1.445144 | **1.000121** |
| After: seats 1 and 2, each | 2.697316 | 0.942781 | 1.445144 | **1.000121** |
| Ground rotated to seated facing | 2.697315 | 0.942781 | 1.445143 | 1.000120 |

The bus faces passengers approximately 90 degrees from their grid facing, exchanging world X/Z width and depth. With matching facing, both width and height ratios are **1.000000** to six decimals. These are renderer bounding volumes; the bent sitting pose naturally changes the occupied silhouette without enlarging the character.

## Root cause

The prefab omitted `seatedScale`, and its loaded serialized object retained **1.3** through the C# default change, so both seating paths still applied a real 30% enlargement. The measured seat chain is uniformly scaled and Sit's 45 scale curves remain approximately one, excluding parent nonuniformity and animation scaling as the cause of this reproduction.

## Transform and animation evidence

| Chain, seat to root | Local scale | World lossy scale | Local Euler rotation |
| --- | --- | --- | --- |
| Seat_0 | 1, 1, 1 | 1.058498, 1.058498, 1.058498 | 270, 90, 0 |
| Body | 1, 1, 1 | 1.058498, 1.058498, 1.058498 | 0, 270, 270 |
| Root | 1, 1, 1 | 1.058498, 1.058498, 1.058498 | 0, 0, 90 |
| OtobusRig | 1, 1, 1 | 1.058498, 1.058498, 1.058498 | 270.0198, 0, 0 |
| Otobus model | 0.814229, 0.814229, 0.814229 | 1.058498, 1.058498, 1.058498 | 0, 180, 0 |
| Bus | 1.3, 1.3, 1.3 | 1.3, 1.3, 1.3 | 0, 0, 0 |
| BusStation | 1, 1, 1 | 1, 1, 1 | 0, 0, 0 |

Grid parent world scale is **1.5** on each axis. Passenger root world scale was **1.95** when seated before the fix, and is **1.5** afterward; seated root local scale is **1.417103**. VisualRoot remains **1.61** on all axes through Setup, SetSeated, Sit sampling, and EnterBus completion. Sit scale keys range approximately **0.9999998–1.000003**, with no VisualRoot scale binding.

## Implementation and verification

- `Passenger.prefab`: preserve Luma's 1.61 standing size and explicitly store seated scale 1, removing dependence on a missing serialized field's editor history.
- `Passenger.cs`: preserve Luma's default 1; derive parent stretch from `localToWorldMatrix` along the passenger's rotated local axes. Dividing directly by the seat's `lossyScale` components associates the wrong axes under rotated nonuniform parents.
- Cache the seated local scale in SetSeated; EnterBus only multiplies that vector during its grow tween. No renderer scans, bounds fitting, or matrix calculations run per tween frame.
- Instantiate the same BoardView passenger prefab under a copy of the actual board transform, initialize Awake, then use the real Setup call as BoardView.Build does for GameplayController. Instantiate and initialize the actual Bus prefab under the station transform, reserve its real seat points, and call SetSeated / EnterBus directly.
- Sample Idle/Sit explicitly in Edit mode, with animation culling disabled on temporary instances. Complete the actual EnterBus tween with callbacks enabled; verify all three seats.
- Two additional rotated nonuniform seat cases, local scales `(0.65, 1.7, 2.2)` and `(2.1, 0.8, 1.4)`, both retain world basis lengths `(1.5, 1.5, 1.5)`. Returning those passengers to the grid parent and calling Setup restores the original ground bounds exactly.
- Search scene/prefab YAML for both size fields: only Passenger.prefab defines them; no scene or variant size overrides were found. Its loaded seated value is 1 after import.
- Final scripts compile cleanly; Console **0 entries**. Temporary probe and unsaved additive scene removed; original Game scene remains clean in the editor and was never saved. Existing on-disk Game.unity edits remain outside this commit.
- Protected assets and spriteatlas files untouched. No vault access and no Play mode.

## Capture and raw evidence

[1080 × 1920 Edit capture](../../BusJamCase_ZVEC_Review/seat/ZSEAT-Edit-1080x1920.png): real bus with three seated passengers above grid passengers, using the scene camera and actual animation poses.

Evidence directory: `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/seat/`. It contains `before-loaded.txt`, `before-forced-one.txt`, `after.txt`, `old-ground.txt`, `stress.txt`, and the archived temporary `ZSeatProbe.cs` used for verification. No verification scene or helper remains in Assets.
