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
        private VisualElement complete, completeBody, completePanel, resultHeader, rays, pile, rewardFlash;
        private VisualElement resultCoinPlate, effects, fail, failPanel, failIcon, lifeCost, noLives;
        private Label rewardLabel, resultCoins, failReason, failLevel, noLivesCount, noLivesCountdown;
        private Button continueButton, retryButton, failClose;
        private readonly VisualElement[] confetti = new VisualElement[54];
        private readonly VisualElement[] twinkles = new VisualElement[8];
        private readonly VisualElement[] flyingCoins = new VisualElement[FlyingCoinCount];
        private Tween celebration, celebrationIdle, failureEntry;
        private IVisualElementScheduledItem noLivesTimer;
        private float noLivesHideAt, celebrationTime;
        private int rewardStart, arrivedCoins, resultCoinValue = -1;
        private bool rewardPending, rewardCredited, completionSession;
        private int skipPointer = -1;

        private void CacheResults()
        {
            complete = root.Q("complete-overlay"); completeBody = root.Q("complete-body");
            completePanel = root.Q("complete-panel"); resultHeader = root.Q("complete-heading"); rays = root.Q("complete-rays");
            pile = root.Q("reward-pile"); rewardFlash = root.Q("reward-flash"); rewardLabel = root.Q<Label>("reward-label");
            resultCoinPlate = root.Q("result-coin-plate"); resultCoins = root.Q<Label>("result-coins");
            continueButton = root.Q<Button>("continue-button"); effects = root.Q("celebration-effects");
            fail = root.Q("fail-overlay"); failPanel = root.Q("fail-panel"); failIcon = root.Q("fail-icon"); lifeCost = root.Q("life-cost");
            failReason = root.Q<Label>("fail-reason"); failLevel = root.Q<Label>("fail-level");
            retryButton = root.Q<Button>("retry-button"); failClose = root.Q<Button>("fail-close");
            noLives = root.Q("no-lives-overlay"); noLivesCount = root.Q<Label>("no-lives-count");
            noLivesCountdown = root.Q<Label>("no-lives-countdown");
            for (int i = 0; i < confetti.Length; i++)
            {
                confetti[i] = Effect(effects, i % 3 == 0 ? "confetti-star" : "confetti-piece");
                confetti[i].style.unityBackgroundImageTintColor = ConfettiColor(i);
            }
            for (int i = 0; i < FlyingCoinCount; i++) flyingCoins[i] = Effect(effects, "flying-coin");
            VisualElement sparkleLayer = root.Q("complete-sparkles");
            for (int i = 0; i < twinkles.Length; i++)
            {
                twinkles[i] = Effect(sparkleLayer, "result-sparkle");
                twinkles[i].style.left = Length.Percent(8 + i * 12);
                twinkles[i].style.top = Length.Percent(i % 2 == 0 ? 16 : 68);
            }
            noLivesTimer?.Pause();
            noLivesTimer = root.schedule.Execute(CheckNoLivesDeadline).Every(100);
            noLivesTimer.Pause();
            textBuffer.Clear().Append('+').Append(coinReward); rewardLabel.text = textBuffer.ToString();
        }

        private static VisualElement Effect(VisualElement parent, string style)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
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
            celebration = Reusable(DOTween.To(GetCelebrationStart, SetCelebrationTime, 2.95f, 2.95f).SetEase(Ease.Linear));
            celebrationIdle = Reusable(DOTween.To(GetCelebrationStart, SetCelebrationIdle, 18f, 18f).SetEase(Ease.Linear).SetLoops(-1));
            failureEntry = Reusable(DOTween.To(GetCelebrationStart, SetFailureEntry, 1f, .45f).SetEase(Ease.OutCubic));
        }

        private static float GetCelebrationStart() => 0;
        private static void ScaleElement(VisualElement element, float value) => element.style.scale = new Scale(new Vector3(value, value, 1));
        private static void MoveElement(VisualElement element, Vector2 point) => element.style.translate = new Translate(point.x, point.y);
        private static float Phase(float time, float start, float duration) => Mathf.Clamp01((time - start) / duration);

        private void DisplayResults(GameState state)
        {
            if (complete == null) return;
            HideNoLives();
            celebration?.Pause(); celebrationIdle?.Pause(); failureEntry?.Pause();
            Show(complete, state == GameState.Complete); Show(fail, state == GameState.Fail);
            if (state == GameState.Complete)
            {
                BuildResultTweens();
                if (completionSession)
                {
                    FinishCelebration(); celebrationIdle.Restart();
                    return;
                }
                completionSession = true;
                rewardStart = SaveSystem.Coins; arrivedCoins = 0; resultCoinValue = -1;
                rewardPending = true; rewardCredited = false;
                SetResultCoinValue(rewardStart);
                SetCelebrationTime(0);
                celebration.Restart(); celebrationIdle.Restart();
            }
            else if (state == GameState.Fail)
            {
                completionSession = rewardPending = false;
                BuildResultTweens();
                ShowFailure(gameManager.FailReason, Mathf.Max(1, gameManager.CurrentLevelNumber));
                failureEntry.Restart();
            }
            else completionSession = rewardPending = false;
        }

        // One reusable timeline drives fixed pools; only discrete coin arrivals update text.
        private void SetCelebrationTime(float time)
        {
            celebrationTime = time;
            float pop = Phase(time, .7f, .32f);
            float panelScale = pop < .7f ? Mathf.Lerp(.6f, 1.08f, Mathf.Sin(pop / .7f * Mathf.PI * .5f))
                : Mathf.Lerp(1.08f, 1f, (pop - .7f) / .3f);
            completePanel.style.opacity = time < .7f ? 0 : 1;
            ScaleElement(completePanel, panelScale);
            float drop = Phase(time, 1.4f, .24f);
            pile.style.opacity = rewardLabel.style.opacity = time < 1.4f ? 0 : 1;
            MoveElement(pile, new Vector2(drop < 1 ? 0 : Mathf.Sin(Phase(time, 1.64f, .24f) * Mathf.PI * 4) * 12 * (1 - Phase(time, 1.64f, .24f)), Mathf.Lerp(-130, 0, 1 - (1 - drop) * (1 - drop))));
            float flash = 1 - Mathf.Abs(Phase(time, 1.64f, .3f) * 2 - 1);
            rewardFlash.style.opacity = flash;
            ScaleElement(rewardFlash, .7f + flash * .6f);
            continueButton.style.opacity = Phase(time, 2.6f, .2f);
            ScaleElement(continueButton, Mathf.Lerp(.8f, 1f, Phase(time, 2.6f, .2f)));
            continueButton.SetEnabled(time >= 2.6f);
            AnimateConfetti(time);
            AnimateCoins(time);
        }

        private void SetCelebrationIdle(float time)
        {
            rays.style.rotate = new Rotate(time * 20);
            rays.style.opacity = .55f + .15f * Mathf.Sin(time * 3);
            for (int i = 0; i < twinkles.Length; i++)
            {
                float pulse = .5f + .5f * Mathf.Sin(time * 7 + i * 1.7f);
                twinkles[i].style.opacity = celebrationTime >= .7f ? pulse : 0;
                ScaleElement(twinkles[i], .3f + pulse * .7f);
            }
            if (celebrationTime >= 2.8f) ScaleElement(continueButton, 1 + .035f * Mathf.Sin(time * 5));
        }

        private void AnimateConfetti(float time)
        {
            float width = effects.resolvedStyle.width, height = effects.resolvedStyle.height;
            for (int i = 0; i < confetti.Length; i++)
            {
                float p = Phase(time, .3f + (i % 6) * .012f, 2.25f);
                float seed = (i * .618034f) % 1;
                Vector2 from, apex, end;
                if (i % 3 == 2)
                {
                    from = new Vector2(seed * width, -70);
                    apex = from + new Vector2(60, height * .3f);
                    end = new Vector2(seed * width + Mathf.Sin(i) * 160, height + 90);
                }
                else
                {
                    bool left = i % 3 == 0;
                    from = new Vector2(left ? 0 : width, height);
                    apex = new Vector2(width * (left ? .12f + seed * .55f : .88f - seed * .55f), height * (-.4f + seed * .3f));
                    end = new Vector2(width * seed, height + 130);
                }
                float inv = 1 - p;
                MoveElement(confetti[i], inv * inv * from + 2 * inv * p * apex + p * p * end);
                confetti[i].style.opacity = time < .3f ? 0 : 1 - Phase(p, .7f, .3f);
                confetti[i].style.rotate = new Rotate((i % 2 == 0 ? 1 : -1) * p * 900);
                ScaleElement(confetti[i], .6f + seed * .8f);
            }
        }

        private void AnimateCoins(float time)
        {
            Vector2 from = effects.WorldToLocal(pile.worldBound.center);
            Vector2 to = effects.WorldToLocal(resultCoinPlate.worldBound.center);
            int arrivals = 0;
            for (int i = 0; i < flyingCoins.Length; i++)
            {
                float p = Phase(time, 1.9f + i * .025f, .55f);
                Vector2 point = Vector2.Lerp(from, to, p);
                point.x += Mathf.Sin(p * Mathf.PI) * ((i - 6.5f) * 18);
                point.y -= Mathf.Sin(p * Mathf.PI) * 160;
                MoveElement(flyingCoins[i], point - Vector2.one * 34);
                flyingCoins[i].style.opacity = p > 0 && p < 1 ? 1 : 0;
                ScaleElement(flyingCoins[i], Mathf.Lerp(1.1f, .65f, p));
                if (p >= 1) arrivals++;
            }
            if (arrivals != arrivedCoins)
            {
                arrivedCoins = arrivals;
                SetResultCoinValue((int)System.Math.Min(int.MaxValue, (long)rewardStart + (long)coinReward * arrivals / FlyingCoinCount));
                coinPulse?.Rewind(); coinPulse?.Restart();
            }
            float latestArrival = 1.9f + .55f + Mathf.Clamp(arrivals - 1, 0, FlyingCoinCount - 1) * .025f;
            float punch = arrivals > 0 ? Mathf.Sin(Phase(time, latestArrival, .13f) * Mathf.PI) : 0;
            ScaleElement(resultCoinPlate, 1 + .12f * punch);
            if (arrivals == FlyingCoinCount) CreditReward();
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
            SetCelebrationTime(2.95f); celebration?.Pause();
            CreditReward();
        }

        private void SkipCelebration(PointerDownEvent evt)
        {
            if (celebrationTime >= 2.95f) return;
            FinishCelebration();
            evt.StopImmediatePropagation();
            // The pointer that skipped must not also activate Continue on release.
            complete.CapturePointer(evt.pointerId);
            skipPointer = evt.pointerId;
        }

        private void ReleaseSkipPointer(PointerUpEvent evt)
        {
            if (evt.pointerId != skipPointer) return;
            complete.ReleasePointer(evt.pointerId);
            skipPointer = -1;
            evt.StopImmediatePropagation();
        }

        private void ContinueLevel()
        {
            if (!Application.isPlaying || shownState != GameState.Complete || celebrationTime < 2.6f) return;
            FinishCelebration(); gameManager.LoadNext();
        }

        private void RetryLevel() { if (Application.isPlaying && shownState == GameState.Fail) gameManager.Retry(); }

        private void ShowFailure(FailReason reason, int level)
        {
            failReason.text = reason == FailReason.OutOfTime ? "Time's up!" : reason == FailReason.NoMoves ? "No moves left!" : "No space left!";
            textBuffer.Clear().Append("Level ").Append(level); failLevel.text = textBuffer.ToString();
            failIcon.EnableInClassList("clock-icon", reason == FailReason.OutOfTime);
            failIcon.EnableInClassList("broken-heart", reason != FailReason.OutOfTime);
            Show(lifeCost, reason != FailReason.OutOfTime);
            SetFailureEntry(1);
        }

        private void SetFailureEntry(float value)
        {
            ScaleElement(failPanel, Mathf.Lerp(.8f, 1, value));
            ScaleElement(failIcon, 1 + Mathf.Sin(Phase(value, .55f, .45f) * Mathf.PI * 3) * .12f * (1 - value));
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
        }

        private void StopResults()
        {
            FinishCelebration();
            HideNoLives(); celebration?.Pause(); celebrationIdle?.Pause(); failureEntry?.Pause();
            if (skipPointer >= 0) complete.ReleasePointer(skipPointer);
            skipPointer = -1;
        }

        private void KillResultTweens()
        {
            celebration?.Kill(); celebrationIdle?.Kill(); failureEntry?.Kill();
            celebration = celebrationIdle = failureEntry = null;
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
