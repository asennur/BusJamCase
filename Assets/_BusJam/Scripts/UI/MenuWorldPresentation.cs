using BusJam.Core;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BusJam.UI
{
    /// <summary>Owns the Start camera and its pooled, deterministic driving presentation.</summary>
    [ExecuteAlways]
    public sealed class MenuWorldPresentation : MonoBehaviour
    {
        [Serializable]
        public struct CameraFraming
        {
            [Tooltip("Camera height above Camera Target, in world units.")] public float height;
            [Tooltip("Camera distance behind Camera Target along the road, in world units.")] public float distance;
            [Tooltip("Downward camera pitch in degrees. Lower values show more of the bus rear.")] public float pitch;
            [Tooltip("Vertical field of view in degrees.")] public float fieldOfView;
            public CameraFraming(float h, float d, float p, float f) { height=h; distance=d; pitch=p; fieldOfView=f; }
        }
        [Serializable]
        public struct ScenerySlot
        {
            public Transform anchor;
            public MeshFilter filter;
            public MeshRenderer renderer;
            public bool tree;
        }
        [Header("Scene References")]
        public GameManager gameManager;
        public LayoutModeController layout;
        public Camera gameplayCamera, menuCamera;
        public UniversalAdditionalCameraData gameplayCameraData;
        public GameObject menuWorld;
        public Transform cameraTarget, sky;
        public Animator busAnimator;
        public Renderer busBody;
        public Color busColor = new Color(.035f, .59f, .86f, 1);
        public Vector2 skyTextureSize = new Vector2(1080, 1920);
        [HideInInspector] public Vector2 portraitLens = new Vector2(52, 32);
        [HideInInspector] public Vector2 squareLens = new Vector2(54, 30);
        [HideInInspector] public Vector2 landscapeLens = new Vector2(58, 24);
        public Transform busAnchor;
        public AnimationClip driveClip;
        public Vector3 busRoadPosition = new Vector3(0, 3.298896f, -14.5f);
        public ScenerySlot[] scenery = Array.Empty<ScenerySlot>();
        public Mesh[] houseVariants = Array.Empty<Mesh>();
        public Mesh[] treeVariants = Array.Empty<Mesh>();
        [Header("Driving World")]
        [Tooltip("World units per second toward the camera along the road (+Z). Zero stops the road and scenery.")]
        [Min(0)] public float scrollSpeed = .9f;
        [Tooltip("Distance between house rows. Smaller values make the world denser. Pool size stays fixed.")]
        [Range(10, 18)] public float scenerySpacing = 12;
        [Tooltip("Left and right house distances from the road centre. Trees extend farther out.")]
        public Vector2 sideOffsets = new Vector2(6.4f, 6.4f);
        [Tooltip("Moves the bus on the road without moving the camera. X is sideways, Y is height, Z is along the road.")]
        public Vector3 busPositionOffset;
        [Tooltip("Extra suspension movement in world units. Drive still owns the wheels and exhaust.")]
        [Range(0, .12f)] public float bobAmplitude = .025f;
        [Tooltip("Fixed seed for house type, side, spacing variation and trees. Does not use Unity's random state.")]
        public int scenerySeed = 20261121;
        [Header("Camera by Aspect Band")]
        public CameraFraming portraitCamera = new CameraFraming(13, 23, 28.5f, 48);
        public CameraFraming squareCamera = new CameraFraming(13, 23, 29, 42);
        public CameraFraming landscapeCamera = new CameraFraming(12, 24, 31, 26);
        [Header("Edit Mode Preview")]
        [Tooltip("Animate only the Start presentation in Edit mode. Turn off for a fixed, repeatable preview time.")]
        public bool animatePreview;
        [Tooltip("Absolute preview time in seconds. Sampling the same time and seed always gives the same world.")]
        [Min(0)] public float previewTime;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Drive = Animator.StringToHash("Drive");
        private static readonly int Motion = Shader.PropertyToID("_MenuMotion");
        private MaterialPropertyBlock bodyProperties;
        private bool menuActive;
        private double motionTime;
#if UNITY_EDITOR
        private double lastEditorTime;
#endif

        private void OnEnable()
        {
            if (gameManager != null) gameManager.StateChanged += ApplyState;
            if (layout != null) layout.Changed += OnLayout;
            ApplyColor();
            ApplyState(Application.isPlaying && gameManager != null ? gameManager.State : GameState.Start);
#if UNITY_EDITOR
            lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
            UnityEditor.EditorApplication.update += PreviewUpdate;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= PreviewUpdate;
#endif
            menuActive = false;
            if (gameManager != null) gameManager.StateChanged -= ApplyState;
            if (layout != null) layout.Changed -= OnLayout;
            if (menuWorld != null) menuWorld.SetActive(false);
            if (menuCamera != null) menuCamera.enabled = false;
            if (gameplayCamera != null) gameplayCamera.enabled = true;
            if (gameplayCameraData != null) gameplayCameraData.renderType = CameraRenderType.Base;
        }

        private void OnValidate() { ApplyColor(); if (isActiveAndEnabled) { RefreshView(); SampleMotion(previewTime); } }
        private void OnLayout(LayoutMode mode) => RefreshView();
        public void RefreshView() => FitAspect(Screen.height > 0 ? (float)Screen.width / Screen.height : 9f / 16);

        public void ApplyState(GameState state)
        {
            bool menuVisible = state == GameState.Start;
            bool entering = menuVisible && !menuActive;
            menuActive = menuVisible;
            if (menuWorld != null) menuWorld.SetActive(menuVisible);
            if (menuCamera != null) menuCamera.enabled = menuVisible;
            // An unstacked URP Overlay does not render. Keeping Camera.main enabled preserves
            // existing indicator and celebration camera bindings during Start initialization.
            if (gameplayCameraData != null) gameplayCameraData.renderType = menuVisible ? CameraRenderType.Overlay : CameraRenderType.Base;
            if (gameplayCamera != null) gameplayCamera.enabled = gameplayCameraData != null || !menuVisible;
            if (busAnimator != null) busAnimator.enabled = menuVisible && Application.isPlaying;
            if (!menuVisible) return;
            RefreshView();
            if (entering) { motionTime = 0; SampleMotion(Application.isPlaying ? 0 : previewTime); }
            if (Application.isPlaying && busAnimator != null) busAnimator.Play(Drive, 0, 0);
        }

        private void Update()
        {
            if (!Application.isPlaying || !menuActive) return;
            motionTime += Time.unscaledDeltaTime;
            SampleMotion(motionTime);
        }

#if UNITY_EDITOR
        private void PreviewUpdate()
        {
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            double delta = now - lastEditorTime; lastEditorTime = now;
            if (Application.isPlaying || !menuActive || !animatePreview) return;
            motionTime += Math.Min(delta, .1);
            SampleMotion(motionTime);
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif

        private static uint Hash(uint value)
        {
            value ^= value >> 16; value *= 0x7feb352d; value ^= value >> 15;
            value *= 0x846ca68b; return value ^ (value >> 16);
        }

        // Absolute travel avoids accumulating drift and allows repeatable editor captures and scrubbing.
        public void SampleMotion(double seconds)
        {
            if (!menuActive || !isActiveAndEnabled) return;
            double travel = Math.Max(0, seconds) * scrollSpeed;
            if (menuWorld != null) Shader.SetGlobalVector(Motion, new Vector4((float)(travel % 3000), menuWorld.transform.position.x, menuWorld.transform.position.z, 0));
            int houses = scenery.Length / 3;
            for (int i = 0; i < scenery.Length; i++)
            {
                var slot = scenery[i]; if (slot.anchor == null || slot.filter == null) continue;
                int local = slot.tree ? i - houses : i;
                int count = slot.tree ? scenery.Length - houses : houses;
                float spacing = scenerySpacing * (slot.tree ? .25f : .5f);
                // Every slot travels a full circuit. Endpoints are beyond the camera and the far fade.
                double length = count * spacing;
                // Reverse the slot order so increasing time always moves toward the camera (+Z).
                double distance = local * spacing - travel;
                int cycle = (int)Math.Floor(distance / length);
                float z = 48 - (float)(distance - cycle * length);
                uint h = Hash(unchecked((uint)(i * 397 ^ cycle * 7919 ^ scenerySeed)));
                float side = ((i + cycle + scenerySeed) & 1) == 0 ? -1 : 1;
                float lateral = side < 0 ? sideOffsets.x : sideOffsets.y;
                lateral += slot.tree ? (h >> 8 & 255) / 255f * 12 : (h >> 8 & 255) / 255f * 1.2f;
                float y = -5 - Mathf.Clamp((-z - 16) * .12f, 0, 3);
                slot.anchor.localRotation = Quaternion.Euler(0, slot.tree ? h % 360 : side < 0 ? -90 : 90, 0);
                var variants = slot.tree ? treeVariants : houseVariants;
                if (variants.Length > 0)
                {
                    var mesh = variants[(h >> 16) % variants.Length];
                    if (slot.filter.sharedMesh != mesh) slot.filter.sharedMesh = mesh;
                    if (!slot.tree) y = 2 - Mathf.Clamp((-z - 16) * .17f, 0, 4) - mesh.bounds.max.z;
                }
                slot.anchor.localPosition = new Vector3(side * lateral, y, z);
                bool visible = z >= -60;
                if (slot.renderer != null && slot.renderer.enabled != visible) slot.renderer.enabled = visible;
            }
            if (busAnchor != null) busAnchor.localPosition = busRoadPosition + busPositionOffset + Vector3.up * (Mathf.Sin((float)(seconds % 10) * Mathf.PI * 4) * bobAmplitude);
#if UNITY_EDITOR
            if (!Application.isPlaying && busAnimator != null)
            {
                busAnimator.enabled = true;
                float duration = driveClip != null ? driveClip.length : 1;
                busAnimator.Play(Drive, 0, (float)((seconds / duration) % 1));
                busAnimator.Update(0);
                busAnimator.enabled = false;
            }
#endif
        }

        public void ApplyColor()
        {
            if (busBody == null) return;
            if (bodyProperties == null) bodyProperties = new MaterialPropertyBlock();
            bodyProperties.SetColor(BaseColor, busColor);
            busBody.SetPropertyBlock(bodyProperties, 0);
        }

        // The quad grows around the optical axis: cover-fit crops the image without stretching it.
        public void FitAspect(float aspect)
        {
            if (menuCamera == null || cameraTarget == null || sky == null || aspect <= 0) return;
            LayoutMode band = LayoutModeController.Classify(aspect);
            CameraFraming framing = band == LayoutMode.Portrait ? portraitCamera : band == LayoutMode.Square ? squareCamera : landscapeCamera;
            menuCamera.fieldOfView = framing.fieldOfView;
            menuCamera.transform.rotation = Quaternion.Euler(framing.pitch, 180, 0);
            menuCamera.transform.position = cameraTarget.position + new Vector3(0, framing.height, framing.distance);
            float distance = sky.localPosition.z;
            float height = 2 * distance * Mathf.Tan(framing.fieldOfView * Mathf.Deg2Rad * .5f);
            float imageAspect = skyTextureSize.x / skyTextureSize.y;
            float coverHeight = height * Mathf.Max(1, aspect / imageAspect);
            sky.localScale = new Vector3(coverHeight * imageAspect, coverHeight, 1);
        }
    }
}
