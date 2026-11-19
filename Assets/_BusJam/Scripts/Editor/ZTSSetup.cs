using System;
using BusJam.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BusJam.Editor
{
    /// <summary>Adds the ready overlay without changing the existing layout.</summary>
    public static class ZTSSetup
    {
        [MenuItem("Bus Jam/ZTS/Install Ready Overlay (Edit)")]
        public static void Install()
        {
            ZM3Setup.RequireEditMode();
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/_BusJam/Scenes/Game.unity")
                throw new InvalidOperationException("Open Game.unity first.");
            var ui = ZM3Setup.InScene<UIManager>(scene);
            var root = ui.transform.Find("ReadyPanel");
            if (root == null)
            {
                root = new GameObject("ReadyPanel", typeof(RectTransform), typeof(Image)).transform;
                root.SetParent(ui.transform, false);
            }
            var rect = (RectTransform)root;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.SetAsLastSibling();
            var dim = root.GetComponent<Image>();
            dim.color = new Color(.04f, .06f, .12f, .22f);
            dim.raycastTarget = true;
            if (root.GetComponent<FullscreenDim>() == null) root.gameObject.AddComponent<FullscreenDim>();
            var child = root.Find("Prompt");
            if (child == null)
            {
                child = new GameObject("Prompt", typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
                child.SetParent(root, false);
            }
            var label = child.GetComponent<TextMeshProUGUI>();
            var textRect = label.rectTransform;
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = Vector2.one * .5f;
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(950, 180);
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_BusJam/Art/Fonts/Fredoka-Bold SDF.asset");
            label.text = "Tap to start";
            label.fontSize = 88;
            label.color = Color.white;
            label.outlineColor = new Color32(18, 25, 48, 255);
            label.outlineWidth = .22f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var ready = root.GetComponent<ReadyPanel>() ?? root.gameObject.AddComponent<ReadyPanel>();
            ZM3Setup.Set(ready, "prompt", label);
            ZM3Setup.Set(ui, "readyPanel", root.gameObject);
            root.gameObject.SetActive(false);
            EditorUtility.SetDirty(ready);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
