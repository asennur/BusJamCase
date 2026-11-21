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
        public const float StartTime = 1.20f, Duration = 1.05f;
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
        private readonly Vector3[] hopAxes = new Vector3[3];
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
            bus.localRotation = Quaternion.Euler(0, 180, 0);
            for (int i = 0; i < riders.Length; i++)
            {
                Passenger passenger = Object.Instantiate(settings.passengerPrefab, cast.transform);
                passenger.enabled = false; passenger.PrepareCelebration();
                riders[i] = passenger.transform;
                Vector3 worldScale = riders[i].lossyScale;
                riders[i].SetParent(vehicle.PresentationSeat(i), false);
                riders[i].localRotation = Quaternion.Euler(passenger.PresentationSeatRotation);
                riders[i].Rotate(Vector3.up, 60, Space.World);
                hopAxes[i] = riders[i].parent.InverseTransformDirection(Vector3.up);
                Vector3 stretch = riders[i].lossyScale;
                riderScales[i] = new Vector3(worldScale.x / stretch.x, worldScale.y / stretch.y, worldScale.z / stretch.z);
                riders[i].localScale = riderScales[i];
                riderBodies[i] = passenger.PresentationBody;
                waveRoots[i] = passenger.PresentationAnimator.gameObject;
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
            float scale = Mathf.Min(halfWidth * .1764f, halfHeight * (camera.aspect > 1.25f ? .12f : .18f));
            cast.transform.localScale = Vector3.one * scale;
            radius = scale * 4.7f;
        }

        public void Sample(float timeline)
        {
            float elapsed = timeline - StartTime;
            if (elapsed < 0 || elapsed >= Duration) { Hide(); return; }
            if (!camera.enabled)
            { cast.SetActive(true); camera.enabled = key.enabled = true; image.style.display = DisplayStyle.Flex; }
            float p = elapsed / Duration;
            Vector3 origin = camera.transform.position + camera.transform.forward * 18;
            float x = Mathf.Lerp(halfWidth + radius, -halfWidth - radius, p);
            float y = -halfHeight * (camera.aspect > 1.25f ? .75f : .61f);
            cast.transform.position = origin + camera.transform.right * x + camera.transform.up * y;
            settings.drive.SampleAnimation(driveRoot, elapsed % settings.drive.length);
            settings.exhaust.SampleAnimation(driveRoot, elapsed % settings.exhaust.length);
            for (int i = 0; i < riders.Length; i++)
            {
                settings.wave.SampleAnimation(waveRoots[i], (elapsed + i * .13f) % settings.wave.length);
                float cycle = Mathf.Repeat(elapsed / .43f + i * .24f, 1);
                float hop = cycle < .78f ? Mathf.Sin(cycle / .78f * Mathf.PI) * .32f : 0;
                float squash = cycle >= .78f ? Mathf.Sin((cycle - .78f) / .22f * Mathf.PI) * .10f : 0;
                riders[i].localPosition = hopAxes[i] * hop;
                riders[i].localScale = Vector3.Scale(riderScales[i], new Vector3(1 + squash * .5f, 1 - squash, 1 + squash * .5f));
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
