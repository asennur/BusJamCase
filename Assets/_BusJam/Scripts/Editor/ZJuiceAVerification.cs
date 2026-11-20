using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BusJam.Gameplay;
using BusJam.Level;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace BusJam.EditorTools
{
    /// <summary>Runs production bus sequences in a disposable preview scene without entering Play.</summary>
    public static class ZJuiceAVerification
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Output = "Docs/ZJUICE-A-Captures";
        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
        private static void Invoke(object target, string name) => target.GetType().GetMethod(name, Flags).Invoke(target, null);
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable] private sealed class Results
        {
            public string unityVersion;
            public bool editMode, arrivalGate, bufferedAtDoor, poolReset, staleBufferCancelled, finalCompletionImmediate;
            public float oldCycle, newCycle, departureStart, departureEnd, nextStart, nextArrival, approach;
            public float overlap, minBusGap, maxPitch, wheelDegrees, firstQueueStart, secondQueueStart;
            public float minScaleY = 1f, maxBob, minBob, approachProgress;
            public int approached, arrived, departed, seatsBeforeDoor, seatsAfterDoor, automaticRunners;
            public string[] checks;
        }

        private sealed class Fixture : IDisposable
        {
            public readonly Scene scene;
            public readonly GameObject root;
            public readonly BusStation station;
            public readonly List<Bus> buses = new List<Bus>();
            public readonly List<Passenger> passengers = new List<Passenger>();
            public readonly Camera camera;
            public readonly Material[] materials;
            public float time;
            private readonly UpdateType previousUpdate;
            private readonly UnityEngine.Random.State previousRandom;

            public Fixture()
            {
                Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit mode required.");
                previousUpdate = DOTween.defaultUpdateType;
                previousRandom = UnityEngine.Random.state;
                DOTween.Init();
                DOTween.defaultUpdateType = UpdateType.Manual;
                scene = EditorSceneManager.NewPreviewScene();
                root = new GameObject("ZJUICE-A disposable fixture");
                SceneManager.MoveGameObjectToScene(root, scene);
                var source = UnityEngine.Object.FindObjectOfType<BusStation>();
                Check(source != null, "Open Game or LevelEditor first.");
                station = UnityEngine.Object.Instantiate(source, root.transform);
                materials = Get<Material[]>(station, "colorMaterials");
                var prefab = Get<Bus>(station, "busPrefab");
                Set(station, "busPool", new ObjectPool<Bus>(() =>
                {
                    var bus = UnityEngine.Object.Instantiate(prefab, station.transform);
                    buses.Add(bus);
                    var animator = Get<Animator>(bus, "animator");
                    animator.Rebind(); animator.Update(0); animator.enabled = false;
                    Invoke(bus, "Awake");
                    return bus;
                }, b => b.gameObject.SetActive(true), b => b.gameObject.SetActive(false)));
                var cameraObject = new GameObject("Bus handoff camera");
                cameraObject.transform.SetParent(root.transform);
                camera = cameraObject.AddComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 2.9f;
                camera.aspect = 2.6f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.10f, .13f, .21f);
                Vector3 door = Get<Transform>(station, "doorPoint").position;
                camera.transform.position = door + new Vector3(-.8f, 10f, -7f);
                camera.transform.LookAt(door + Vector3.left * .8f);
                var lightObject = new GameObject("Fixture light");
                lightObject.transform.SetParent(root.transform);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            }

            public Passenger Passenger(PassengerColor color)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<Passenger>("Assets/_BusJam/Prefabs/Passenger.prefab");
                var passenger = UnityEngine.Object.Instantiate(prefab, root.transform);
                passengers.Add(passenger);
                var animator = Get<Animator>(passenger, "animator");
                if (animator != null) { animator.Rebind(); animator.Update(0); animator.enabled = false; }
                Invoke(passenger, "Awake");
                passenger.Setup(color, Vector2Int.zero, false, materials[(int)color]);
                return passenger;
            }

            public void Fill(Bus bus)
            {
                for (int i = 0; i < 3; i++)
                {
                    var p = Passenger(bus.Color);
                    p.SetSeated(bus.ReserveSeat(out int index));
                    station.NotifySeatArrived(bus, index, p);
                }
            }

            public void Step(float dt)
            {
                time += dt;
                DOTween.ManualUpdate(dt, dt);
                foreach (var bus in buses)
                {
                    if (!bus.gameObject.activeSelf) continue;
                    Get<Animator>(bus, "animator").Update(dt);
                    Invoke(bus, "LateUpdate");
                }
                foreach (var passenger in passengers)
                    if (passenger != null && passenger.gameObject.activeInHierarchy)
                        Get<Animator>(passenger, "animator")?.Update(dt);
            }

            public void Until(float target)
            {
                while (time < target - .00001f) Step(Mathf.Min(.001f, target - time));
            }

            public Texture2D Capture()
            {
                var target = RenderTexture.GetTemporary(780, 300, 24);
                var previous = RenderTexture.active;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                return texture;
            }

            public void Dispose()
            {
                foreach (var passenger in passengers)
                    if (passenger != null) UnityEngine.Object.DestroyImmediate(passenger.gameObject);
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(scene);
                DOTween.defaultUpdateType = previousUpdate;
                UnityEngine.Random.state = previousRandom;
            }
        }

        public static string Run()
        {
            var result = new Results { unityVersion = Application.unityVersion, editMode = !Application.isPlaying,
                oldCycle = .16666667f + .3f + .3f + .3f, minBusGap = float.MaxValue };
            Directory.CreateDirectory(Output);
            using (var f = new Fixture())
            {
                f.station.Setup(new List<PassengerColor> { PassengerColor.Red, PassengerColor.Blue, PassengerColor.Green, PassengerColor.Yellow });
                f.Until(.4f);
                Check(f.station.ActiveBus.IsAtDoor, "Initial bus must reach the door.");
                var leaving = f.station.ActiveBus;
                var next = f.station.NextBus;
                var tail = Get<List<Bus>>(f.station, "queue")[2];
                Vector3 nextStart = next.transform.position, tailStart = tail.transform.position;
                float start = f.time;
                f.station.BusDeparting += b => result.departureStart = f.time - start;
                f.station.BusDeparted += b =>
                {
                    result.departed++; result.departureEnd = f.time - start;
                    foreach (var passenger in b.SeatedPassengers) passenger.gameObject.SetActive(false);
                    f.station.ReleaseBus(b);
                };
                f.station.BusApproaching += b =>
                {
                    result.approached++; result.approach = f.time - start;
                    result.arrivalGate = !b.IsAtDoor && b.CanApproach;
                    result.approachProgress = 1f - Mathf.Abs(b.transform.position.x - Get<Transform>(f.station, "doorPoint").position.x)
                        / Get<float>(f.station, "queueSpacing");
                };
                f.station.BusArrived += b => { result.arrived++; result.nextArrival = f.time - start; };
                f.Fill(leaving);
                var strip = new Texture2D(780, 1500, TextureFormat.RGB24, false);
                float[] times = { .04f, .22f, .32f, .40f, .50f };
                int frame = 0;
                for (int step = 1; step <= 550; step++)
                {
                    f.Step(.001f);
                    float elapsed = f.time - start;
                    if (result.nextStart == 0 && Vector3.Distance(nextStart, next.transform.position) > .00001f) result.nextStart = elapsed;
                    if (result.firstQueueStart == 0 && Vector3.Distance(tailStart, tail.transform.position) > .00001f) result.firstQueueStart = elapsed;
                    var q = Get<List<Bus>>(f.station, "queue");
                    if (result.secondQueueStart == 0 && Get<bool>(q[2], "motionActive")) result.secondQueueStart = elapsed;
                    if (elapsed < .38f) result.minBusGap = Mathf.Min(result.minBusGap, leaving.transform.position.x - next.transform.position.x);
                    if (elapsed < .08f) result.minScaleY = Mathf.Min(result.minScaleY,
                        Get<Transform>(leaving, "model").localScale.y / Get<Vector3>(leaving, "modelRestScale").y);
                    result.maxPitch = Mathf.Max(result.maxPitch, Quaternion.Angle(Get<Quaternion>(next, "modelRestRotation"), Get<Transform>(next, "model").localRotation));
                    float bob = Get<Transform>(next, "model").localPosition.y - Get<Vector3>(next, "modelRestPosition").y;
                    result.maxBob = Mathf.Max(result.maxBob, bob); result.minBob = Mathf.Min(result.minBob, bob);
                    Check(next.transform.position.x <= Get<Transform>(f.station, "doorPoint").position.x + .0001f, "Arrival overshoot.");
                    if (frame < times.Length && elapsed + .0001f >= times[frame])
                    {
                        var image = f.Capture();
                        strip.SetPixels(0, (4 - frame) * 300, 780, 300, image.GetPixels());
                        UnityEngine.Object.DestroyImmediate(image);
                        frame++;
                    }
                }
                strip.Apply(); File.WriteAllBytes(Output + "/Bus-Handoff-5-Frames.png", strip.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(strip);
                result.newCycle = result.nextArrival;
                result.overlap = (result.departureEnd - result.nextStart) / (result.departureEnd - result.departureStart);
                result.wheelDegrees = Get<float>(next, "wheelAngle");
                Check(result.newCycle <= result.oldCycle && result.overlap >= .595f, "Cycle/overlap target failed.");
                Check(result.approached == 1 && result.arrived == 1 && result.arrivalGate, "Arrival event order failed.");
                Check(Mathf.Abs(result.firstQueueStart - result.nextStart - .04f) < .002f, "Queue stagger failed.");
                Check(Mathf.Abs(result.secondQueueStart - result.firstQueueStart - .04f) < .002f, "Tail stagger failed.");
                Check(result.maxPitch >= 2.9f && result.maxPitch <= 3.1f, "Pitch out of range.");
                Check(result.minBusGap > 4.12f, "Bus bodies overlap.");
                Check(Mathf.Abs(result.minScaleY - .95f) < .001f, "Anticipation squash missing.");
                Check(result.maxBob > .024f && result.minBob < -.024f, "Suspension bob missing.");
                Check(result.approachProgress >= .85f && result.approachProgress < .86f, "Approach threshold incorrect.");
                Check(Mathf.Abs(result.wheelDegrees - 9.6f / .24f * Mathf.Rad2Deg) < .1f, "Wheel speed must follow distance.");
            }
            VerifyBuffer(result);
            VerifyBufferReset(result);
            VerifyFinalAndReset(result);
            result.checks = new[] { "0.08 s anticipation", "InQuad departure / OutCubic arrival", "60% overlap",
                "0.04 s queue stagger", "3 degree child pitch", "wheel distance integration", "one approach and one arrival event",
                "slot runners at 85%", "no pre-door boarding", "buffer flush once", "reset cancels buffered boarding",
                "pool/reset cancels stale work", "final event remains immediate" };
            string json = JsonUtility.ToJson(result, true);
            File.WriteAllText(Output + "/Verification.json", json);
            return json;
        }

        private static void VerifyBuffer(Results result)
        {
            using (var f = new Fixture())
            {
                f.station.Setup(new List<PassengerColor> { PassengerColor.Blue });
                var bus = f.station.ActiveBus;
                var waitingSource = UnityEngine.Object.FindObjectOfType<WaitingArea>();
                var waiting = UnityEngine.Object.Instantiate(waitingSource, f.root.transform);
                waiting.Setup(1);
                var p = f.Passenger(PassengerColor.Blue);
                waiting.TryReserveSlot(p, out Vector3 slot);
                // Zero-length approach deliberately forces the passenger to reach the buffer first.
                p.transform.position = Get<Transform>(f.station, "doorPoint").position
                    + bus.DoorEntry.position - bus.transform.position;
                p.SetInSlot(waiting.WaitingRotation, 1f);
                f.station.BusApproaching += b => result.automaticRunners += waiting.SendMatchingPassengers(b, (b2, seat, passenger) =>
                {
                    Check(b2.IsAtDoor, "Seat callback before physical arrival.");
                    result.seatsAfterDoor++;
                    f.station.NotifySeatArrived(b2, seat, passenger);
                });
                f.Until(.18f);
                result.seatsBeforeDoor = Get<int>(bus, "arrivedCount");
                result.bufferedAtDoor = Get<Passenger[]>(bus, "bufferedPassengers")[0] == p;
                Check(result.automaticRunners == 1 && result.bufferedAtDoor && !bus.IsAtDoor && result.seatsBeforeDoor == 0,
                    "Early slot runner was not buffered.");
                f.Until(.8f);
                Check(bus.IsAtDoor && result.seatsAfterDoor == 1 && Get<int>(bus, "arrivedCount") == 1, "Buffered boarding did not flush exactly once.");
                bus.SetAtDoor(true); f.Until(1f);
                Check(result.seatsAfterDoor == 1, "Duplicate buffer flush.");
            }
        }

        private static void VerifyFinalAndReset(Results result)
        {
            using (var f = new Fixture())
            {
                f.station.Setup(new List<PassengerColor> { PassengerColor.Red, PassengerColor.Green });
                f.Until(.4f);
                f.Fill(f.station.ActiveBus);
                f.Until(.45f);
                f.station.Setup(new List<PassengerColor> { PassengerColor.Blue });
                f.Until(.8f);
                var bus = f.station.ActiveBus;
                result.poolReset = bus.Color == PassengerColor.Blue && bus.IsAtDoor
                    && Get<List<Bus>>(f.station, "departing").Count == 0 && Get<int>(bus, "arrivedCount") == 0;
                Check(result.poolReset, "Reset retained departure or seat state.");
                int complete = 0, sound = 0;
                f.station.AllBusesDone += () => complete++;
                f.station.BusDeparting += b => sound++;
                f.Fill(bus);
                result.finalCompletionImmediate = complete == 1;
                Check(result.finalCompletionImmediate, "Final completion was delayed.");
                f.Until(1.3f);
                Check(complete == 1 && sound == 0, "Final event or silent departure changed.");
                f.station.Setup(new List<PassengerColor>());
                Check(f.station.ActiveBus == null, "Empty order must release the queue.");
            }
        }

        private static void VerifyBufferReset(Results result)
        {
            using (var f = new Fixture())
            {
                f.station.Setup(new List<PassengerColor> { PassengerColor.Blue });
                f.Until(.18f);
                var bus = f.station.ActiveBus;
                var passenger = f.Passenger(PassengerColor.Blue);
                bus.ReserveSeat(out int seat);
                int callbacks = 0;
                bus.BoardWhenReady(seat, passenger, () => callbacks++);
                Check(Get<Passenger[]>(bus, "bufferedPassengers")[seat] == passenger, "Reset fixture did not buffer.");
                f.station.Setup(new List<PassengerColor> { PassengerColor.Red });
                // A coarse frame also exercises approach and arrival in the same tween tick.
                f.Step(.35f); f.Step(.5f);
                result.staleBufferCancelled = callbacks == 0 && f.station.ActiveBus.IsAtDoor
                    && Get<Passenger[]>(f.station.ActiveBus, "bufferedPassengers")[seat] == null;
                Check(result.staleBufferCancelled, "A reset allowed stale buffered boarding.");
            }
        }
    }
}
