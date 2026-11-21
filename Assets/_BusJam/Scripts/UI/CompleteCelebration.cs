using System;
using BusJam.Gameplay;
using BusJam.Level;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace BusJam.UI
{
    /// <summary>One pooled side-view bus stop, sampled by the existing unscaled DOTween result timeline.</summary>
    public sealed class CompleteCelebration : IDisposable
    {
        public const float StartTime = 1.05f, Duration = 2.4f;
        private readonly ResultTimeline timeline;
        private CelebrationChoreography Motion => timeline.Celebration;
        private readonly CelebrationSettings settings;
        private readonly GameObject stage, cast, driveRoot;
        private readonly Camera camera;
        private readonly Camera worldCamera;
        private readonly int worldCameraMask;
        private readonly Light key;
        private readonly Image image;
        private readonly Transform bus;
        private readonly Transform[] riders = new Transform[3];
        private readonly GameObject[] waveRoots = new GameObject[3];
        private readonly Vector3[] riderScales = new Vector3[3];
        private readonly Quaternion[] riderRotations = new Quaternion[3];
        private readonly Transform[] upperArms = new Transform[3], lowerArms = new Transform[3], heads = new Transform[3], thighs = new Transform[3];
        private readonly Transform[] clipUpperArms = new Transform[3], clipLowerArms = new Transform[3];
        private readonly Quaternion[] waveCenters = new Quaternion[3];
        private readonly float[] waveRaises = new float[3];
        private readonly float[] riderHeights = new float[3];
        private readonly Transform[] feet = new Transform[3];
        private readonly Vector3[] footSoles = new Vector3[3], deckPositions = new Vector3[3];
        private readonly Renderer[] riderBodies = new Renderer[3];
        private readonly Renderer busBody;
        private readonly Material[] busMaterials;
        private RenderTexture target;
        private int width, height;
        private float halfWidth, halfHeight, radius;
        public Camera Camera => camera;
        public RenderTexture Target => target;
        public GameObject Stage => stage;
        public bool Visible => camera.enabled;

        public CompleteCelebration(CelebrationSettings assets, Image output, UnityEngine.SceneManagement.Scene scene, Camera sourceCamera = null, ResultTimeline resultTimeline = null)
        {
            settings = assets; image = output; timeline = resultTimeline != null ? resultTimeline : assets.resultTimeline;
            worldCamera = sourceCamera;
            worldCameraMask = worldCamera != null ? worldCamera.cullingMask : 0;
            stage = new GameObject("Complete Celebration Pool") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(stage, scene);
            stage.transform.position = new Vector3(10000, 10000, 10000);
            stage.SetActive(false);
            cast = new GameObject("Celebration Cast"); cast.transform.SetParent(stage.transform, false);
            Bus vehicle = Object.Instantiate(settings.busPrefab, cast.transform);
            vehicle.enabled = false; bus = vehicle.transform;
            bus.name = "Celebration Bus";
            busBody = vehicle.PresentationBody; busMaterials = busBody.sharedMaterials;
            driveRoot = vehicle.GetComponentInChildren<Animator>(true).gameObject;
            bus.localRotation = Quaternion.identity;
            for (int i = 0; i < riders.Length; i++)
            {
                Passenger passenger = Object.Instantiate(settings.passengerPrefab, cast.transform);
                passenger.enabled = false; passenger.PrepareCelebration();
                riders[i] = passenger.transform;
                Vector3 worldScale = riders[i].lossyScale;
                riders[i].SetParent(vehicle.PresentationSeat(i), false);
                riders[i].rotation = Quaternion.Euler(0, 180 + (i - 1) * 10, 0);
                riderRotations[i] = riders[i].localRotation;
                Vector3 stretch = riders[i].lossyScale;
                riderScales[i] = new Vector3(worldScale.x / stretch.x, worldScale.y / stretch.y, worldScale.z / stretch.z) * 1.65f;
                riders[i].localScale = riderScales[i];
                riderBodies[i] = passenger.PresentationBody;
                waveRoots[i] = passenger.PresentationAnimator.gameObject;
                Transform spine = waveRoots[i].transform.Find("YolcuRig/Root/Hips/Spine");
                clipUpperArms[i] = spine.Find("UpperArm.R"); clipLowerArms[i] = clipUpperArms[i].Find("LowerArm.R");
                upperArms[i] = spine.Find(i % 2 == 0 ? "UpperArm.R" : "UpperArm.L");
                lowerArms[i] = upperArms[i].GetChild(0);
                heads[i] = spine.Find("Head"); thighs[i] = spine.parent.Find("Thigh.R");
                feet[i] = spine.parent.Find("Thigh.L/Shin.L/Foot.L");
                settings.wave.SampleAnimation(waveRoots[i], settings.wave.length * .125f);
                waveCenters[i] = clipLowerArms[i].localRotation;
                Vector3 direction = spine.InverseTransformDirection(clipUpperArms[i].up);
                waveRaises[i] = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                settings.idle.SampleAnimation(waveRoots[i], 0);
                footSoles[i] = FindSole((SkinnedMeshRenderer)riderBodies[i], feet[i]);
                riderHeights[i] = StandingHeight((SkinnedMeshRenderer)riderBodies[i], heads[i], feet[i].TransformPoint(footSoles[i]));
                deckPositions[i] = bus.InverseTransformPoint(riders[i].parent.position);
                // Feet stand on the existing deck, in front of the seat backs and inside the rail.
                deckPositions[i] += new Vector3(.3f, 0, .2f);
                deckPositions[i].y = 2.04f;
                Destroy(passenger);
            }
            // Remove gameplay scripts before the inactive clones can Awake or create their own tweens.
            Destroy(vehicle);
            foreach (var animator in cast.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var collider in cast.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var renderer in cast.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                if (renderer is SkinnedMeshRenderer skin)
                { skin.updateWhenOffscreen = true; skin.forceMatrixRecalculationPerRender = true; }
            }
            SetLayer(stage.transform, LayerMask.NameToLayer("Celebration"));
            if (worldCamera != null) worldCamera.cullingMask &= ~(1 << stage.layer);
            var cameraObject = new GameObject("CelebrationCamera"); cameraObject.transform.SetParent(stage.transform, false);
            camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << stage.layer; camera.fieldOfView = Motion.cameraFov;
            camera.nearClipPlane = .1f; camera.farClipPlane = 60; camera.allowHDR = false;
            camera.allowMSAA = true; camera.useOcclusionCulling = false; camera.depth = 10;
            camera.transform.localRotation = Quaternion.Euler(Motion.cameraPitch, Motion.cameraYaw, 0);
            camera.transform.localPosition = -(camera.transform.forward * Motion.cameraDistance);
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            // URP 14 post processing forces opaque alpha; keep transparent compositing and the warm key.
            data.renderPostProcessing = false; data.renderShadows = false;
            data.requiresColorOption = data.requiresDepthOption = CameraOverrideOption.Off;
            data.volumeLayerMask = 0;
            var lightObject = new GameObject("Celebration Warm Key"); lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(35, -25, 0);
            key = lightObject.AddComponent<Light>(); key.type = LightType.Directional;
            key.color = settings.keyColor; key.intensity = settings.keyIntensity;
            key.cullingMask = camera.cullingMask; key.shadows = LightShadows.None; key.enabled = false;
            lightObject.layer = cameraObject.layer = stage.layer;
            cast.SetActive(false); stage.SetActive(true);
            image.style.display = DisplayStyle.None;
        }

        private static void SetLayer(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            for (int i = 0; i < node.childCount; i++) SetLayer(node.GetChild(i), layer);
        }

        // Use the posed bones directly: a newly cloned inactive skin can return stale BakeMesh data.
        private static float StandingHeight(SkinnedMeshRenderer body, Transform head, Vector3 sole)
        {
            Mesh mesh = body.sharedMesh;
            int bone = Array.IndexOf(body.bones, head);
            Matrix4x4 bind = mesh.bindposes[bone];
            Vector3[] vertices = mesh.vertices; BoneWeight[] weights = mesh.boneWeights;
            float top = float.NegativeInfinity;
            for (int i = 0; i < vertices.Length; i++)
                if (weights[i].boneIndex0 == bone && weights[i].weight0 > .99f)
                    top = Mathf.Max(top, head.TransformPoint(bind.MultiplyPoint3x4(vertices[i])).y);
            return top - sole.y;
        }

        // Cache one supporting sole from the existing foot vertices; no mesh reads in Sample.
        private static Vector3 FindSole(SkinnedMeshRenderer body, Transform foot)
        {
            Mesh mesh = body.sharedMesh;
            int bone = Array.IndexOf(body.bones, foot);
            Matrix4x4 bind = mesh.bindposes[bone];
            Vector3[] vertices = mesh.vertices;
            BoneWeight[] weights = mesh.boneWeights;
            Vector3 sole = Vector3.zero;
            float lowest = float.PositiveInfinity;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (weights[i].boneIndex0 != bone || weights[i].weight0 < .99f) continue;
                Vector3 point = bind.MultiplyPoint3x4(vertices[i]);
                float y = foot.TransformPoint(point).y;
                if (y < lowest) { lowest = y; sole = point; }
            }
            return sole;
        }

        public void Prepare(LevelData level)
        {
            Material material = settings.colors[(int)LastColor(level)];
            busMaterials[0] = material; busBody.sharedMaterials = busMaterials;
            for (int i = 0; i < riderBodies.Length; i++) riderBodies[i].sharedMaterial = material;
            Hide();
        }

        public static PassengerColor LastColor(LevelData level) => level != null && level.busOrder.Count > 0
            ? level.busOrder[level.busOrder.Count - 1] : PassengerColor.Red;

        public void Resize(int newWidth, int newHeight)
        {
            newWidth = Mathf.Max(1, newWidth); newHeight = Mathf.Max(1, newHeight);
            if (width == newWidth && height == newHeight && target != null) return;
            ReleaseTarget(); width = newWidth; height = newHeight;
            target = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            { name = "Celebration Screen Target", hideFlags = HideFlags.HideAndDontSave, antiAliasing = 2 };
            target.Create(); camera.targetTexture = target; camera.aspect = (float)width / height; image.image = target;
        }

        private void Frame()
        {
            var motion = Motion;
            camera.fieldOfView = motion.cameraFov;
            camera.transform.localRotation = Quaternion.Euler(motion.cameraPitch, motion.cameraYaw, 0);
            camera.transform.localPosition = -camera.transform.forward * motion.cameraDistance;
            halfHeight = Mathf.Tan(motion.cameraFov * .5f * Mathf.Deg2Rad) * motion.cameraDistance;
            halfWidth = halfHeight * camera.aspect;
            float referenceHeight = Mathf.Tan(35 * .5f * Mathf.Deg2Rad) * 18;
            float scale = Mathf.Min(referenceHeight * .15f, referenceHeight * camera.aspect * .24f) * motion.busScale;
            cast.transform.localScale = Vector3.one * scale;
            radius = scale * 7;
        }

        private static float Ease(AnimationCurve curve, float phase, bool normalize = false)
        {
            if (phase <= 0) return 0;
            if (phase >= 1) return 1;
            if (curve == null || curve.length == 0) return phase;
            float value = curve.Evaluate(phase);
            // Preserve authored ascending or descending curve keys while traversing the arc forward.
            if (normalize)
            {
                float start = curve.Evaluate(0), range = curve.Evaluate(1) - start;
                value = Mathf.Abs(range) > .00001f ? (value - start) / range : phase;
            }
            return Mathf.Clamp01(value);
        }

        private Vector2 ScreenPoint(Vector2 point) => new Vector2((point.x * 2 - 1) * halfWidth, (point.y * 2 - 1) * halfHeight);

        public void Sample(float time)
        {
            var motion = Motion;
            float elapsed = time - motion.startTime;
            if (elapsed < 0 || elapsed >= motion.Duration) { Hide(); return; }
            Frame();
            if (!camera.enabled)
            { cast.SetActive(true); camera.enabled = key.enabled = true; image.style.display = DisplayStyle.Flex; }
            Vector3 origin = stage.transform.position;
            Vector2 stop = ScreenPoint(motion.stopPosition);
            Vector2 point = stop;
            Vector2 tangent = Vector2.left;
            bool arc = motion.pathMode == CelebrationPathMode.Arc;
            float moving = 0;
            bool entering = elapsed < motion.enterDuration;
            bool holding = !entering && elapsed < motion.enterDuration + motion.holdDuration;
            if (entering)
            {
                float p = elapsed / motion.enterDuration;
                float edge = motion.enterSide == CelebrationEnterSide.Left ? -halfWidth - radius : halfWidth + radius;
                float progress = Ease(motion.enterEase, p, arc);
                if (arc)
                {
                    Vector2 start = ScreenPoint(motion.enterStart), control = ScreenPoint(motion.enterArcControl);
                    point = CelebrationChoreography.Arc(start, control, stop, progress);
                    tangent = CelebrationChoreography.ArcTangent(start, control, stop, progress);
                }
                else point.x = Mathf.Lerp(edge, stop.x, progress);
                moving = 1 - p;
            }
            else if (!holding)
            {
                float p = motion.exitDuration > 0 ? (elapsed - motion.enterDuration - motion.holdDuration) / motion.exitDuration : 1;
                Vector2 exit = motion.exitDirection == CelebrationExitDirection.Down ? new Vector2(stop.x, -halfHeight - radius)
                    : new Vector2(motion.exitDirection == CelebrationExitDirection.Left ? -halfWidth - radius : halfWidth + radius, stop.y);
                float progress = Ease(motion.exitEase, p, arc);
                if (arc)
                {
                    Vector2 end = ScreenPoint(motion.exitEnd), control = ScreenPoint(motion.exitArcControl);
                    point = CelebrationChoreography.Arc(stop, control, end, progress);
                    tangent = CelebrationChoreography.ArcTangent(stop, control, end, progress);
                }
                else point = Vector2.Lerp(stop, exit, progress);
                moving = p;
            }
            point.y += Mathf.Sin(elapsed * 19) * cast.transform.localScale.y * motion.busBounceAmplitude * moving;
            cast.transform.position = origin + camera.transform.right * point.x + camera.transform.up * point.y;
            float tilt = arc ? Mathf.Clamp(Mathf.Atan2(-tangent.y, Mathf.Abs(tangent.x)) * Mathf.Rad2Deg, -motion.pathTilt, motion.pathTilt) * moving : 0;
            cast.transform.rotation = Quaternion.AngleAxis(tilt, camera.transform.forward);
            // Freeze the driving pose during the stop; exhaust and riders keep their own clocks.
            float driveTime = entering ? elapsed : holding ? motion.enterDuration : elapsed - motion.holdDuration;
            settings.drive.SampleAnimation(driveRoot, driveTime % settings.drive.length);
            settings.exhaust.SampleAnimation(driveRoot, elapsed % settings.exhaust.length);
            float holdTime = Mathf.Clamp(elapsed - motion.enterDuration, 0, motion.holdDuration);
            for (int i = 0; i < riders.Length; i++)
            {
                float riderTime = Mathf.Max(0, holdTime - i * motion.riderHopStagger);
                settings.wave.SampleAnimation(waveRoots[i], riderTime * motion.waveSpeed % settings.wave.length);
                Quaternion upper = Quaternion.AngleAxis(motion.waveArmRaise - waveRaises[i], Vector3.forward) * clipUpperArms[i].localRotation;
                Quaternion lower = Quaternion.SlerpUnclamped(waveCenters[i], clipLowerArms[i].localRotation, motion.waveAmplitude / 25);
                settings.idle.SampleAnimation(waveRoots[i], riderTime % settings.idle.length);
                if (i % 2 != 0)
                {
                    upper = new Quaternion(upper.x, -upper.y, -upper.z, upper.w);
                    lower = new Quaternion(lower.x, -lower.y, -lower.z, lower.w);
                }
                upperArms[i].localRotation = upper; lowerArms[i].localRotation = lower;
                float cycle = Mathf.Repeat(riderTime * motion.riderHopFrequency, 1);
                float passengerHeight = riderHeights[i] * cast.transform.localScale.y;
                // Smooth takeoff/landing and a grounded interval; envelope grounds both hold boundaries.
                float envelope = holding ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(holdTime * 10))
                    * Mathf.SmoothStep(0, 1, Mathf.Clamp01((motion.holdDuration - holdTime) * 10)) : 0;
                float lift = cycle < .36f ? Mathf.Pow(Mathf.Sin(cycle / .36f * Mathf.PI), 2) : 0;
                float hop = lift * passengerHeight * motion.riderHopHeight * envelope;
                float squash = holding && cycle >= .36f && cycle < .5f ? Mathf.Sin((cycle - .36f) / .14f * Mathf.PI) * .07f * envelope : 0;
                riders[i].localRotation = riderRotations[i] * Quaternion.Euler(holding ? Mathf.Sin(cycle * Mathf.PI * 2) * 5 * envelope : 0, 0, 0);
                riders[i].localScale = Vector3.Scale(riderScales[i], new Vector3(1 + squash * .5f, 1 - squash, 1 + squash * .5f));
                thighs[i].localRotation *= Quaternion.Euler(-hop / passengerHeight * 65, 0, 0);
                riders[i].position = bus.TransformPoint(deckPositions[i]);
                float soleHeight = Vector3.Dot(feet[i].TransformPoint(footSoles[i]) - riders[i].position, bus.up);
                riders[i].position += bus.up * (hop - soleHeight);
            }
        }

        public void Hide()
        {
            if (camera.enabled) { camera.enabled = key.enabled = false; cast.SetActive(false); }
            image.style.display = DisplayStyle.None;
        }

        private void ReleaseTarget()
        {
            image.image = null; camera.targetTexture = null;
            if (target == null) return;
            target.Release(); Destroy(target); target = null;
        }

        private static void Destroy(Object value)
        { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }

        public void Dispose()
        {
            Hide(); ReleaseTarget();
            if (worldCamera != null) worldCamera.cullingMask = worldCameraMask;
            Destroy(stage);
        }
    }
}
