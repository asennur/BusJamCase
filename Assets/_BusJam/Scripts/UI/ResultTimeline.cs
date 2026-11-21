using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam.UI
{
    public enum ResultTrack { Complete, Collection, Fail, Retry }
    public enum ResultEase { Linear, InQuad, OutQuad, OutCubic, OutBack }
    public enum ResultStepId
    {
        Confetti, Overlay, CelebrationBus, Title, Rays, CoinPile, CoinCount, ContinueButton,
        CoinFlight, CoinPlate, RewardFlash, IntroHold, CollectionGlow, CollectionPulse,
        CoinCounterPunch, CollectionFade, BoardHold, FailOverlay, Panel, LifeLoss, RetryFade
    }

    [Serializable]
    public struct ResultStep
    {
        public ResultStepId id;
        public bool enabled;
        [Min(0)] public float start, duration;
        public ResultEase ease;
        [Min(0)] public float overshoot, stagger;
        public float End => start + duration + stagger * (id == ResultStepId.Confetti ? 5 : id == ResultStepId.CoinFlight ? 13 : 0);
        public bool Visible(float time) => enabled && time >= start;
        public float Phase(float time, int index = 0)
        {
            if (!enabled) return 0;
            float elapsed = time - start - stagger * index;
            return duration > 0 ? Mathf.Clamp01(elapsed / duration) : elapsed >= 0 ? 1 : 0;
        }
        public float Evaluate(float time, int index = 0) => ApplyEase(Phase(time, index));
        public float ApplyEase(float p)
        {
            switch (ease)
            {
                case ResultEase.InQuad: return p * p;
                case ResultEase.OutQuad: return 1 - (1 - p) * (1 - p);
                case ResultEase.OutCubic: return 1 - (1 - p) * (1 - p) * (1 - p);
                case ResultEase.OutBack: float q = p - 1; return 1 + (overshoot + 1) * q * q * q + overshoot * q * q;
                default: return p;
            }
        }
    }

    /// <summary>Named presentation steps. Inspector edits rebuild a small lookup, never the frame sampler.</summary>
    [CreateAssetMenu(menuName = "BusJam/Result Timeline")]
    public sealed class ResultTimeline : ScriptableObject
    {
        [SerializeField] private List<ResultStep> steps = Defaults();
        [NonSerialized] private ResultStep[] lookup;
        [NonSerialized] private readonly float[] lengths = new float[4];
        public IReadOnlyList<ResultStep> Steps => steps;
        public int Revision { get; private set; }
        public string ValidationMessage { get; private set; }
        public float Length(ResultTrack track) => lengths[(int)track];

        public ResultStep Get(ResultStepId id)
        {
            var step = lookup[(int)id];
            if (Track(id) == ResultTrack.Fail && id != ResultStepId.BoardHold)
            {
                var hold = lookup[(int)ResultStepId.BoardHold];
                if (hold.enabled) step.start = Mathf.Max(step.start, hold.End);
            }
            return step;
        }

        public static ResultTrack Track(ResultStepId id)
        {
            if (id == ResultStepId.RetryFade) return ResultTrack.Retry;
            if (id >= ResultStepId.BoardHold) return ResultTrack.Fail;
            if (id == ResultStepId.CoinFlight || id >= ResultStepId.CollectionGlow && id <= ResultStepId.CollectionFade) return ResultTrack.Collection;
            return ResultTrack.Complete;
        }

        private void OnEnable() => Rebuild();
        private void OnValidate() => Rebuild();
        public void Rebuild()
        {
            const int count = (int)ResultStepId.RetryFade + 1;
            if (lookup == null) lookup = new ResultStep[count];
            Array.Clear(lookup, 0, count); Array.Clear(lengths, 0, lengths.Length);
            ValidationMessage = "";
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                int key = (int)step.id;
                if (key < 0 || key >= count) continue;
                for (int j = 0; j < i; j++) if (steps[j].id == step.id) ValidationMessage = "Duplicate step names: the last entry wins. Use one entry per name.";
                step.start = Finite(step.start); step.duration = Finite(step.duration);
                step.overshoot = Finite(step.overshoot); step.stagger = Finite(step.stagger);
                steps[i] = step; lookup[key] = step;
            }
            for (int i = 0; i < count; i++)
            {
                var step = Get((ResultStepId)i);
                if (step.enabled) lengths[(int)Track((ResultStepId)i)] = Mathf.Max(lengths[(int)Track((ResultStepId)i)], step.End);
            }
            var flight = Get(ResultStepId.CoinFlight);
            var punch = Get(ResultStepId.CoinCounterPunch);
            if (flight.enabled && punch.enabled)
                lengths[(int)ResultTrack.Collection] = Mathf.Max(lengths[(int)ResultTrack.Collection], flight.End + punch.End);
            Revision++;
        }
        private static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Max(0, value);

        private static ResultStep S(ResultStepId id, float start, float duration, ResultEase ease = ResultEase.Linear, float stagger = 0)
            => new ResultStep { id = id, enabled = true, start = start, duration = duration, ease = ease, overshoot = 1.70158f, stagger = stagger };
        private static List<ResultStep> Defaults() => new List<ResultStep>
        {
            S(ResultStepId.Confetti, .3f, .75f, stagger: .009f),
            S(ResultStepId.Overlay, 1.05f, .2f), S(ResultStepId.CelebrationBus, 1.05f, 1.05f),
            S(ResultStepId.Title, 1.75f, .3f, ResultEase.OutBack), S(ResultStepId.Rays, 1.75f, .3f),
            S(ResultStepId.CoinPile, 2.5f, .3f, ResultEase.OutBack), S(ResultStepId.CoinCount, 2.5f, .9f, ResultEase.OutQuad),
            S(ResultStepId.ContinueButton, 2.6f, .3f, ResultEase.OutBack),
            S(ResultStepId.CoinPlate, .3f, .15f), S(ResultStepId.RewardFlash, 2.5f, .45f), S(ResultStepId.IntroHold, 3.4f, .05f),
            S(ResultStepId.CoinFlight, 0, .875f, ResultEase.InQuad, .025f),
            S(ResultStepId.CollectionGlow, .25f, .8f), S(ResultStepId.CollectionPulse, 0, .5f),
            S(ResultStepId.CoinCounterPunch, 0, .13f), S(ResultStepId.CollectionFade, 1.35f, .45f),
            S(ResultStepId.BoardHold, 0, .6f), S(ResultStepId.FailOverlay, .6f, .2f),
            S(ResultStepId.Panel, .6f, .2f, ResultEase.OutBack), S(ResultStepId.LifeLoss, .7f, .6f),
            S(ResultStepId.RetryFade, 0, .4f)
        };
    }
}
