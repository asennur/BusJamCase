using System;
using System.Reflection;
using BusJam.Core;
using BusJam.Save;
using BusJam.UI;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Deterministic Edit-mode checks and the two requested result captures.</summary>
    public static class ZGVerification
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static object Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        public static string Run()
        {
            ZM3Setup.RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var complete = ZM3Setup.InScene<CompletePanel>(scene);
            var fail = ZM3Setup.InScene<FailPanel>(scene);
            bool hadCoins = PlayerPrefs.HasKey("Coins"); int oldCoins = PlayerPrefs.GetInt("Coins");
            try
            {
                PlayerPrefs.SetInt("Coins", 0); SaveSystem.AddCoins(25); Check(SaveSystem.Coins == 25,"Coin reward persists.");
                SaveSystem.AddCoins(-1); Check(SaveSystem.Coins == 25,"Negative rewards cannot subtract coins.");
                PlayerPrefs.SetInt("Coins",int.MaxValue - 2); SaveSystem.AddCoins(25); Check(SaveSystem.Coins == int.MaxValue,"Coin overflow saturates.");
                complete.PreviewFinalPose();
                PlayerPrefs.SetInt("Coins", 0);
                Check((bool)Call(complete,"CreditReward"),"First credit is accepted.");
                Check(!(bool)Call(complete,"CreditReward") && SaveSystem.Coins == 25,"Duplicate credit is blocked.");
                complete.PreviewFinalPose();
                Call(complete,"Build");
                var entry = Get<Sequence>(complete,"entry"); entry.ForceInit(); entry.Goto(1.6f,true); entry.Pause();
                Check(Get<TMP_Text>(complete,"rewardText").text == "+25","Entry counts to the serialized reward.");
                Check(Get<RectTransform>(complete,"header").localScale == Vector3.one,"Header settles at 1.");
                Check(Get<Image[]>(complete,"confetti").Length == 40 && Get<Image[]>(complete,"coins").Length == 10,"Fixed pools are complete.");
                foreach(var image in Get<Image[]>(complete,"confetti")) Check(image.color.a < .001f,"Confetti fades out.");
                var pool = Get<Image[]>(complete,"coins");
                Call(complete,"Continue");
                var transfer = Get<Sequence>(complete,"transfer"); transfer.Goto(1.04f,true); transfer.Pause();
                var counter = Get<CoinCounter>(complete,"coinCounter");
                Check(Get<TMP_Text>(counter,"countText").text == "47", "Nine arrivals show 25 + 22 pending coins.");
                Check(SaveSystem.Coins == 25,"Partial transfer does not credit the save.");
                Call(complete,"OnDisable");
                Check(!entry.IsPlaying() && !transfer.IsPlaying() && object.ReferenceEquals(pool,Get<Image[]>(complete,"coins")),"Close stops timelines and retains the pool.");
                foreach (var image in pool) Check(!image.gameObject.activeSelf,"Close hides flying coins.");
                foreach(FailReason reason in Enum.GetValues(typeof(FailReason)))
                {
                    fail.Show(reason,50);
                    Check(Get<TMP_Text>(fail,"reasonText").text == (reason == FailReason.OutOfTime ? "Time's up!" : reason == FailReason.NoMoves ? "No moves left!" : "No space left!"),"Failure reason matches.");
                    Check(Get<GameObject>(fail,"minusOne").activeSelf == (reason != FailReason.OutOfTime),"Life cost visibility matches.");
                }
                return "Edit checks passed: coin persistence/negative/overflow; sequence final reward/header/confetti; 40+10 pools; staggered arrivals and close/reset; once-only credit; all three failure reasons and life-cost visibility. No Play entry.";
            }
            finally
            {
                Get<Sequence>(complete,"entry")?.Kill(); Get<Sequence>(complete,"ambient")?.Kill(); Get<Sequence>(complete,"transfer")?.Kill();
                Get<Tween>(complete,"rayRotation")?.Kill(); Get<Tween>(complete,"rayPulse")?.Kill(); Get<Tween>(complete,"buttonIdle")?.Kill();
                if(hadCoins) PlayerPrefs.SetInt("Coins",oldCoins); else PlayerPrefs.DeleteKey("Coins"); PlayerPrefs.Save();
            }
        }
        public static string Capture(bool failed)
        {
            ZM3Setup.RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var ui = ZM3Setup.InScene<UIManager>(scene);
            var complete = ZM3Setup.InScene<CompletePanel>(scene);
            var fail = ZM3Setup.InScene<FailPanel>(scene);
            var canvas = ZM3Setup.InScene<Canvas>(scene);
            var safe = ZM3Setup.InScene<SafeArea>(scene);
            var camera = Camera.main;
            var panels = new GameObject[] { Get<GameObject>(ui,"startPanel"),Get<GameObject>(ui,"hudPanel"),complete.gameObject,fail.gameObject,Get<GameObject>(ui,"noLivesPanel"),Get<GameObject>(ui,"coinCounter") };
            var active = new bool[panels.Length]; for(int i=0;i<panels.Length;i++) { active[i]=panels[i].activeSelf; panels[i].SetActive(false); }
            var mode = canvas.renderMode; var worldCamera = canvas.worldCamera; float distance = canvas.planeDistance;
            var target = camera.targetTexture; var oldActive = RenderTexture.active;
            var rt = new RenderTexture(1242,2688,24); var texture = new Texture2D(1242,2688,TextureFormat.RGB24,false);
            try
            {
                complete.gameObject.SetActive(!failed); fail.gameObject.SetActive(failed); panels[5].SetActive(!failed);
                if(failed) fail.Show(FailReason.NoSpace,50); else complete.PreviewFinalPose();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 1;
                camera.targetTexture = rt; Canvas.ForceUpdateCanvases();
                // Dim stretches to the complete canvas, independent of the SafeArea content bounds.
                var dim = (failed ? fail.transform : complete.transform).Find("Dim").GetComponent<RectTransform>();
                var canvasRect = (RectTransform)canvas.transform;
                dim.position = canvasRect.TransformPoint(canvasRect.rect.center); dim.sizeDelta = canvasRect.rect.size * canvasRect.lossyScale.x / dim.parent.lossyScale.x;
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4]; var safeRect = (RectTransform)safe.transform; safeRect.GetWorldCorners(corners);
                var bounds = new Rect(corners[0], corners[2]-corners[0]);
                var content = failed ? Get<RectTransform>(fail,"panel") : Get<RectTransform>(complete,"pile"); content.GetWorldCorners(corners);
                Check(bounds.Contains(corners[0]) && bounds.Contains(corners[2]),"Result content must fit safe area at 1242x2688.");
                camera.Render(); RenderTexture.active = rt; texture.ReadPixels(new Rect(0,0,1242,2688),0,0); texture.Apply();
                System.IO.Directory.CreateDirectory("Docs/ZG-Captures");
                string path = "Docs/ZG-Captures/" + (failed ? "Fail.png" : "Complete.png");
                System.IO.File.WriteAllBytes(path,texture.EncodeToPNG()); return path + " (1242x2688; content bounds inside SafeArea)";
            }
            finally
            {
                camera.targetTexture = target; RenderTexture.active = oldActive;
                canvas.renderMode = mode; canvas.worldCamera = worldCamera; canvas.planeDistance = distance;
                for(int i=0;i<panels.Length;i++) panels[i].SetActive(active[i]);
                Object.DestroyImmediate(texture); rt.Release(); Object.DestroyImmediate(rt); Canvas.ForceUpdateCanvases();
            }
        }
    }
}
