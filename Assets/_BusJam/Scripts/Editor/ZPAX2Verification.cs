using System;
using System.Linq;
using System.Reflection;
using System.Text;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Save;
using BusJam.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Runs deterministic production logic in Edit mode and restores every touched save key.</summary>
    public static class ZPAX2Verification
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int checks;
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
        private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);

        public static string Layout(Scene scene)
        {
            var result = new StringBuilder();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                result.Append(t.GetInstanceID()).Append(':').Append(t.name).Append(':').Append(t.parent == null ? 0 : t.parent.GetInstanceID())
                    .Append(':').Append(t.GetSiblingIndex()).Append(':').Append(t.localPosition.ToString("R"))
                    .Append(':').Append(t.localRotation.ToString("R")).Append(':').Append(t.localScale.ToString("R"));
                if (t is RectTransform r) result.Append(':').Append(r.anchorMin.ToString("R")).Append(':').Append(r.anchorMax.ToString("R"))
                    .Append(':').Append(r.anchoredPosition.ToString("R")).Append(':').Append(r.sizeDelta.ToString("R")).Append(':').Append(r.pivot.ToString("R"));
                result.AppendLine();
            }
            return result.ToString();
        }

        public static string Run()
        {
            ZM3Setup.RequireEditMode();
            checks = 0;
            var keys = new[] { "Lives", "LastRegenUtc", "LivesUtcHighWater", "Coins" };
            var exists = Array.ConvertAll(keys, PlayerPrefs.HasKey);
            var values = Array.ConvertAll(keys, k => k == "Lives" || k == "Coins" ? PlayerPrefs.GetInt(k).ToString() : PlayerPrefs.GetString(k));
            var go = new GameObject("ZPAX2Checks");
            var lives = go.AddComponent<LivesSystem>();
            var summary = new StringBuilder();
            try
            {
                long now = DateTime.UtcNow.Ticks;
                long second = TimeSpan.TicksPerSecond;
                long interval = TimeSpan.TicksPerMinute * 20;
                Action<long> refresh = (Action<long>)Delegate.CreateDelegate(typeof(Action<long>), lives, typeof(LivesSystem).GetMethod("RefreshAt", Private));
                Action<int, long, long> seed = (count, regen, guard) =>
                {
                    PlayerPrefs.SetInt("Lives", count); PlayerPrefs.SetString("LastRegenUtc", regen.ToString()); PlayerPrefs.SetString("LivesUtcHighWater", guard.ToString());
                    ZM3Setup.Set(lives, "initialized", false);
                    Call(lives, "Initialize", now);
                };
                Call(lives, "Initialize", now);
                summary.AppendLine("Actual current save: " + lives.Lives + " lives, next=" + lives.NextLifeSeconds + " seconds.");
                Check(lives.Lives == 5 && lives.NextLifeSeconds == 0, "Cleared production save starts FULL.");
                Check(lives.TrySpendLife() && lives.Lives == 4 && lives.NextLifeSeconds == 1200, "Spending from FULL starts exactly 20 minutes.");
                seed(4, now, now);
                for (int i = 1; i < 1200; i++)
                {
                    refresh(now + i * second);
                    Check(lives.Lives == 4 && lives.NextLifeSeconds == 1200 - i, "Each second must decrease the real countdown.");
                }
                refresh(now + interval);
                Check(lives.Lives == 5 && lives.NextLifeSeconds == 0, "Exact regeneration boundary reaches FULL.");
                seed(0, now - interval * 2 - second * 15, now - interval * 2);
                Check(lives.Lives == 2 && lives.NextLifeSeconds == 1185, "Offline time awards multiple lives and preserves partial time.");
                seed(4, now - interval * 100, now - interval * 100);
                Check(lives.Lives == 5, "Offline award caps at five.");
                seed(4, now, now + TimeSpan.TicksPerDay);
                refresh(now + second);
                Check(lives.NextLifeSeconds == 1199, "Legacy future guard cannot freeze the timer.");
                refresh(now + interval);
                Check(lives.Lives == 5, "Legacy future guard recovers one life after twenty minutes.");
                seed(4, now - interval + second * 145, now + TimeSpan.TicksPerDay);
                Check(lives.NextLifeSeconds == 145, "A valid partial interval survives a stale future guard and restart.");
                refresh(now + second * 145);
                Check(lives.Lives == 5, "A saved 145-second countdown really refills after 145 seconds.");
                seed(4, now + interval, now + interval);
                refresh(now + second);
                Check(lives.NextLifeSeconds == 1199, "Future regeneration timestamp rebases once.");
                seed(1, now, now);
                refresh(now - interval * 3);
                Check(lives.Lives == 1 && lives.NextLifeSeconds == 1200, "Real rollback awards no lives.");
                refresh(now - interval * 3 + second);
                Check(lives.NextLifeSeconds == 1199, "Countdown advances after real rollback.");
                refresh(now);
                Check(lives.Lives == 1 && lives.NextLifeSeconds == 1200, "Forward rebound grants no free lives.");
                refresh(now + interval);
                Check(lives.Lives == 2, "A complete interval after recovery awards one life.");
                PlayerPrefs.SetString("LastRegenUtc", "bad"); PlayerPrefs.SetString("LivesUtcHighWater", "bad"); PlayerPrefs.SetInt("Lives", -7);
                ZM3Setup.Set(lives, "initialized", false); Call(lives, "Initialize", now);
                Check(lives.Lives == 0 && lives.NextLifeSeconds == 1200 && !lives.TrySpendLife(), "Malformed save clamps and recovers; zero cannot be spent.");
                seed(4, now, now);
                int events = 0;
                Action<int, int> changed = (count, seconds) => events++;
                lives.Changed += changed;
                refresh(now + 100);
                Check(events == 0, "Unchanged second emits no event.");
                refresh(now + second);
                Check(events == 1, "New displayed second emits one event.");
                lives.Changed -= changed;
                long bytes = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) refresh(now + second + i);
                Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Steady refresh allocates no bytes.");
                summary.AppendLine("Production logic: 1200 -> 1199 -> ... -> 1 -> FULL; offline/future timestamps/rollback/rebound/malformed save/event/allocation checks passed.");
                Coins();
                Visuals(summary);
                return checks + " Edit checks passed. No Play entered. Save keys restored.\n" + summary;
            }
            finally
            {
                Object.DestroyImmediate(go);
                for (int i = 0; i < keys.Length; i++)
                    if (!exists[i]) PlayerPrefs.DeleteKey(keys[i]);
                    else if (i == 0 || i == 3) PlayerPrefs.SetInt(keys[i], int.Parse(values[i]));
                    else PlayerPrefs.SetString(keys[i], values[i]);
                PlayerPrefs.Save();
            }
        }

        private static void Coins()
        {
            var counters = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CoinCounter>(true)).ToArray();
            var clones = counters.Select(c => Object.Instantiate(c.gameObject)).ToArray();
            try
            {
                foreach (var clone in clones) Call(clone.GetComponent<CoinCounter>(), "OnEnable");
                int before = SaveSystem.Coins;
                SaveSystem.AddCoins(25);
                foreach (var clone in clones)
                {
                    var counter = clone.GetComponent<CoinCounter>();
                    Check(ZM3Setup.Get<TMP_Text>(counter, "countText").text == (before + 25).ToString(), "Every enabled counter updates immediately through CoinsChanged.");
                    Check(counter.GetComponent<LivesDisplay>() == null, "Money plates have no timer component.");
                }
                foreach (var clone in clones) Call(clone.GetComponent<CoinCounter>(), "OnDisable");
                SaveSystem.AddCoins(1);
                foreach (var clone in clones) Check(ZM3Setup.Get<TMP_Text>(clone.GetComponent<CoinCounter>(), "countText").text == (before + 25).ToString(), "Disabled counters unsubscribe.");
                Check(counters.Length >= 2, "Start and Complete both have coin counters.");
            }
            finally { foreach (var clone in clones) Object.DestroyImmediate(clone); }
        }

        private static void Visuals(StringBuilder summary)
        {
            var passenger = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Passenger>("Assets/_BusJam/Prefabs/Passenger.prefab"));
            var bus = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Bus>("Assets/_BusJam/Prefabs/Bus.prefab"));
            try
            {
                var body = (SkinnedMeshRenderer)ZM3Setup.Get<Renderer>(passenger, "body");
                var hull = ZM3Setup.Get<GameObject>(passenger, "outline").GetComponent<SkinnedMeshRenderer>();
                Check(body.sharedMesh == hull.sharedMesh && body.rootBone == hull.rootBone && body.bones.SequenceEqual(hull.bones), "Hull shares the new body mesh and skeleton.");
                var animator = ZM3Setup.Get<Animator>(passenger, "animator");
                Check(animator.runtimeAnimatorController.animationClips.Length == 4, "Four passenger states remain bound.");
                foreach (var clip in animator.runtimeAnimatorController.animationClips) Check(clip.isLooping == (clip.name != "Sit"), "Idle/Run/Wave loop; Sit does not.");
                Vector3 standing = ZM3Setup.Get<Transform>(passenger, "visualRoot").lossyScale;
                var seat = ZM3Setup.Get<Transform[]>(bus, "seatPoints")[1];
                passenger.SetSeated(seat);
                Vector3 seated = ZM3Setup.Get<Transform>(passenger, "visualRoot").lossyScale;
                Check(Vector3.Distance(standing, seated) < .0001f, "Seat parenting preserves the standing world scale.");
                Check(Vector3.Distance(passenger.transform.forward, bus.transform.right) < .01f, "Passenger faces forward along the bus.");
                summary.AppendLine("VisualRoot world scale: standing=" + standing.ToString("F4") + "; seated=" + seated.ToString("F4") + "; seat parent=" + seat.lossyScale.ToString("F4") + ".");
                passenger.EnterBus(ZM3Setup.Get<Transform>(bus, "doorEntry"), seat, () => { });
                var tween = ZM3Setup.Get<DG.Tweening.Tween>(passenger, "runTween");
                DG.Tweening.TweenExtensions.Complete(tween, true);
                Check(Vector3.Distance(standing, ZM3Setup.Get<Transform>(passenger, "visualRoot").lossyScale) < .0001f, "EnterBus grows to the full standing world scale.");
                var busAnimator = ZM3Setup.Get<Animator>(bus, "animator");
                var controller = (AnimatorController)busAnimator.runtimeAnimatorController;
                var exhaust = controller.layers.First(l => l.name == "Exhaust");
                var smoke = (AnimationClip)exhaust.stateMachine.defaultState.motion;
                Check(exhaust.defaultWeight == 1 && smoke.isLooping, "Exhaust loops independently of door/drive states.");
                Check(AnimationUtility.GetCurveBindings(smoke).All(b => b.path.Contains("Smoke_")), "Exhaust layer affects smoke bones only.");
                Check(exhaust.avatarMask != null && !exhaust.stateMachine.defaultState.writeDefaultValues, "Smoke mask cannot overwrite door/seat animation.");
                var smokeBone = bus.GetComponentsInChildren<Transform>().First(t => t.name == "Smoke_0");
                smoke.SampleAnimation(busAnimator.gameObject, .05f); Vector3 first = smokeBone.localScale;
                smoke.SampleAnimation(busAnimator.gameObject, .5f);
                Check(Vector3.Distance(first, smokeBone.localScale) > .1f, "Imported smoke puffs actually animate.");
                Call(bus, "Awake");
                Vector3 rootPosition = bus.transform.position;
                Vector3 entryPosition = bus.DoorEntry.position;
                bus.PlayStop(false); Call(bus, "LateUpdate");
                Check(bus.transform.position == rootPosition && bus.DoorEntry.position == entryPosition, "Engine shake leaves bus root and DoorEntry fixed.");
                var shake = (Action)Delegate.CreateDelegate(typeof(Action), bus, typeof(Bus).GetMethod("LateUpdate", Private));
                shake();
                long bytes = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) shake();
                Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Steady bus shake allocates no bytes.");
                bus.PlayDrive();
                Check(!ZM3Setup.Get<bool>(bus, "idleShake"), "Drive disables idle shake.");
                bus.PlayStop(true);
                Check(!ZM3Setup.Get<bool>(bus, "idleShake"), "Brake disables idle shake.");
                bus.CloseDoor();
                Check(ZM3Setup.Get<bool>(bus, "idleShake"), "Door states retain engine life.");
                var lives = ZM3Setup.InScene<LivesSystem>(SceneManager.GetActiveScene());
                Check(lives.MaxLives == 5 && ZM3Setup.Get<int>(lives, "regenMinutes") == 20, "Production remains five lives / twenty minutes.");
            }
            finally { Object.DestroyImmediate(passenger.gameObject); Object.DestroyImmediate(bus.gameObject); }
        }
    }
}
