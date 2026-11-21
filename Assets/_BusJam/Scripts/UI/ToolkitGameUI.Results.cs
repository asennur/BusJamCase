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
        private const float IntroDuration = 3.45f, CollectionDuration = 1.8f;
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
            celebrationBus = new CompleteCelebration(assets, celebrationImage, gameObject.scene, Application.isPlaying ? Camera.main : null);
            celebrationBus.Resize(Screen.width, Screen.height);
        }

        private void CacheResults()
        {
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
            if (celebration != null) return;
            celebration = Reusable(DOTween.To(GetTimelineStart, SetCelebrationTime, IntroDuration, IntroDuration).SetEase(Ease.Linear));
            celebrationIdle = Reusable(DOTween.To(GetTimelineStart, SetCelebrationIdle, 72f, 72f).SetEase(Ease.Linear).SetLoops(-1));
            failureEntry = Reusable(DOTween.To(GetTimelineStart, SetFailureEntry, 1.3f, 1.3f).SetEase(Ease.Linear));
            collection = Reusable(DOTween.To(GetTimelineStart, SetCollectionTime, CollectionDuration, CollectionDuration).SetEase(Ease.Linear));
            failureExit = Reusable(DOTween.To(GetTimelineStart, SetFailureExit, .4f, .4f).SetEase(Ease.Linear));
        }

        private static float GetTimelineStart() => 0;
        private static void ScaleElement(VisualElement element, float value) => element.style.scale = new Scale(new Vector3(value, value, 1));
        private static void MoveElement(VisualElement element, Vector2 point) => element.style.translate = new Translate(point.x, point.y);
        private static float Phase(float time, float start, float duration) => Mathf.Clamp01((time - start) / duration);
        private static float Pop(float value)
        {
            float p = value - 1;
            return 1 + 2.70158f * p * p * p + 1.70158f * p * p;
        }

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
                if (completionSession) { FinishCelebration(); celebrationIdle.Restart(); return; }
                completionSession = true;
                celebrationBus?.Prepare(gameManager.CurrentLevel);
                rewardStart = SaveSystem.Coins; arrivedCoins = 0; resultCoinValue = rewardValue = -1;
                rewardPending = true; rewardCredited = false;
                for (int i = 0; i <= FlyingCoinCount; i++)
                    arrivalLabels[i] = ((int)System.Math.Min(int.MaxValue, (long)rewardStart + (long)coinReward * i / FlyingCoinCount)).ToString();
                SetResultCoinValue(rewardStart); SetCelebrationTime(0); SetCollectionTime(0); SetCelebrationIdle(0);
                celebration.Restart(); celebrationIdle.Restart();
            }
            else if (state == GameState.Fail)
            {
                completionSession = rewardPending = false;
                BuildResultTweens();
                ShowFailure(gameManager.FailReason, Mathf.Max(1, gameManager.CurrentLevelNumber));
                SetFailureEntry(0); failureEntry.Restart();
            }
            else completionSession = rewardPending = false;
        }

        // Shared deterministic sampler: DOTween in-game, explicit time in Edit-mode evidence.
        private void SetCelebrationTime(float time)
        {
            celebrationTime = time;
            celebrationBus?.Sample(time);
            complete.style.backgroundColor = new Color(.10f, .015f, .18f, .88f * Phase(time, 1.05f, .2f));
            resultCoinPlate.style.opacity = Phase(time, .3f, .15f);
            float title = Phase(time, 1.75f, .3f);
            resultHeader.style.opacity = time < 1.75f ? 0 : 1;
            ScaleElement(resultHeader, Mathf.Max(.01f, Pop(title)));
            rays.style.opacity = .8f * title;
            ScaleElement(rays, .5f + title * 1.5f);
            float count = Phase(time, 2.5f, .9f);
            rewardContent.style.opacity = time < 2.5f ? 0 : 1;
            ScaleElement(pile, Mathf.Lerp(.15f, 1, Pop(Phase(time, 2.5f, .3f))));
            SetRewardValue(Mathf.RoundToInt(coinReward * (1 - (1 - count) * (1 - count))));
            float button = Phase(time, 2.6f, .3f);
            continueButton.style.opacity = time < 2.6f ? 0 : 1;
            ScaleElement(continueButton, Mathf.Max(.01f, Pop(button)));
            continueButton.SetEnabled(time >= 2.6f && !collecting);
            rewardFlash.style.opacity = .45f * Mathf.Sin(Phase(time, 2.5f, .45f) * Mathf.PI);
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
                twinkles[i].style.opacity = celebrationTime >= 1.75f ? pulse : 0;
                ScaleElement(twinkles[i], .15f + pulse * .7f);
            }
            for (int i = 0; i < pileGlints.Length; i++)
            {
                float pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin(time * 8 + i * 2.1f)), 8);
                pileGlints[i].style.opacity = celebrationTime >= 2.5f ? pulse : 0;
                ScaleElement(pileGlints[i], .15f + pulse * .7f);
            }
        }

        private void AnimateConfetti(float time)
        {
            float width = effects.resolvedStyle.width, height = effects.resolvedStyle.height;
            for (int i = 0; i < confetti.Length; i++)
            {
                float start = .3f + i % 6 * .009f;
                float p = Phase(time, start, .75f);
                float seed = (i * .618034f) % 1;
                Vector2 from = new Vector2(width * (.3f + seed * .4f), height * .54f);
                float direction = i % 2 == 0 ? -1 : 1;
                Vector2 point = from + new Vector2(direction * width * (.18f + seed * .4f) * p,
                    height * (-1.5f * p + 1.8f * p * p) * (.5f + seed * .4f));
                MoveElement(confetti[i], point);
                confetti[i].style.opacity = time <= start ? 0 : 1 - Phase(p, .72f, .28f);
                confetti[i].style.rotate = new Rotate(direction * (i * 17 + p * 660));
                confetti[i].style.scale = new Scale(new Vector3((.5f + seed * .7f) * (.4f + .6f * Mathf.Abs(Mathf.Cos(p * 12 + i))), .5f + seed * .7f, 1));
            }
        }

        private void SetCollectionTime(float time)
        {
            buttonGlow.style.opacity = collecting ? 1 - Phase(time, .25f, .8f) : 0;
            ScaleElement(buttonGlow, 1 + .2f * Mathf.Sin(Phase(time, 0, .5f) * Mathf.PI));
            Vector2 from = effects.WorldToLocal(pile.worldBound.center);
            Vector2 to = effects.WorldToLocal(resultCoinPlate.worldBound.center);
            int arrivals = 0;
            for (int i = 0; i < flyingCoins.Length; i++)
            {
                float p = Phase(time, i * .025f, .875f);
                float seed = (i * .618034f) % 1;
                Vector2 origin = from + new Vector2((seed - .5f) * 240, (i % 4 - 1.5f) * 35);
                Vector2 control = Vector2.Lerp(origin, to, .45f) + new Vector2((seed - .5f) * 800, -220 - seed * 200);
                float q = p * p, inv = 1 - q;
                MoveElement(flyingCoins[i], inv * inv * origin + 2 * inv * q * control + q * q * to - Vector2.one * 34);
                flyingCoins[i].style.opacity = collecting && p > 0 && p < 1 ? 1 : 0;
                flyingCoins[i].style.rotate = new Rotate((seed - .5f) * Mathf.Sin(p * Mathf.PI) * 260);
                ScaleElement(flyingCoins[i], Mathf.Lerp(1.1f, .65f, p));
                if (p >= 1) arrivals++;
            }
            if (collecting && arrivals != arrivedCoins)
            {
                arrivedCoins = arrivals;
                resultCoinValue = (int)System.Math.Min(int.MaxValue, (long)rewardStart + (long)coinReward * arrivals / FlyingCoinCount);
                resultCoins.text = arrivalLabels[arrivals];
            }
            float latest = .875f + Mathf.Clamp(arrivals - 1, 0, FlyingCoinCount - 1) * .025f;
            ScaleElement(resultCoinPlate, 1 + (arrivals > 0 ? .12f * Mathf.Sin(Phase(time, latest, .13f) * Mathf.PI) : 0));
            if (collecting && arrivals == FlyingCoinCount) CreditReward();
            complete.style.opacity = collecting ? 1 - Phase(time, 1.35f, .45f) : 1;
            if (collecting && time >= CollectionDuration) FinishResultTransition(false);
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
            if (shownState != GameState.Complete || celebrationTime < 2.6f || collecting || resultTransitionDone) return;
            FinishCelebration(); collecting = true; continueButton.SetEnabled(false);
            collection.Restart();
        }

        private void RetryLevel()
        {
            if (shownState != GameState.Fail || failureTime < .8f || retrying || resultTransitionDone) return;
            retrying = true; retryButton.SetEnabled(false); failClose.SetEnabled(false);
            failureExit.Restart();
        }

        private void FinishResultTransition(bool retry)
        {
            if (resultTransitionDone) return;
            resultTransitionDone = true;
            collection?.Pause(); failureExit?.Pause();
            if (Application.isPlaying) { if (retry) gameManager.Retry(); else gameManager.LoadNext(); }
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
            float p = Phase(time, .6f, .2f);
            fail.style.backgroundColor = new Color(.06f, .03f, .16f, .72f * p);
            failPanel.style.opacity = time < .6f ? 0 : p;
            ScaleElement(failPanel, .86f + .14f * Pop(p));
            retryButton.SetEnabled(time >= .8f && !retrying); failClose.SetEnabled(time >= .8f && !retrying);
            failLives.text = time < .7f ? lifeBeforeLossText : lifeAfterLossText;
            failCountdown.text = time < .7f ? lifeBeforeLossTime : lifeAfterLossTime;
            float heart = Phase(time, .7f, .6f);
            floatingHeart.style.opacity = time < .7f ? 0 : 1 - heart;
            Vector2 origin = failEffects.WorldToLocal(failHudHeart.worldBound.center);
            MoveElement(floatingHeart, origin + new Vector2(-33 + heart * 40, -33 - heart * 150));
            floatingHeart.style.rotate = new Rotate(heart * 25);
            ScaleElement(floatingHeart, .7f + .3f * Mathf.Sin(heart * Mathf.PI));
        }

        private void SetFailureExit(float time)
        {
            fail.style.opacity = 1 - Phase(time, 0, .4f);
            if (retrying && time >= .4f) FinishResultTransition(true);
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
            if (shownState == GameState.Fail && failureTime >= .7f)
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
