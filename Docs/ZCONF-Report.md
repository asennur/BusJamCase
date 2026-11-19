# ZCONF Report
- Unity 2022.3.19f1, Edit mode only; Play never entered; no screenshots taken.
- Imported the existing SlotIdle take with Loop Time enabled; added its base-layer state with no transitions; other states unchanged.
- Included Claude's existing Passenger.cs SlotIdle/WaitingHash change; no scripts edited by Codex.
- Confetti cause: no scene invisibility fault found; PreviewFinalPose explicitly hides confetti (alpha 0); runtime absence remains unverified.
- Scene fix: all 40 confetti Images now draw after Dim/Header and before Continue; valid sprites, 30 px size, alpha 1, active children, no masks, raycasts off.
- Level 1 slots: bindings are intact and both ends lock at level 1; existing capture helpers use Setup(100), explaining seven unlocked white tiles.
- Compile completed cleanly; Console 0 after clearing the recovered MCP transport warning; one failed MCP snippet succeeded on its single corrected retry.
- Layout retained; protected materials/model directories and AsenOS untouched; no push.
