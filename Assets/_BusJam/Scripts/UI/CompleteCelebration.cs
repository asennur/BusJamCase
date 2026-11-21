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
    /// <summary>One pooled 3D bus pass, sampled by the existing unscaled DOTween result timeline.</summary>
    public sealed class CompleteCelebration : IDisposable
    {
        public const float StartTime = 1.05f, Duration = 1.05f;
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
        private readonly float[] riderHeights = new float[3];
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

        public CompleteCelebration(CelebrationSettings assets, Image output, UnityEngine.SceneManagement.Scene scene, Camera sourceCamera = null)
        {
            settings = assets; image = output;
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
            bus.localRotation = Quaternion.Euler(0, 135, 0);
            for (int i = 0; i < riders.Length; i++)
            {
                Passenger passenger = Object.Instantiate(settings.passengerPrefab, cast.transform);
                passenger.enabled = false; passenger.PrepareCelebration();
                riders[i] = passenger.transform;
                Vector3 worldScale = riders[i].lossyScale;
                riders[i].SetParent(vehicle.PresentationSeat(i), false);
                riders[i].rotation = Quaternion.Euler(0, 175 + (i - 1) * 12, 0);
                riderRotations[i] = riders[i].localRotation;
                Vector3 stretch = riders[i].lossyScale;
                riderScales[i] = new Vector3(worldScale.x / stretch.x, worldScale.y / stretch.y, worldScale.z / stretch.z) * 1.65f;
                riders[i].localScale = riderScales[i];
                riderBodies[i] = passenger.PresentationBody;
                waveRoots[i] = passenger.PresentationAnimator.gameObject;
                Transform spine = waveRoots[i].transform.Find("YolcuRig/Root/Hips/Spine");
                upperArms[i] = spine.Find("UpperArm.R"); lowerArms[i] = upperArms[i].Find("LowerArm.R");
                heads[i] = spine.Find("Head"); thighs[i] = spine.parent.Find("Thigh.R");
                settings.idle.SampleAnimation(waveRoots[i], 0);
                riderHeights[i] = riderBodies[i].bounds.size.y;
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
            camera.cullingMask = 1 << stage.layer; camera.fieldOfView = settings.fieldOfView;
            camera.nearClipPlane = .1f; camera.farClipPlane = 60; camera.allowHDR = false;
            camera.allowMSAA = true; camera.useOcclusionCulling = false; camera.depth = 10;
            camera.transform.localRotation = Quaternion.Euler(settings.cameraPitch, 0, 0);
            camera.transform.localPosition = -(camera.transform.forward * 18);
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
            halfHeight = Mathf.Tan(settings.fieldOfView * .5f * Mathf.Deg2Rad) * 18;
            halfWidth = halfHeight * camera.aspect;
            float scale = Mathf.Min(halfWidth * .20f, halfHeight * .15f);
            cast.transform.localScale = Vector3.one * scale;
            radius = scale * 5.5f;
        }

        public void Sample(float timeline)
        {
            float elapsed = timeline - StartTime;
            if (elapsed < 0 || elapsed >= Duration) { Hide(); return; }
            if (!camera.enabled)
            { cast.SetActive(true); camera.enabled = key.enabled = true; image.style.display = DisplayStyle.Flex; }
            float p = elapsed / Duration;
            Vector3 origin = camera.transform.position + camera.transform.forward * 18;
            // Fast entrance, readable central drift, then a full left exit within the same pass.
            float x = p < .24f ? Mathf.Lerp(halfWidth + radius, halfWidth * .14f, 1 - Mathf.Pow(1 - p / .24f, 3))
                : p < .7f ? Mathf.Lerp(halfWidth * .14f, 0, (p - .24f) / .46f)
                : Mathf.Lerp(0, -halfWidth - radius, Mathf.Pow((p - .7f) / .3f, 3));
            float suspension = Mathf.Sin(elapsed * 19);
            float y = -halfHeight * .66f + suspension * cast.transform.localScale.y * .06f;
            cast.transform.position = origin + camera.transform.right * x + camera.transform.up * y;
            cast.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 13) * 3.5f);
            settings.drive.SampleAnimation(driveRoot, elapsed % settings.drive.length);
            settings.exhaust.SampleAnimation(driveRoot, elapsed % settings.exhaust.length);
            for (int i = 0; i < riders.Length; i++)
            {
                settings.wave.SampleAnimation(waveRoots[i], (elapsed + i * .13f) % settings.wave.length);
                Quaternion upper = upperArms[i].localRotation, lower = lowerArms[i].localRotation;
                // Keep Wave on the raised arm and standing Idle on the remaining rig.
                settings.idle.SampleAnimation(waveRoots[i], (elapsed + i * .13f) % settings.idle.length);
                upperArms[i].localRotation = upper; lowerArms[i].localRotation = lower;
                float cycle = Mathf.Repeat(elapsed / .43f + i * .24f, 1);
                float passengerHeight = riderHeights[i] * cast.transform.localScale.y;
                float hop = cycle < .78f ? Mathf.Sin(cycle / .78f * Mathf.PI) * passengerHeight * .5f : 0;
                float squash = cycle >= .78f ? Mathf.Sin((cycle - .78f) / .22f * Mathf.PI) * .14f : 0;
                riders[i].position = riders[i].parent.position + bus.up * (hop + .06f) + (i == 2 ? bus.right * .4f : Vector3.zero);
                riders[i].localRotation = riderRotations[i] * Quaternion.Euler(i == 2 ? -38 : Mathf.Sin(cycle * Mathf.PI * 2) * 9, 0, i == 2 ? -15 : 0);
                riders[i].localScale = Vector3.Scale(riderScales[i], new Vector3(1 + squash * .5f, 1 - squash, 1 + squash * .5f));
                Vector3 raisedDirection = bus.up + camera.transform.right * (.4f + Mathf.Sin(elapsed * 16 + i) * .18f);
                upperArms[i].rotation = Quaternion.FromToRotation(upperArms[i].up, raisedDirection) * upperArms[i].rotation;
                lowerArms[i].rotation = Quaternion.FromToRotation(lowerArms[i].up, raisedDirection) * lowerArms[i].rotation;
                // Lengthen the existing raised arm for a clear silhouette above the large head.
                upperArms[i].localScale = new Vector3(1, 1.7f, 1);
                lowerArms[i].localScale = new Vector3(1, 1.2f, 1);
                heads[i].localScale = Vector3.one * .85f;
                thighs[i].localRotation *= Quaternion.Euler(-hop / passengerHeight * 65, 0, 0);
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
