using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam.UI
{
    public enum ResultTrack { Complete, Collection, Fail, Retry }
    public enum ResultEase { Linear, InQuad, OutQuad, OutCubic, OutBack }
    public enum CelebrationEnterSide { Left, Right }
    public enum CelebrationExitDirection { Down, Left, Right }
    public enum CelebrationPathMode { Arc, Straight }

    /// <summary>Screen-space bus choreography shared by runtime and the Edit-mode preview.</summary>
    [Serializable]
    public sealed class CelebrationChoreography
    {
        [Tooltip("Arc follows the curved screen path. Straight uses the legacy Enter Side and Exit Direction.")] public CelebrationPathMode pathMode = CelebrationPathMode.Arc;
        [Tooltip("Entry origin in normalized screen coordinates; (0,0) is bottom left. Values outside 0 to 1 are offscreen.")] public Vector2 enterStart = new Vector2(1.15f, -.35f);
        [Tooltip("Quadratic entry arc control in normalized screen coordinates. Place it above the entry origin for an upward curve.")] public Vector2 enterArcControl = new Vector2(.85f, .2f);
        [Tooltip("Exit destination in normalized screen coordinates, just beyond the bottom-left corner by default.")] public Vector2 exitEnd = new Vector2(-.15f, -.35f);
        [Tooltip("Quadratic exit arc control in normalized screen coordinates. Keep it near stop height for a smooth downward exit.")] public Vector2 exitArcControl = new Vector2(.15f, .2f);
        [Tooltip("Maximum bus roll in degrees toward the arc tangent. Settles flat at the stop. Zero disables tilt."), Range(0, 15)] public float pathTilt = 5;
        [Tooltip("Obsolete: imported Cheer clips now own rider motion. Retained for serialized compatibility."), Range(0, 30)] public float waveArmRaise = 24;
        [Tooltip("Obsolete: imported Cheer clips now own rider motion. Retained for serialized compatibility."), Range(0, 3)] public float waveSpeed = 1;
        [Tooltip("Obsolete: imported Cheer clips now own rider motion. Retained for serialized compatibility."), Range(0, 30)] public float waveAmplitude = 25;
        [Tooltip("Seconds after Complete before the bus enters."), Min(0)] public float startTime = 1.05f;
        [Tooltip("Seconds spent slowing from the entry edge to the stop."), Min(0)] public float enterDuration = .55f;
        [Tooltip("Screen edge from which the bus enters.")] public CelebrationEnterSide enterSide = CelebrationEnterSide.Right;
        [Tooltip("Bus origin on screen: (0,0) is bottom left, (1,1) is top right.")] public Vector2 stopPosition = new Vector2(.5f, .2f);
        [Tooltip("Seconds the bus stays stopped while its riders celebrate."), Min(0)] public float holdDuration = 1.2f;
        [Tooltip("Seconds spent accelerating from the stop to fully offscreen."), Min(0)] public float exitDuration = .65f;
        [Tooltip("Screen direction in which the bus leaves after the hold.")] public CelebrationExitDirection exitDirection = CelebrationExitDirection.Down;
        [Tooltip("Camera orbit around the bus in degrees; zero shows its side."), Range(-180, 180)] public float cameraYaw;
        [Tooltip("Camera downward viewing angle in degrees; positive values reveal the open deck."), Range(-15, 60)] public float cameraPitch = 10;
        [Tooltip("Camera distance from the stage in world units."), Range(8, 40)] public float cameraDistance = 18;
        [Tooltip("Vertical camera field of view in degrees."), Range(20, 60)] public float cameraFov = 35;
        [Tooltip("Bus and rider size multiplier. One fits the side profile inside each preview aspect."), Range(.2f, 2)] public float busScale = 1;
        [Tooltip("Obsolete: imported Cheer clips now own rider motion. Retained for serialized compatibility."), Range(0, .4f)] public float riderHopHeight = .25f;
        [Tooltip("Obsolete: imported Cheer clips now own rider motion. Retained for serialized compatibility."), Range(0, 5)] public float riderHopFrequency = 1.666667f;
        [Tooltip("Obsolete: imported Cheer clips now own rider motion. Retained for serialized compatibility."), Min(0)] public float riderHopStagger = .2f;
        [Tooltip("Entry progress curve. Default slows smoothly to a complete stop.")] public AnimationCurve enterEase = new AnimationCurve(new Keyframe(0, 0, 3, 3), new Keyframe(1, 1, 0, 0));
        [Tooltip("Exit progress curve. Default accelerates smoothly from rest.")] public AnimationCurve exitEase = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 3, 3));
        [Tooltip("Suspension bounce in unscaled bus units while moving; the hold stays stationary."), Range(0, .2f)] public float busBounceAmplitude = .04f;
        public float Duration => enterDuration + holdDuration + exitDuration;
        public float End => startTime + Duration;

        public void Validate()
        {
            startTime = Clean(startTime); enterDuration = Clean(enterDuration); holdDuration = Clean(holdDuration); exitDuration = Clean(exitDuration);
            stopPosition = new Vector2(Mathf.Clamp01(Clean(stopPosition.x)), Mathf.Clamp01(Clean(stopPosition.y)));
            cameraYaw = Mathf.Clamp(Signed(cameraYaw), -180, 180); cameraPitch = Mathf.Clamp(Signed(cameraPitch), -15, 60);
            cameraDistance = Mathf.Clamp(Clean(cameraDistance), 8, 40); cameraFov = Mathf.Clamp(Clean(cameraFov), 20, 60);
            busScale = Mathf.Clamp(Clean(busScale), .2f, 2); riderHopHeight = Mathf.Clamp(Clean(riderHopHeight), 0, .4f);
            riderHopFrequency = Mathf.Clamp(Clean(riderHopFrequency), 0, 5); riderHopStagger = Clean(riderHopStagger);
            busBounceAmplitude = Mathf.Clamp(Clean(busBounceAmplitude), 0, .2f);
            enterStart = FinitePoint(enterStart); enterArcControl = FinitePoint(enterArcControl);
            exitEnd = FinitePoint(exitEnd); exitArcControl = FinitePoint(exitArcControl);
            pathTilt = Mathf.Clamp(Clean(pathTilt), 0, 15); waveArmRaise = Mathf.Clamp(Clean(waveArmRaise), 0, 30);
            waveSpeed = Mathf.Clamp(Clean(waveSpeed), 0, 3); waveAmplitude = Mathf.Clamp(Clean(waveAmplitude), 0, 30);
        }
        private static Vector2 FinitePoint(Vector2 value) => new Vector2(Signed(value.x), Signed(value.y));

        public static Vector2 Arc(Vector2 start, Vector2 control, Vector2 end, float t)
            => (1 - t) * (1 - t) * start + 2 * (1 - t) * t * control + t * t * end;

        public static Vector2 ArcTangent(Vector2 start, Vector2 control, Vector2 end, float t)
            => 2 * ((1 - t) * (control - start) + t * (end - control));
        private static float Signed(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : value;
        private static float Clean(float value) => Mathf.Max(0, Signed(value));
    }
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
        [SerializeField, Tooltip("Side-view enter, stop, celebrate and exit controls. Changes are visible when scrubbing the preview.")]
        private CelebrationChoreography celebration = new CelebrationChoreography();
        public CelebrationChoreography Celebration => celebration;
        [NonSerialized] private ResultStep[] lookup;
        [NonSerialized] private readonly float[] lengths = new float[4];
        public IReadOnlyList<ResultStep> Steps => steps;
        public int Revision { get; private set; }
        public string ValidationMessage { get; private set; }
        public float Length(ResultTrack track) => lengths[(int)track];

        public ResultStep Get(ResultStepId id)
        {
            var step = lookup[(int)id];
            if (id == ResultStepId.CelebrationBus)
            { step.start = celebration.startTime; step.duration = celebration.Duration; }
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
            if (celebration == null) celebration = new CelebrationChoreography();
            celebration.Validate();
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
                if (step.id == ResultStepId.CelebrationBus)
                { step.start = celebration.startTime; step.duration = celebration.Duration; step.ease = ResultEase.Linear; step.stagger = 0; }
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
            S(ResultStepId.Confetti, .3f, 1f, stagger: .009f),
            S(ResultStepId.Overlay, 1.05f, .2f), S(ResultStepId.CelebrationBus, 1.05f, 2.4f),
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
