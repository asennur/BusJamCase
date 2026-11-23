using BusJam.Core;
using BusJam.Save;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    public sealed partial class ToolkitGameUI
    {
        [SerializeField, Min(0)] private int coinReward = 25;
        [SerializeField, Min(0)] private float noLivesDuration = 3.5f;
        private const int FlyingCoinCount = 14;
        [SerializeField] private CelebrationSettings celebrationSettings;
        [SerializeField] private ResultTimeline resultTimeline;
        private int timelineRevision;
        private ResultTimeline sampledTimeline;
        private float IntroDuration => resultTimeline.Length(ResultTrack.Complete);
        private float CollectionDuration => resultTimeline.Length(ResultTrack.Collection);
        private float FailureDuration => resultTimeline.Length(ResultTrack.Fail);
        private float RetryDuration => resultTimeline.Length(ResultTrack.Retry);
        private ResultStep Step(ResultStepId id) => resultTimeline.Get(id);
        private float RetryReadyAt => Step(ResultStepId.Panel).start + Step(ResultStepId.Panel).duration;
        private VisualElement complete, completePanel, resultHeader, rays, raysFront, raysBack, halo, burst, pile, rewardContent, rewardFlash;
        private VisualElement resultCoinPlate, effects, fail, failPanel, failIcon, lifeCost, noLives;
        private VisualElement buttonGlow, floatingHeart, failEffects, failHudHeart, confettiBase;
        private Label rewardLabel, resultCoins, failReason, failLevel, noLivesCount, noLivesCountdown, failLives, failCountdown;
        private Button continueButton, retryButton, failClose;
        private readonly VisualElement[] confetti = new VisualElement[96];
        private const int ColoredConfetti = 72; // the rest are small white paper flecks
        private readonly VisualElement[] twinkles = new VisualElement[8];
        private readonly VisualElement[] dust = new VisualElement[12];
        // Başlık çevresindeki parıltı noktaları (başlık kutusu 739x380 ref px).
        private static readonly Vector2[] TwinkleSpots =
        { new Vector2(78, 92), new Vector2(648, 70), new Vector2(30, 236), new Vector2(700, 214),
          new Vector2(206, 34), new Vector2(560, 22), new Vector2(150, 318), new Vector2(612, 330) };
        private static readonly Color ResultDim = new Color(.03f, .04f, .19f, .9f);
        private float idleTime;
        private readonly VisualElement[] pileGlints = new VisualElement[4];
        private readonly VisualElement[] flyingCoins = new VisualElement[FlyingCoinCount];
        private readonly string[] arrivalLabels = new string[FlyingCoinCount + 1];
        private string[] rewardLabels;
        private string lifeBeforeLossText, lifeBeforeLossTime, lifeAfterLossText, lifeAfterLossTime;
        private Tween celebration, celebrationIdle, failureEntry, collection, failureExit;
        private IVisualElementScheduledItem noLivesTimer;
        private float noLivesHideAt, celebrationTime, failureTime;
        private int rewardStart, arrivedCoins, resultCoinValue = -1, rewardValue = -1;
        private bool rewardPending, rewardCredited, completionSession, collecting, retrying, resultTransitionDone;
        private int skipPointer = -1;
        private CompleteCelebration celebrationBus;
        private Image celebrationImage;

        // Edit-mode reviews opt in explicitly; the real scene never creates preview objects.
        private void InitializeCelebrationBus()
        {
            if (celebrationBus != null) return;
            celebrationBus = new CompleteCelebration(celebrationSettings, celebrationImage, gameObject.scene, Application.isPlaying ? Camera.main : null, resultTimeline);
            celebrationBus.Resize(Screen.width, Screen.height);
        }

        private void CacheResults()
        {
            if (resultTimeline == null) resultTimeline = celebrationSettings.resultTimeline;
            timelineRevision = resultTimeline.Revision;
            sampledTimeline = resultTimeline;
            complete = root.Q("complete-overlay"); completePanel = root.Q("complete-panel");
            celebrationImage = root.Q<Image>("celebration-bus");
            if (Application.isPlaying) InitializeCelebrationBus();
            resultHeader = root.Q("complete-title-group"); rays = root.Q("complete-rays");
            raysFront = root.Q("rays-front"); raysBack = root.Q("rays-back");
            halo = root.Q("complete-halo"); burst = root.Q("complete-burst");
            pile = root.Q("reward-pile"); rewardContent = root.Q("reward-content");
            rewardFlash = root.Q("reward-flash"); rewardLabel = root.Q<Label>("reward-label");
            resultCoinPlate = root.Q("result-coin-plate"); resultCoins = root.Q<Label>("result-coins");
            continueButton = root.Q<Button>("continue-button"); buttonGlow = root.Q("continue-glow");
            effects = root.Q("celebration-effects"); confettiBase = root.Q("bar-space");
            fail = root.Q("fail-overlay"); failPanel = root.Q("fail-panel"); failIcon = root.Q("fail-icon"); lifeCost = root.Q("life-cost");
            failReason = root.Q<Label>("fail-reason"); failLevel = root.Q<Label>("fail-level");
            failLives = root.Q<Label>("fail-lives-count"); failCountdown = root.Q<Label>("fail-countdown");
            failEffects = root.Q("fail-effects"); failHudHeart = root.Q("fail-hud-heart");
            retryButton = root.Q<Button>("retry-button"); failClose = root.Q<Button>("fail-close");
            noLives = root.Q("no-lives-overlay"); noLivesCount = root.Q<Label>("no-lives-count");
            noLivesCountdown = root.Q<Label>("no-lives-countdown");
            string[] shapes = { "confetti-star", "confetti-heart", "confetti-disc", "confetti-piece" };
            for (int i = 0; i < confetti.Length; i++)
            {
                confetti[i] = Effect(effects, i < ColoredConfetti ? shapes[i % shapes.Length] : "confetti-piece");
                confetti[i].style.unityBackgroundImageTintColor = i < ColoredConfetti ? ConfettiColor(i) : Color.white;
            }
            for (int i = 0; i < FlyingCoinCount; i++) flyingCoins[i] = Effect(effects, "flying-coin");
            VisualElement sparkleLayer = root.Q("complete-sparkles");
            for (int i = 0; i < twinkles.Length; i++)
            {
                twinkles[i] = Effect(sparkleLayer, "result-sparkle");
                MoveElement(twinkles[i], TwinkleSpots[i] - new Vector2(19, 19));
            }
            VisualElement dustLayer = root.Q("complete-dust");
            for (int i = 0; i < dust.Length; i++) dust[i] = Effect(dustLayer, "result-dust");
            for (int i = 0; i < pileGlints.Length; i++)
            {
                pileGlints[i] = Effect(root.Q("pile-glints"), "result-sparkle");
                pileGlints[i].style.left = Length.Percent(8 + i * 23);
                pileGlints[i].style.top = Length.Percent(14 + i % 3 * 18);
            }
            floatingHeart = Effect(failEffects, "life-float");
            rewardLabels = new string[coinReward + 1];
            for (int i = 0; i <= coinReward; i++) rewardLabels[i] = i.ToString();
            noLivesTimer?.Pause();
            noLivesTimer = root.schedule.Execute(CheckNoLivesDeadline).Every(100);
            noLivesTimer.Pause();
        }

        private static VisualElement Effect(VisualElement parent, string style)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore, usageHints = UsageHints.DynamicTransform };
            element.AddToClassList("pooled-effect"); element.AddToClassList(style); parent.Add(element);
            return element;
        }

        private void BindResults(bool bind)
        {
            if (bind)
            {
                continueButton.clicked += ContinueLevel; retryButton.clicked += RetryLevel; failClose.clicked += RetryLevel;
                complete.RegisterCallback<PointerDownEvent>(SkipCelebration, TrickleDown.TrickleDown);
                complete.RegisterCallback<PointerUpEvent>(ReleaseSkipPointer, TrickleDown.TrickleDown);
            }
            else
            {
                continueButton.clicked -= ContinueLevel; retryButton.clicked -= RetryLevel; failClose.clicked -= RetryLevel;
                complete.UnregisterCallback<PointerDownEvent>(SkipCelebration, TrickleDown.TrickleDown);
                complete.UnregisterCallback<PointerUpEvent>(ReleaseSkipPointer, TrickleDown.TrickleDown);
            }
        }

        private void BuildResultTweens()
        {
            if (!Application.isPlaying || celebration != null) return;
            celebration = Reusable(DOTween.To(GetTimelineStart, SetCelebrationTime, Mathf.Max(.0001f, IntroDuration), Mathf.Max(.0001f, IntroDuration)).SetEase(Ease.Linear));
            celebrationIdle = Reusable(DOTween.To(GetTimelineStart, SetCelebrationIdle, 72f, 72f).SetEase(Ease.Linear).SetLoops(-1));
            failureEntry = Reusable(DOTween.To(GetTimelineStart, SetFailureEntry, Mathf.Max(.0001f, FailureDuration), Mathf.Max(.0001f, FailureDuration)).SetEase(Ease.Linear));
            collection = Reusable(DOTween.To(GetTimelineStart, SetCollectionTime, Mathf.Max(.0001f, CollectionDuration), Mathf.Max(.0001f, CollectionDuration)).SetEase(Ease.Linear));
            failureExit = Reusable(DOTween.To(GetTimelineStart, SetFailureExit, Mathf.Max(.0001f, RetryDuration), Mathf.Max(.0001f, RetryDuration)).SetEase(Ease.Linear));
        }

        private static float GetTimelineStart() => 0;
        private static void ScaleElement(VisualElement element, float value) => element.style.scale = new Scale(new Vector3(value, value, 1));
        private static void MoveElement(VisualElement element, Vector2 point) => element.style.translate = new Translate(point.x, point.y);
        private static float Phase(float time, float start, float duration) => Mathf.Clamp01((time - start) / duration);
        private void DisplayResults(GameState state)
        {
            if (complete == null) return;
            HideNoLives(); PauseResultTweens();
            celebrationBus?.Hide();
            collecting = retrying = resultTransitionDone = false;
            Show(complete, state == GameState.Complete); Show(fail, state == GameState.Fail);
            if (state == GameState.Complete)
            {
                BuildResultTweens();
                complete.style.opacity = completePanel.style.opacity = 1;
                if (completionSession) { FinishCelebration(); celebrationIdle?.Restart(); return; }
                completionSession = true;
                celebrationBus?.Prepare(gameManager.CurrentLevel);
                rewardStart = SaveSystem.Coins; arrivedCoins = 0; resultCoinValue = rewardValue = -1;
                rewardPending = true; rewardCredited = false;
                for (int i = 0; i <= FlyingCoinCount; i++)
                    arrivalLabels[i] = ((int)System.Math.Min(int.MaxValue, (long)rewardStart + (long)coinReward * i / FlyingCoinCount)).ToString();
                SetResultCoinValue(rewardStart); SetCelebrationTime(0); SetCollectionTime(0); SetCelebrationIdle(0);
                celebration?.Restart(); celebrationIdle?.Restart();
            }
            else if (state == GameState.Fail)
            {
                completionSession = rewardPending = false;
                BuildResultTweens();
                ShowFailure(gameManager.FailReason, Mathf.Max(1, gameManager.CurrentLevelNumber));
                SetFailureEntry(0); failureEntry?.Restart();
            }
            else completionSession = rewardPending = false;
        }

        // Shared deterministic sampler: DOTween in-game, explicit time in Edit-mode evidence.
        private void SetCelebrationTime(float time)
        {
            celebrationTime = time;
            var busStep = Step(ResultStepId.CelebrationBus);
            if (busStep.enabled) celebrationBus?.Sample(time);
            else celebrationBus?.Hide();
            var dim = ResultDim; dim.a *= Step(ResultStepId.Overlay).Evaluate(time);
            complete.style.backgroundColor = dim;
            resultCoinPlate.style.opacity = Step(ResultStepId.CoinPlate).Evaluate(time);
            var title = Step(ResultStepId.Title);
            resultHeader.style.opacity = title.Visible(time) ? 1 : 0;
            // Pop-in: küçükten taşarak oturur, hafif eğiklikten düzelir.
            float pop = title.Evaluate(time);
            ScaleElement(resultHeader, Mathf.Max(.01f, .3f + .7f * pop));
            resultHeader.style.rotate = new Rotate(-6 * (1 - Mathf.Clamp01(pop)));
            AnimateHeading();
            var pileStep = Step(ResultStepId.CoinPile);
            pile.style.opacity = pileStep.Visible(time) ? 1 : 0;
            rewardContent.style.opacity = 1;
            ScaleElement(pile, Mathf.Lerp(.15f, 1, pileStep.Evaluate(time)));
            var count = Step(ResultStepId.CoinCount);
            rewardLabel.style.opacity = count.Visible(time) ? 1 : 0;
            SetRewardValue(Mathf.RoundToInt(coinReward * count.Evaluate(time)));
            var button = Step(ResultStepId.ContinueButton);
            continueButton.style.opacity = button.Visible(time) ? 1 : 0;
            ScaleElement(continueButton, Mathf.Max(.01f, button.Evaluate(time)));
            continueButton.SetEnabled(button.Visible(time) && !collecting);
            var flash = Step(ResultStepId.RewardFlash);
            rewardFlash.style.opacity = flash.enabled ? .45f * Mathf.Sin(flash.Evaluate(time) * Mathf.PI) : 0;
            AnimateConfetti(time);
        }

        private void SetRewardValue(int value)
        {
            value = Mathf.Clamp(value, 0, coinReward);
            if (rewardValue == value) return;
            rewardValue = value; rewardLabel.text = rewardLabels[value];
        }

        private void SetCelebrationIdle(float time)
        {
            idleTime = time;
            AnimateHeading();
            for (int i = 0; i < pileGlints.Length; i++)
            {
                float pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin(time * 8 + i * 2.1f)), 8);
                pileGlints[i].style.opacity = Step(ResultStepId.CoinPile).Visible(celebrationTime) ? pulse : 0;
                ScaleElement(pileGlints[i], .15f + pulse * .7f);
            }
        }

        /// <summary>Başlık ışığı: yumuşak hale (giriş + nefes), ters dönen iki ışın katmanı, oturma anında parlama,
        /// yükselen toz ve yanıp sönen yıldızlar. Giriş celebrationTime'dan, döngü idleTime'dan; kapalı form, ayırma yok.</summary>
        private void AnimateHeading()
        {
            var title = Step(ResultStepId.Title);
            var rayStep = Step(ResultStepId.Rays);
            float t = celebrationTime, idle = idleTime;
            bool shown = title.Visible(t);
            float open = rayStep.enabled ? 1 - Mathf.Pow(1 - rayStep.Phase(t), 3) : 1;
            float breathe = Mathf.Sin(idle * 2.1f);
            float land = title.start + title.duration * .6f;
            float flash = Phase(t, land, .5f);
            float flashPulse = t >= land ? Mathf.Sin(flash * Mathf.PI) : 0;

            halo.style.opacity = shown ? Mathf.Min(1, .75f * open + .08f * breathe * open + .25f * flashPulse) : 0;
            ScaleElement(halo, (.55f + .45f * open) * (1 + .04f * breathe));
            rays.style.opacity = shown ? .12f * open * (.85f + .15f * breathe) : 0;
            rays.style.scale = new Scale(new Vector3(.7f + .3f * open, (.7f + .3f * open) * .62f, 1));
            raysFront.style.rotate = new Rotate(idle * 6);
            raysBack.style.rotate = new Rotate(9 - idle * 4);
            burst.style.opacity = t >= land && flash < 1 ? .9f * (1 - flash) : 0;
            ScaleElement(burst, .4f + 1.8f * (1 - (1 - flash) * (1 - flash)));

            for (int i = 0; i < dust.Length; i++)
            {
                float seed = (i * .618034f) % 1, period = 3.2f + seed * 1.8f;
                float f = (idle / period + seed) % 1;
                float x = 90 + seed * 560 + Mathf.Sin(f * 6.283f + i) * 14;
                float y = 340 - f * 300;
                MoveElement(dust[i], new Vector2(x - 13, y - 13));
                dust[i].style.opacity = shown ? open * .8f * Mathf.Sin(f * Mathf.PI) : 0;
                ScaleElement(dust[i], .35f + .5f * ((i * .414214f) % 1));
            }
            for (int i = 0; i < twinkles.Length; i++)
            {
                float pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin(idle * 3.4f + i * 1.9f)), 6);
                twinkles[i].style.opacity = shown ? pulse * open : 0;
                ScaleElement(twinkles[i], .2f + pulse * (i % 3 == 0 ? 1.1f : .75f));
                twinkles[i].style.rotate = new Rotate(pulse * 45);
            }
        }

        // Reference win: two bursts from the ends of the bottom bar strip shoot up and inward over the board, then flutter down.
        // Closed-form drag motion, so scrubbing any time gives the same frame.
        private void AnimateConfetti(float time)
        {
            var step = Step(ResultStepId.Confetti);
            float width = effects.resolvedStyle.width, height = effects.resolvedStyle.height;
            if (float.IsNaN(width) || width <= 0) return;
            float left = width * .2f, right = width * .8f, baseY = height * .86f;
            if (confettiBase != null && confettiBase.worldBound.width > 0)
            {
                Rect bar = confettiBase.worldBound;
                Vector2 a = effects.WorldToLocal(bar.min), b = effects.WorldToLocal(bar.max);
                left = a.x + (b.x - a.x) * .12f; right = b.x - (b.x - a.x) * .12f; baseY = Mathf.Lerp(a.y, b.y, .35f);
            }
            const float drag = 5f;
            // Pieces sit above the result overlay, so they dim with it like the reference (falling under the purple layer).
            float dim = 1 - .85f * Step(ResultStepId.Overlay).Evaluate(time);
            for (int i = 0; i < confetti.Length; i++)
            {
                float s = step.Phase(time, i % 6) * step.duration;
                float seed = (i * .618034f) % 1, seed2 = (i * .414214f + .3f) % 1, seed3 = (i * .732051f + .6f) % 1;
                float inward = i % 2 == 0 ? 1 : -1;
                float decay = (1 - Mathf.Exp(-drag * s)) / drag;
                // Up speed sets the apex (lower board to the road); fall speed tends to gravity / drag.
                float up = height * (1.6f + seed2 * 2.9f), gravity = height * 1.5f;
                float side = width * inward * (-.3f + seed3 * 2.4f);
                float x = (i % 2 == 0 ? left : right) + (seed - .5f) * width * .1f + side * decay
                    + Mathf.Sin(s * 9 + i) * width * .012f * Mathf.Min(1, s * 3);
                float y = baseY - (up + gravity / drag) * decay + gravity / drag * s;
                MoveElement(confetti[i], new Vector2(x, y));
                bool live = step.enabled && time > step.start + i % 6 * step.stagger;
                confetti[i].style.opacity = live ? dim * (1 - Phase(s, step.duration * .75f, step.duration * .25f)) : 0;
                confetti[i].style.rotate = new Rotate(i * 37 + inward * (300 + seed * 500) * s);
                float size = (.95f + seed2 * .5f) * (i < ColoredConfetti ? 1 : .35f);
                confetti[i].style.scale = new Scale(new Vector3(size * (.35f + .65f * Mathf.Abs(Mathf.Cos(s * (6 + seed * 8) + i))), size, 1));
            }
        }

        private void SetCollectionTime(float time)
        {
            var glow = Step(ResultStepId.CollectionGlow);
            var pulse = Step(ResultStepId.CollectionPulse);
            var flight = Step(ResultStepId.CoinFlight);
            buttonGlow.style.opacity = collecting && glow.enabled ? 1 - glow.Evaluate(time) : 0;
            ScaleElement(buttonGlow, 1 + (pulse.enabled ? .2f * Mathf.Sin(pulse.Evaluate(time) * Mathf.PI) : 0));
            Vector2 from = effects.WorldToLocal(pile.worldBound.center);
            Vector2 to = effects.WorldToLocal(resultCoinPlate.worldBound.center);
            int arrivals = 0;
            for (int i = 0; i < flyingCoins.Length; i++)
            {
                float p = flight.Phase(time, i);
                float seed = (i * .618034f) % 1;
                Vector2 origin = from + new Vector2((seed - .5f) * 240, (i % 4 - 1.5f) * 35);
                Vector2 control = Vector2.Lerp(origin, to, .45f) + new Vector2((seed - .5f) * 800, -220 - seed * 200);
                float q = flight.ApplyEase(p), inv = 1 - q;
                MoveElement(flyingCoins[i], inv * inv * origin + 2 * inv * q * control + q * q * to - Vector2.one * 34);
                flyingCoins[i].style.opacity = collecting && flight.enabled && p > 0 && p < 1 ? 1 : 0;
                flyingCoins[i].style.rotate = new Rotate((seed - .5f) * Mathf.Sin(p * Mathf.PI) * 260);
                ScaleElement(flyingCoins[i], Mathf.Lerp(1.1f, .65f, p));
                if (flight.enabled && p >= 1) arrivals++;
            }
            if (collecting && arrivals != arrivedCoins)
            {
                arrivedCoins = arrivals;
                resultCoinValue = (int)System.Math.Min(int.MaxValue, (long)rewardStart + (long)coinReward * arrivals / FlyingCoinCount);
                resultCoins.text = arrivalLabels[arrivals];
            }
            float latest = flight.start + flight.duration + Mathf.Clamp(arrivals - 1, 0, FlyingCoinCount - 1) * flight.stagger;
            var punch = Step(ResultStepId.CoinCounterPunch);
            ScaleElement(resultCoinPlate, 1 + (punch.enabled && arrivals > 0 ? .12f * Mathf.Sin(punch.Evaluate(time - latest) * Mathf.PI) : 0));
            if (collecting && arrivals == FlyingCoinCount) CreditReward();
            var fade = Step(ResultStepId.CollectionFade);
            complete.style.opacity = collecting && fade.enabled ? 1 - fade.Evaluate(time) : 1;
            // The level fade darkens while the panel fades, so the finished board never shows uncovered.
            if (collecting && (time >= CollectionDuration || fade.enabled && time >= fade.start)) FinishResultTransition(false);
        }

        private void CreditReward()
        {
            if (!rewardPending || rewardCredited) return;
            rewardCredited = true;
            // Level editörünün Test Play'i gerçek kayda coin yazmaz.
            if (Application.isPlaying && GameManager.TestLevel == null) SaveSystem.AddCoins(coinReward);
        }

        private void FinishCelebration()
        {
            if (!rewardPending) return;
            SetCelebrationTime(IntroDuration); celebration?.Pause();
        }

        private void SkipCelebration(PointerDownEvent evt)
        {
            if (collecting) { evt.StopImmediatePropagation(); SetCollectionTime(CollectionDuration); return; }
            if (!collecting && celebrationTime >= IntroDuration) return;
            FinishCelebration();
            evt.StopImmediatePropagation();
            // The skipping pointer cannot also activate Continue on release.
            complete.CapturePointer(evt.pointerId); skipPointer = evt.pointerId;
        }

        private void ReleaseSkipPointer(PointerUpEvent evt)
        {
            if (evt.pointerId != skipPointer) return;
            complete.ReleasePointer(evt.pointerId); skipPointer = -1; evt.StopImmediatePropagation();
        }

        private void ContinueLevel()
        {
            if (shownState != GameState.Complete || !Step(ResultStepId.ContinueButton).Visible(celebrationTime) || collecting || resultTransitionDone) return;
            FinishCelebration(); collecting = true; continueButton.SetEnabled(false);
            collection?.Restart();
        }

        private void RetryLevel()
        {
            if (shownState != GameState.Fail || failureTime < RetryReadyAt || retrying || resultTransitionDone) return;
            retrying = true; retryButton.SetEnabled(false); failClose.SetEnabled(false);
            failureExit?.Restart();
        }

        private void FinishResultTransition(bool retry)
        {
            if (resultTransitionDone) return;
            resultTransitionDone = true;
            // Collection and retry fades keep running under the level fade; the state change pauses them.
            if (Application.isPlaying) BeginTransition(retry ? FadeAction.Retry : FadeAction.Next);
        }

        private void ShowFailure(FailReason reason, int level)
        {
            failReason.text = reason == FailReason.OutOfTime ? "Time's up!" : reason == FailReason.NoMoves ? "No moves left!" : "No space left!";
            textBuffer.Clear().Append("Level ").Append(level); failLevel.text = textBuffer.ToString();
            if (lifeBeforeLossText == null) lifeBeforeLossText = Mathf.Min(livesSystem.MaxLives, lastLives + 1).ToString();
            if (lifeBeforeLossTime == null) lifeBeforeLossTime = "FULL";
            lifeAfterLossText = livesCount.text; lifeAfterLossTime = lifeCountdown.text;
            failIcon.RemoveFromClassList("clock-icon"); failIcon.AddToClassList("broken-heart"); Show(lifeCost, true);
            fail.style.opacity = 1;
        }

        private void SetFailureEntry(float time)
        {
            failureTime = time;
            var overlay = Step(ResultStepId.FailOverlay);
            var panel = Step(ResultStepId.Panel);
            var loss = Step(ResultStepId.LifeLoss);
            fail.style.backgroundColor = new Color(.02f, .02f, .05f, .8f * overlay.Evaluate(time));
            failPanel.style.opacity = panel.Phase(time);
            ScaleElement(failPanel, refScale * (.86f + .14f * panel.Evaluate(time)));
            retryButton.SetEnabled(time >= RetryReadyAt && !retrying); failClose.SetEnabled(time >= RetryReadyAt && !retrying);
            failLives.text = !loss.enabled || loss.Visible(time) ? lifeAfterLossText : lifeBeforeLossText;
            failCountdown.text = !loss.enabled || loss.Visible(time) ? lifeAfterLossTime : lifeBeforeLossTime;
            float heart = loss.Evaluate(time);
            floatingHeart.style.opacity = loss.Visible(time) ? 1 - heart : 0;
            Vector2 origin = failEffects.WorldToLocal(failHudHeart.worldBound.center);
            MoveElement(floatingHeart, origin + new Vector2(-33 + heart * 40, -33 - heart * 150));
            floatingHeart.style.rotate = new Rotate(heart * 25);
            ScaleElement(floatingHeart, .7f + .3f * Mathf.Sin(heart * Mathf.PI));
        }

        private void SetFailureExit(float time)
        {
            var fade = Step(ResultStepId.RetryFade);
            fail.style.opacity = fade.enabled ? 1 - fade.Evaluate(time) : 1;
            if (retrying && (time >= RetryDuration || fade.enabled && time >= fade.start)) FinishResultTransition(true);
        }

        private void SetResultCoinValue(int count)
        {
            if (count == resultCoinValue) return;
            resultCoinValue = count;
            textBuffer.Clear().Append(count); resultCoins.text = textBuffer.ToString();
            resultCoins.style.fontSize = Mathf.Min(48, 250f / textBuffer.Length);
        }

        private void ShowResultCoins(int count)
        {
            if (resultCoins != null && (!rewardPending || rewardCredited)) SetResultCoinValue(count);
        }

        private void ShowNoLives()
        {
            if (shownState != GameState.Start) return;
            noLivesHideAt = Time.unscaledTime + noLivesDuration;
            if (noLives.ClassListContains("hidden")) PopIn(noLives);
            Show(noLives, true); noLivesTimer.Resume();
        }

        private void CheckNoLivesDeadline() { if (Time.unscaledTime >= noLivesHideAt) HideNoLives(); }
        private void HideNoLives() { if (noLives != null) Show(noLives, false); noLivesTimer?.Pause(); }
        private void ShowNoLivesTime(int count, int seconds)
        {
            if (noLivesCount == null) return;
            noLivesCount.text = livesCount.text;
            noLivesCountdown.text = count >= livesSystem.MaxLives ? "FULL" : lifeCountdown.text;
            lifeAfterLossText = livesCount.text; lifeAfterLossTime = lifeCountdown.text;
            if (shownState == GameState.Fail && (!Step(ResultStepId.LifeLoss).enabled || Step(ResultStepId.LifeLoss).Visible(failureTime)))
            { failLives.text = lifeAfterLossText; failCountdown.text = lifeAfterLossTime; }
        }

        private void PauseResultTweens()
        {
            celebration?.Pause(); celebrationIdle?.Pause(); failureEntry?.Pause(); collection?.Pause(); failureExit?.Pause();
        }

        private void StopResults()
        {
            celebrationBus?.Hide();
            CreditReward(); HideNoLives(); collecting = retrying = false;
            celebration?.Rewind(); celebrationIdle?.Rewind(); failureEntry?.Rewind(); collection?.Rewind(); failureExit?.Rewind();
            PauseResultTweens();
            if (skipPointer >= 0) complete.ReleasePointer(skipPointer);
            skipPointer = -1;
        }

        private void KillResultTweens()
        {
            celebration?.Kill(); celebrationIdle?.Kill(); failureEntry?.Kill(); collection?.Kill(); failureExit?.Kill();
            celebration = celebrationIdle = failureEntry = collection = failureExit = null;
        }

        private void RefreshResultTimeline()
        {
            if (resultTimeline == null || sampledTimeline == resultTimeline && timelineRevision == resultTimeline.Revision) return;
            timelineRevision = resultTimeline.Revision;
            sampledTimeline = resultTimeline;
            Retime(celebration, IntroDuration, shownState == GameState.Complete && !collecting);
            Retime(failureEntry, FailureDuration, shownState == GameState.Fail && !retrying);
            Retime(collection, CollectionDuration, collecting && !resultTransitionDone);
            Retime(failureExit, RetryDuration, retrying && !resultTransitionDone);
            if (shownState == GameState.Complete) SetCelebrationTime(celebrationTime);
            else if (shownState == GameState.Fail) SetFailureEntry(failureTime);
        }

        private static void Retime(Tween tween, float length, bool active)
        {
            if (tween == null) return;
            float elapsed = tween.Elapsed();
            ((Tweener)tween).ChangeEndValue(Mathf.Max(.0001f, length), Mathf.Max(.0001f, length), false);
            tween.Goto(Mathf.Min(elapsed, length), active && elapsed < length);
        }

        private static Color ConfettiColor(int index)
        {
            switch (index % 7)
            {
                case 0: return new Color(1, .82f, .15f);
                case 1: return new Color(1, .3f, .6f);
                case 2: return new Color(.2f, .85f, 1);
                case 3: return new Color(.6f, 1, .2f);
                case 4: return new Color(.6f, .35f, 1);
                case 5: return new Color(1, .45f, .15f);
                default: return new Color(.35f, .45f, 1);
            }
        }
    }
}
