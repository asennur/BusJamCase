#if UNITY_EDITOR
using BusJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    public sealed partial class ToolkitGameUI
    {
        // Only the isolated preview clone calls these methods; no gameplay state or saves are changed.
        public void ConfigureResultPreview(UIDocument previewDocument, ResultTimeline timeline)
        { document = previewDocument; resultTimeline = timeline; }

        public bool SampleResultPreview(ResultTrack track, float time, int width, int height)
        {
            if (Application.isPlaying || !CacheElements()) return false;
            // The clone's viewport is independent of the real Game View layout controller.
            root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            var state = track == ResultTrack.Fail || track == ResultTrack.Retry ? GameState.Fail : GameState.Complete;
            if (shownState != state)
            {
                ShowLives(5, 0); ShowLives(4, 1199);
                InitializeCelebrationBus(); DisplayState(state);
                celebrationBus.Camera.scene = gameObject.scene;
            }
            celebrationBus?.Resize(width, height);
            float aspect = (float)width / height;
            root.EnableInClassList("portrait", aspect < .8f);
            root.EnableInClassList("square", aspect >= .8f && aspect <= 1.25f);
            root.EnableInClassList("landscape", aspect > 1.25f);
            collecting = track == ResultTrack.Collection; retrying = track == ResultTrack.Retry;
            resultTransitionDone = false;
            if (state == GameState.Complete)
            {
                SetCollectionTime(collecting ? time : 0);
                SetCelebrationTime(collecting ? IntroDuration : time);
                SetCelebrationIdle(collecting ? IntroDuration + time : time);
            }
            else
            {
                fail.style.opacity = 1;
                SetFailureEntry(retrying ? FailureDuration : time);
                if (retrying) SetFailureExit(time);
            }
            return true;
        }

        public void RenderPreviewCelebration()
        { if (celebrationBus != null && celebrationBus.Visible) celebrationBus.Camera.Render(); }
    }
}
#endif
