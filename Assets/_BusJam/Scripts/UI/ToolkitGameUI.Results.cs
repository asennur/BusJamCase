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
        [SerializeField] private ResultTimeline resultTimeline;
        private int timelineRevision;
        private ResultTimeline sampledTimeline;
        private float IntroDuration => resultTimeline.Length(ResultTrack.Complete);
        private float CollectionDuration => resultTimeline.Length(ResultTrack.Collection);
        private float FailureDuration => resultTimeline.Length(ResultTrack.Fail);
        private float RetryDuration => resultTimeline.Length(ResultTrack.Retry);
        private ResultStep Step(ResultStepId id) => resultTimeline.Get(id);
        private float RetryReadyAt => Step(ResultStepId.Panel).start + Step(ResultStepId.Panel).duration;
        private VisualElement complete, completePanel, resultHeader, rays, pile, rewardContent, rewardFlash;
        private VisualElement resultCoinPlate, effects, fail, failPanel, failIcon, lifeCost, noLives;
        private VisualElement buttonGlow, floatingHeart, failEffects, failHudHeart;
        private Label rewardLabel, resultCoins, failReason, failLevel, noLivesCount, noLivesCountdown, failLives, failCountdown;
        private Button continueButton, retryButton, failClose;
        private readonly VisualElement[] confetti = new VisualElement[54];
        private readonly VisualElement[] twinkles = new VisualElement[8];
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
            var assets = Resources.Load<CelebrationSettings>("CelebrationSettings");
            celebrationBus = new CompleteCelebration(assets, celebrationImage, gameObject.scene, Application.isPlaying ? Camera.main : null, resultTimeline);
            celebrationBus.Resize(Screen.width, Screen.height);
        }

        private void CacheResults()
        {
            if (resultTimeline == null) resultTimeline = Resources.Load<CelebrationSettings>("CelebrationSettings").resultTimeline;
            timelineRevision = resultTimeline.Revision;
            sampledTimeline = resultTimeline;
            complete = root.Q("complete-overlay"); completePanel = root.Q("complete-panel");
            celebrationImage = root.Q<Image>("celebration-bus");
            if (Application.isPlaying) InitializeCelebrationBus();
            resultHeader = root.Q("complete-title-group"); rays = root.Q("complete-rays");
            pile = root.Q("reward-pile"); rewardContent = root.Q("reward-content");
            rewardFlash = root.Q("reward-flash"); rewardLabel = root.Q<Label>("reward-label");
            resultCoinPlate = root.Q("result-coin-plate"); resultCoins = root.Q<Label>("result-coins");
            continueButton = root.Q<Button>("continue-button"); buttonGlow = root.Q("continue-glow");
            effects = root.Q("celebration-effects");
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
                confetti[i] = Effect(effects, shapes[i % shapes.Length]);
                confetti[i].style.unityBackgroundImageTintColor = ConfettiColor(i);
            }
            for (int i = 0; i < FlyingCoinCount; i++) flyingCoins[i] = Effect(effects, "flying-coin");
            VisualElement sparkleLayer = root.Q("complete-sparkles");
            for (int i = 0; i < twinkles.Length; i++)
            {
                twinkles[i] = Effect(sparkleLayer, "result-sparkle");
                twinkles[i].style.left = Length.Percent(5 + i * 12);
                twinkles[i].style.top = Length.Percent(i % 2 == 0 ? 14 : 70);
            }
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
            complete.style.backgroundColor = new Color(.10f, .015f, .18f, .88f * Step(ResultStepId.Overlay).Evaluate(time));
            resultCoinPlate.style.opacity = Step(ResultStepId.CoinPlate).Evaluate(time);
            var title = Step(ResultStepId.Title);
            resultHeader.style.opacity = title.Visible(time) ? 1 : 0;
            ScaleElement(resultHeader, Mathf.Max(.01f, title.Evaluate(time)));
            float ray = Step(ResultStepId.Rays).Evaluate(time);
            rays.style.opacity = .8f * ray; ScaleElement(rays, .5f + ray * 1.5f);
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
            rays.style.rotate = new Rotate(time * 5);
            for (int i = 0; i < twinkles.Length; i++)
            {
                float pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin(time * 5 + i * 1.7f)), 4);
                twinkles[i].style.opacity = Step(ResultStepId.Title).Visible(celebrationTime) ? pulse : 0;
                ScaleElement(twinkles[i], .15f + pulse * .7f);
            }
            for (int i = 0; i < pileGlints.Length; i++)
            {
                float pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin(time * 8 + i * 2.1f)), 8);
                pileGlints[i].style.opacity = Step(ResultStepId.CoinPile).Visible(celebrationTime) ? pulse : 0;
                ScaleElement(pileGlints[i], .15f + pulse * .7f);
            }
        }

        private void AnimateConfetti(float time)
        {
            float width = effects.resolvedStyle.width, height = effects.resolvedStyle.height;
            for (int i = 0; i < confetti.Length; i++)
            {
                var step = Step(ResultStepId.Confetti);
                float start = step.start + i % 6 * step.stagger;
                float p = step.Evaluate(time, i % 6);
                float seed = (i * .618034f) % 1;
                Vector2 from = new Vector2(width * (.3f + seed * .4f), height * .54f);
                float direction = i % 2 == 0 ? -1 : 1;
                Vector2 point = from + new Vector2(direction * width * (.18f + seed * .4f) * p,
                    height * (-1.5f * p + 1.8f * p * p) * (.5f + seed * .4f));
                MoveElement(confetti[i], point);
                confetti[i].style.opacity = !step.enabled || time <= start ? 0 : 1 - Phase(p, .72f, .28f);
                confetti[i].style.rotate = new Rotate(direction * (i * 17 + p * 660));
                confetti[i].style.scale = new Scale(new Vector3((.5f + seed * .7f) * (.4f + .6f * Mathf.Abs(Mathf.Cos(p * 12 + i))), .5f + seed * .7f, 1));
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
            if (collecting && time >= CollectionDuration) { CreditReward(); FinishResultTransition(false); }
        }

        private void CreditReward()
        {
            if (!rewardPending || rewardCredited) return;
            rewardCredited = true;
            if (Application.isPlaying) SaveSystem.AddCoins(coinReward);
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
            collection?.Pause(); failureExit?.Pause();
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
            fail.style.backgroundColor = new Color(.06f, .03f, .16f, .72f * overlay.Evaluate(time));
            failPanel.style.opacity = panel.Phase(time);
            ScaleElement(failPanel, .86f + .14f * panel.Evaluate(time));
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
            if (retrying && time >= RetryDuration) FinishResultTransition(true);
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
            Show(noLives, true); noLivesTimer.Resume();
        }

        private void CheckNoLivesDeadline() { if (Time.unscaledTime >= noLivesHideAt) HideNoLives(); }
        private void HideNoLives() { if (noLives != null) Show(noLives, false); noLivesTimer?.Pause(); }
        private void ShowNoLivesTime(int count, int seconds)
        {
            if (noLivesCount == null) return;
            noLivesCount.text = livesCount.text;
            noLivesCountdown.text = count >= livesSystem.MaxLives ? "" : "Next life in " + Clock(seconds, true);
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
            switch (index % 5)
            {
                case 0: return new Color(1, .8f, .2f);
                case 1: return new Color(1, .3f, .55f);
                case 2: return new Color(.25f, .8f, 1);
                case 3: return new Color(.4f, 1, .5f);
                default: return new Color(.75f, .4f, 1);
            }
        }
    }
}
