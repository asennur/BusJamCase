# ZG — Celebration, coins and failure UI

- Unity 2022.3.19f1, Edit mode only. No vault access or push. M_Purple, M_Yellow and ArtSource are untouched and excluded.
- Production restored before editing. Existing Start/HUD objects were retained. Their horizontal layout group serializes its driven anchors/positions; no authored layout settings were changed.

## Hierarchy and art

- Existing `StartPanel/UpUI/MoneyCounter` now reads saved coins beside the lives plate. `SafeArea/CoinCounter` is its Complete-only counterpart, top-right at (-365,-103), avoiding a duplicate Start counter.
- `CompletePanel`: fullscreen Dim; Header (glow_rays, ribbon_purple, title_complete); Reward (coin_pile, RewardAmount, six sparkles); NextButton; 40 confetti and 10 flying coin Images.
- `FailPanel`: fullscreen Dim; panel_modal with panel_header/Level, title_failed, ReasonIcon, LifeCost, Reason, RetryButton and icon_close. Both buttons retry without spending an additional life.
- All new sprites reference UIArt.asset. Fredoka-Bold SDF for UI, LilitaOne SDF for large numbers. Sliced plates/buttons use ZFIX's height-derived PPU multiplier; other art uses Simple + Preserve Aspect. LilitaOne material scale ratios were normalized by TMP rendering.
- Canvas remains 1080x1920, match 0.5. Content stays under SafeArea; FullscreenDim extends only the backdrop to the canvas edges.

## Timing and tuning

Select CompletePanel: reward defaults to 25; every animation duration/start offset is serialized. Confetti 0.00–1.40; dim 0.15 + 0.25; header 0.30 + 0.35; pile 0.70 + 0.30, counting over 0.90; Continue 1.10 + 0.30. Rays rotate 20 degrees/second; sparkle/button loops use unscaled time. Transfer is 10 curved flights, 0.60 each, staggered by 0.05. Each arrival updates/punches the counter; the final callback credits PlayerPrefs once, then loads the next level. Integer overflow saturates safely.

Complete timelines and Images are constructed once. On hide, timelines rewind/pause and pieces reset; OnDestroy kills all cached tween handles. This deliberately retains dormant timelines rather than killing/reallocating them on every close, to meet the no-per-Complete-allocation requirement. Fail entry is killed on hide. CoinCounter exposes its punch duration; FailPanel exposes dim/entry/punch durations (0.20/0.25/0.35).

## Verification

- Compilation clean; final Console check: zero entries.
- `ZGVerification.Run()` passed coin persistence, negative/overflow guards, duplicate-credit rejection, final reward/header/confetti state, 40+10 fixed pools, nine staggered arrivals without early saving, close/reset, and all three failure reasons. PlayerPrefs restored after checks.
- Exactly two Edit-mode captures: `ZG-Captures/Complete.png` and `Fail.png`, both 1242x2688. Visual inspection: intact caps/artwork, no visible overflow. SafeArea bounds assertions passed for reward/modal. Play was never entered; live input and LoadNext execution were not exercised.
- One setup invocation hit a missing CanvasScaler reference, corrected on its single retry. Refresh handled its own one reconnect/retry successfully.
- After committing: Prepare One-Life Test (Edit), save Game.unity, keep Play off. This final scene change remains uncommitted.
