using System;
using System.Reflection;
using BusJam.Core;
using BusJam.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Installs ZG without moving existing Start or HUD transforms.</summary>
    public static class ZGSetup
    {
        private static UIArt art;
        private static TMP_FontAsset uiFont, numberFont;
        private static void Set(Object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform; r.SetParent(parent, false);
            r.sizeDelta = size; r.anchoredPosition = position; return r;
        }
        private static Image Image(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, bool sliced = false)
        {
            var r = Rect(parent, name, size, position); var image = r.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false;
            image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = !sliced;
            if (sliced) image.pixelsPerUnitMultiplier = sprite.rect.height * 100 / (sprite.pixelsPerUnit * size.y);
            return image;
        }
        private static TMP_Text Text(Transform parent, string name, string text, Vector2 size, Vector2 position, float fontSize, bool number = false)
        {
            var r = Rect(parent, name, size, position); var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = number ? numberFont : uiFont; t.text = text; t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            t.color = Color.white; return t;
        }
        private static Button Button(Transform parent, string name, string label, Vector2 pos)
        {
            var image = Image(parent, name, art.btn_green, new Vector2(594, 190), pos, true);
            image.raycastTarget = true; var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text(image.transform, "Label", label, new Vector2(470, 110), new Vector2(0, 8), 62);
            return button;
        }
        private static Image Dim(Transform parent)
        {
            var image = Image(parent, "Dim", null, Vector2.zero, Vector2.zero);
            image.rectTransform.anchorMin = Vector2.zero; image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.sizeDelta = Vector2.zero; image.color = new Color(.102f, .043f, .18f, .65f);
            image.raycastTarget = true; image.gameObject.AddComponent<FullscreenDim>(); return image;
        }
        public static string Install()
        {
            ZM3Setup.RequireEditMode();
            art = AssetDatabase.LoadAssetAtPath<UIArt>("Assets/_BusJam/UI/UIArt.asset");
            uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_BusJam/Art/Fonts/Fredoka-Bold SDF.asset");
            numberFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_BusJam/Art/Fonts/LilitaOne SDF.asset");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var manager = ZM3Setup.InScene<GameManager>(scene);
            var ui = ZM3Setup.InScene<UIManager>(scene);
            var safe = ui.transform.Find("SafeArea");
            if (safe == null) safe = ui.GetComponentInChildren<SafeArea>(true).transform;
            if (safe.Find("CoinCounter") != null) return "ZG is already installed.";
            var scaler = ZM3Setup.InScene<CanvasScaler>(scene); scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            var counterImage = Image(safe, "CoinCounter", art.counter_plate, new Vector2(260, 116), new Vector2(-365, -103), true);
            counterImage.rectTransform.anchorMin = counterImage.rectTransform.anchorMax = Vector2.one;
            Image(counterImage.transform, "icon_coin", art.icon_coin, new Vector2(100, 120), new Vector2(-100, 0));
            var counterText = Text(counterImage.transform, "Coins", "0", new Vector2(180, 90), new Vector2(28, 0), 55, true);
            var counter = counterImage.gameObject.AddComponent<CoinCounter>(); Set(counter, "countText", counterText);
            Set(ui, "coinCounter", counter.gameObject);
            var complete = ZM3Setup.InScene<CompletePanel>(scene); complete.gameObject.SetActive(false);
            var fail = ZM3Setup.InScene<FailPanel>(scene); fail.gameObject.SetActive(false);
            // Only the two result panels are rebuilt. Start and HUD objects remain intact.
            for (int i = complete.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(complete.transform.GetChild(i).gameObject);
            for (int i = fail.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(fail.transform.GetChild(i).gameObject);
            var dim = Dim(complete.transform);
            var header = Rect(complete.transform, "Header", new Vector2(850, 460), new Vector2(0, 370));
            var rays = Image(header, "glow_rays", art.glow_rays, new Vector2(950, 950), Vector2.zero);
            Image(header, "ribbon_purple", art.ribbon_purple, new Vector2(900, 290), new Vector2(0, -55));
            Image(header, "title_complete", art.title_complete, new Vector2(790, 400), new Vector2(0, 35));
            var pile = Rect(complete.transform, "Reward", new Vector2(650, 430), new Vector2(0, -100));
            Image(pile, "coin_pile", art.coin_pile, new Vector2(560, 340), new Vector2(0, 40));
            var reward = Text(pile, "RewardAmount", "+25", new Vector2(500, 140), new Vector2(0, -165), 110, true);
            var sparkles = new Image[6];
            for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3; sparkles[i] = Image(pile, "Sparkle_" + i, art.fx_sparkle, new Vector2(65, 65), new Vector2(Mathf.Cos(a) * 280, Mathf.Sin(a) * 150 + 35)); }
            var next = Button(complete.transform, "NextButton", "Continue", new Vector2(0, -520));
            var confetti = new Image[40]; var palette = new[] { Color.yellow, Color.cyan, Color.green, new Color(.65f,.3f,1), new Color(1,.3f,.7f), new Color(1,.5f,.1f) };
            for (int i = 0; i < 40; i++) { confetti[i] = Image(complete.transform, "Confetti_" + i, i % 2 == 0 ? art.fx_confetti_star : art.fx_confetti_rect, new Vector2(30, 30), new Vector2(0, 350)); confetti[i].color = palette[i % 6]; }
            var coins = new Image[10];
            for (int i = 0; i < 10; i++) { coins[i] = Image(complete.transform, "FlyingCoin_" + i, art.icon_coin, new Vector2(85, 85), Vector2.zero); coins[i].gameObject.SetActive(false); }
            Set(complete,"gameManager",manager); Set(complete,"nextButton",next); Set(complete,"coinCounter",counter); Set(complete,"dim",dim);
            Set(complete,"header",header); Set(complete,"rays",rays.rectTransform); Set(complete,"pile",pile); Set(complete,"rewardText",reward);
            Set(complete,"confetti",confetti); Set(complete,"sparkles",sparkles); Set(complete,"coins",coins);
            var failDim = Dim(fail.transform);
            var panel = Image(fail.transform,"panel_modal",art.panel_modal,new Vector2(880,1100),Vector2.zero,true);
            var bar = Image(panel.transform,"panel_header",art.panel_header,new Vector2(870,150),new Vector2(0,440),true);
            var level = Text(bar.transform,"Level","Level 50",new Vector2(600,100),Vector2.zero,62);
            Image(panel.transform,"title_failed",art.title_failed,new Vector2(720,280),new Vector2(0,240));
            var icon = Image(panel.transform,"ReasonIcon",art.icon_heart_broken,new Vector2(210,210),new Vector2(0,10));
            var minus = Text(panel.transform,"LifeCost","-1",new Vector2(180,100),new Vector2(145,-15),80,true);
            var reason = Text(panel.transform,"Reason","No space left!",new Vector2(740,100),new Vector2(0,-160),57);
            var retry = Button(panel.transform,"RetryButton","Try Again",new Vector2(0,-350));
            var closeImage = Image(panel.transform,"icon_close",art.icon_close,new Vector2(110,110),new Vector2(370,460));
            closeImage.raycastTarget = true; var close = closeImage.gameObject.AddComponent<Button>(); close.targetGraphic = closeImage;
            Set(fail,"gameManager",manager); Set(fail,"reasonText",reason); Set(fail,"levelText",level); Set(fail,"retryButton",retry); Set(fail,"closeButton",close);
            Set(fail,"dim",failDim); Set(fail,"reasonIcon",icon); Set(fail,"panel",panel.rectTransform); Set(fail,"minusOne",minus.gameObject); Set(fail,"art",art);
            // Existing Start money artwork is preserved and now displays saved coins too.
            var oldMoney = safe.Find("StartPanel/UpUI/MoneyCounter");
            if (oldMoney != null) { var old = oldMoney.gameObject.AddComponent<CoinCounter>(); Set(old,"countText",oldMoney.GetComponentInChildren<TMP_Text>(true)); }
            counter.transform.SetAsLastSibling();
            EditorUtility.SetDirty(ui); EditorUtility.SetDirty(complete); EditorUtility.SetDirty(fail);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "ZG installed. Existing Start/HUD transforms preserved; new shared counter at safe-area top right (-365,-103); existing Start money counter reused.";
        }
    }
}
