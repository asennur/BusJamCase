using BusJam.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BusJam.UI
{
    /// <summary>Switches the authored menu world with game state and fits its camera on viewport changes.</summary>
    [ExecuteAlways]
    public sealed class MenuWorldPresentation : MonoBehaviour
    {
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
        public Vector2 portraitLens = new Vector2(52, 32);
        public Vector2 squareLens = new Vector2(54, 30);
        public Vector2 landscapeLens = new Vector2(58, 24);
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Idle = Animator.StringToHash("Idle");
        private MaterialPropertyBlock bodyProperties;

        private void OnEnable()
        {
            if (gameManager != null) gameManager.StateChanged += ApplyState;
            if (layout != null) layout.Changed += OnLayout;
            ApplyColor();
            ApplyState(Application.isPlaying && gameManager != null ? gameManager.State : GameState.Start);
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.StateChanged -= ApplyState;
            if (layout != null) layout.Changed -= OnLayout;
            if (menuWorld != null) menuWorld.SetActive(false);
            if (menuCamera != null) menuCamera.enabled = false;
            if (gameplayCamera != null) gameplayCamera.enabled = true;
            if (gameplayCameraData != null) gameplayCameraData.renderType = CameraRenderType.Base;
        }

        private void OnValidate() { ApplyColor(); if (isActiveAndEnabled) RefreshView(); }
        private void OnLayout(LayoutMode mode) => RefreshView();
        public void RefreshView() => FitAspect(Screen.height > 0 ? (float)Screen.width / Screen.height : 9f / 16);

        public void ApplyState(GameState state)
        {
            bool menuVisible = state == GameState.Start;
            if (menuWorld != null) menuWorld.SetActive(menuVisible);
            if (menuCamera != null) menuCamera.enabled = menuVisible;
            // An unstacked URP Overlay does not render. Keeping Camera.main enabled preserves
            // existing indicator and celebration camera bindings during Start initialization.
            if (gameplayCameraData != null) gameplayCameraData.renderType = menuVisible ? CameraRenderType.Overlay : CameraRenderType.Base;
            if (gameplayCamera != null) gameplayCamera.enabled = gameplayCameraData != null || !menuVisible;
            if (!menuVisible) return;
            RefreshView();
            if (Application.isPlaying && busAnimator != null) busAnimator.Play(Idle, 0, 0);
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
            Vector2 lens = band == LayoutMode.Portrait ? portraitLens : band == LayoutMode.Square ? squareLens : landscapeLens;
            menuCamera.fieldOfView = lens.x;
            menuCamera.transform.position = cameraTarget.position - menuCamera.transform.forward * lens.y;
            float distance = sky.localPosition.z;
            float height = 2 * distance * Mathf.Tan(lens.x * Mathf.Deg2Rad * .5f);
            float imageAspect = skyTextureSize.x / skyTextureSize.y;
            float coverHeight = height * Mathf.Max(1, aspect / imageAspect);
            sky.localScale = new Vector3(coverHeight * imageAspect, coverHeight, 1);
        }
    }
}
