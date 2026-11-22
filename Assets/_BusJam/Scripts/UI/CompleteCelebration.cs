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
    /// <summary>Havuzdan tek yandan görünüşlü otobüs durağı; mevcut unscaled DOTween sonuç timeline'ı ile örneklenir.</summary>
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
        private readonly Transform[] riders;
        private readonly GameObject[] waveRoots;
        private readonly Renderer[] riderBodies;
        // Tutunma ↔ kutlama geçişi için önceden ayrılmış kemik tamponları (karede allocation yok).
        private readonly Transform[][] riderBones;
        private readonly Vector3[][] bracePositions;
        private readonly Quaternion[][] braceRotations;
        private readonly Renderer busBody;
        private readonly Material[] busMaterials;
        private readonly float modelScale;
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
            CelebrationVehicle vehicle = Object.Instantiate(settings.busPrefab, cast.transform);
            bus = vehicle.transform; bus.name = "Celebration Bus";
            busBody = vehicle.body; busMaterials = busBody.sharedMaterials;
            // Önceki 8.44 birimlik otobüs silüeti genişliği Asen'in ayarladığı Bus Scale altında korunur.
            modelScale = 8.44f / busBody.bounds.size.x;
            driveRoot = vehicle.model.gameObject;
            int count = Mathf.Clamp(settings.riderCount, 1, 2);
            riders = new Transform[count]; waveRoots = new GameObject[count]; riderBodies = new Renderer[count];
            riderBones = new Transform[count][]; bracePositions = new Vector3[count][]; braceRotations = new Quaternion[count][];
            Transform model = vehicle.model.transform;
            for (int i = 0; i < count; i++)
            {
                Passenger passenger = Object.Instantiate(settings.passengerPrefab, cast.transform);
                passenger.enabled = false; passenger.PrepareCelebration();
                riders[i] = passenger.transform;
                Transform seat = i == 0 ? vehicle.frontSeat : vehicle.rearSeat;
                riders[i].SetParent(seat, false);
                // Boy otobüs ölçeğinden bağımsız: ayakta oyun yolcusu × riderScale.
                Vector3 parentScale = seat.lossyScale;
                riders[i].localScale = new Vector3(1 / parentScale.x, 1 / parentScale.y, 1 / parentScale.z) * settings.riderScale;
                // Ofset ve yön otobüs modeli uzayında verilir; koltuk kemiğinin ekseninden bağımsız.
                Vector3 offset = i == 0 ? settings.frontRiderOffset : settings.rearRiderOffset;
                riders[i].position = model.TransformPoint(model.InverseTransformPoint(seat.position) + offset);
                riders[i].rotation = model.rotation * Quaternion.Euler(0, i == 0 ? settings.frontRiderYaw : settings.rearRiderYaw, 0);
                riderBodies[i] = passenger.PresentationBody;
                waveRoots[i] = passenger.PresentationAnimator.gameObject;
                settings.idle.SampleAnimation(waveRoots[i], 0);
                GroundOnDeck((SkinnedMeshRenderer)riderBodies[i], riders[i]);
                riderBones[i] = waveRoots[i].GetComponentsInChildren<Transform>(true);
                bracePositions[i] = new Vector3[riderBones[i].Length]; braceRotations[i] = new Quaternion[riderBones[i].Length];
                Destroy(passenger);
            }
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
            // URP 14 post processing alfayı opak yapar; şeffaf birleştirme ve sıcak ana ışık korunur.
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

        // Idle pozunda ayak tabanı güverte noktasına bir kez hizalanır; sonraki tüm hareket Cheer kliplerine aittir.
        private static void GroundOnDeck(SkinnedMeshRenderer body, Transform rider)
        {
            var mesh = body.sharedMesh;
            var vertices = mesh.vertices; var weights = mesh.boneWeights;
            var bones = body.bones; var binds = mesh.bindposes;
            float bottom = float.PositiveInfinity;
            for (int i = 0; i < vertices.Length; i++)
            {
                var w = weights[i];
                Vector3 point = bones[w.boneIndex0].TransformPoint(binds[w.boneIndex0].MultiplyPoint3x4(vertices[i])) * w.weight0;
                if (w.weight1 > 0) point += bones[w.boneIndex1].TransformPoint(binds[w.boneIndex1].MultiplyPoint3x4(vertices[i])) * w.weight1;
                if (w.weight2 > 0) point += bones[w.boneIndex2].TransformPoint(binds[w.boneIndex2].MultiplyPoint3x4(vertices[i])) * w.weight2;
                if (w.weight3 > 0) point += bones[w.boneIndex3].TransformPoint(binds[w.boneIndex3].MultiplyPoint3x4(vertices[i])) * w.weight3;
                bottom = Mathf.Min(bottom, rider.InverseTransformPoint(point).y);
            }
            rider.localPosition += new Vector3(0, -bottom * rider.localScale.y, 0);
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
            // GPU belleği yalnız otobüs görünürken oluşturulur (bkz. Sample/Hide).
            if (camera.enabled) target.Create();
            camera.targetTexture = target; camera.aspect = (float)width / height; image.image = target;
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
            float scale = Mathf.Min(referenceHeight * .15f, referenceHeight * camera.aspect * .24f) * motion.busScale * modelScale;
            cast.transform.localScale = Vector3.one * scale;
            radius = scale * 7;
        }

        private static float Ease(AnimationCurve curve, float phase, bool normalize = false)
        {
            if (phase <= 0) return 0;
            if (phase >= 1) return 1;
            if (curve == null || curve.length == 0) return phase;
            float value = curve.Evaluate(phase);
            // Yay ileri doğru izlenirken tasarlanmış artan ya da azalan eğri anahtarları korunur.
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
            {
                if (target != null && !target.IsCreated()) { target.Create(); image.MarkDirtyRepaint(); }
                cast.SetActive(true); camera.enabled = key.enabled = true; image.style.display = DisplayStyle.Flex;
            }
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
            float holdTime = elapsed - motion.enterDuration;
            float exitTime = holdTime - motion.holdDuration;
            // Tekerlek hareketi Drive'a aittir; faz klibi süspansiyon ve koltuk kemiklerini üstüne ekler.
            // Arrive girişin son kısmını, Leave kalkışın ilk kısmını kapsar.
            float arriveDuration = Mathf.Min(motion.enterDuration, settings.celebrateArrive.length);
            bool arriving = entering && elapsed >= motion.enterDuration - arriveDuration;
            bool leaving = !entering && !holding && exitTime < settings.celebrateLeave.length;
            if (entering || !holding)
                settings.drive.SampleAnimation(driveRoot, Mathf.Repeat(elapsed, settings.drive.length));
            if (arriving)
                settings.celebrateArrive.SampleAnimation(driveRoot, elapsed - motion.enterDuration + arriveDuration);
            else if (holding)
                settings.celebrateHold.SampleAnimation(driveRoot, Mathf.Repeat(holdTime, settings.celebrateHold.length));
            else if (leaving)
                settings.celebrateLeave.SampleAnimation(driveRoot, exitTime);
            SampleRiders(elapsed, motion.enterDuration - settings.cheerLead, motion.enterDuration + motion.holdDuration + settings.cheerTail);
        }

        // Otobüs yoldayken yolcular çömelip tutunur (Cheer_Lean); durunca ön yolcu zıplar, arka yolcu faz farkıyla el sallar.
        private void SampleRiders(float elapsed, float cheerStart, float cheerEnd)
        {
            float blend = settings.braceBlend;
            float cheer = Mathf.Clamp01((elapsed - cheerStart) / blend) * Mathf.Clamp01((cheerEnd - elapsed) / blend);
            cheer = cheer * cheer * (3 - 2 * cheer);
            AnimationClip brace = settings.cheerLean;
            for (int i = 0; i < riders.Length; i++)
            {
                GameObject root = waveRoots[i];
                if (cheer < 1) brace.SampleAnimation(root, Mathf.Repeat(elapsed + i * .2f, brace.length));
                if (cheer <= 0) continue;
                Transform[] bones = riderBones[i];
                if (cheer < 1)
                    for (int b = 0; b < bones.Length; b++)
                    { bracePositions[i][b] = bones[b].localPosition; braceRotations[i][b] = bones[b].localRotation; }
                AnimationClip clip = i == 0 ? settings.cheerJump : settings.cheerWave;
                float phase = i == 0 ? 0 : settings.rearCheerPhase;
                clip.SampleAnimation(root, Mathf.Repeat(elapsed - cheerStart + phase, clip.length));
                if (cheer < 1)
                    for (int b = 0; b < bones.Length; b++)
                    {
                        bones[b].localPosition = Vector3.LerpUnclamped(bracePositions[i][b], bones[b].localPosition, cheer);
                        bones[b].localRotation = Quaternion.SlerpUnclamped(braceRotations[i][b], bones[b].localRotation, cheer);
                    }
            }
        }

        public void Hide()
        {
            if (camera.enabled) { camera.enabled = key.enabled = false; cast.SetActive(false); }
            image.style.display = DisplayStyle.None;
            // Obje (ve kimliği) tutulur ama tam ekran MSAA yüzeyleri kazanmalar arasında serbest bırakılır.
            if (target != null && target.IsCreated()) target.Release();
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
