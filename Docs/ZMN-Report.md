# ZMN — Menu, HUD and passenger scale

- Restored ZM3 production first; `Game.unity` had no diff before implementation.
- Menu: green Level N button (55% width, centre at 78% from top), Fredoka Bold, unscaled 1–1.04 pulse; only the button starts play. Existing zero-life gating remains. One heart/count/regen plate shows mm:ss or FULL; menu time and tap prompt removed.
- HUD: two-line level badge, clock plate with m:ss, red/punch below 10 seconds, bottom-right Restart; no lives display.
- Passenger factor: **1.4×**, the largest clean tenth-step with at least 0.01 unit slot clearance. At 1.5×, slot body bounds reach 1.0592 against 1.0030 spacing. At 1.4×: slot width 0.9886; grid envelope 0.8378 × 0.6020 in 1-unit cells. Level_09 has 12 packed horizontal neighbour pairs; full seven-slot portrait preview fits. Body, outline, ropes, cloak and overhead marks share VisualRoot. Collider, cell size and paths unchanged; seat parenting restores visual scale to 1 and pooling restores the standing factor.
- Unity **2022.3.19f1** compiled clean. Edit-mode checks: 61 seconds → 1:01; 9 seconds → red 0:09; regen → 12:34/FULL; HUD lives displays → 0; seat compensation passed. Console: **0 entries** after clearing the recovered MCP transport warning. No Play entered; runtime interaction was not exercised.
- Two final 1080×1920 renders: [StartPanel](ZMN-Captures/StartPanel-1080x1920.png), [HudPanel / Level_09](ZMN-Captures/HudPanel-1080x1920.png).
- Production scene is committed. After commit, reapply the one-life / one-minute ZM3 seed, save Game.unity and leave Play off; seed stays uncommitted.
- AsenOS, M_Purple, M_Yellow and ArtSource were not modified by this work. No push.
