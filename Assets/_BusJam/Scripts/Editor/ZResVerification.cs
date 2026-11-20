using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using BusJam.Gameplay;
using BusJam.Level;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace BusJam.EditorTools
{
    /// <summary>Deterministic Game view evidence without Play mode or saved preview objects.</summary>
    public static class ZResVerification
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        public static readonly string[] Screens = { "Start", "HUD", "Complete", "Fail", "Settings", "NoLives", "LevelEditor" };
        private static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, Flags).GetValue(o);

        public static void Size(int width, int height)
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(type);
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance", Flags).GetValue(null);
            var group = sizesType.GetMethod("GetGroup", Flags).Invoke(sizes, new[] { type.GetProperty("currentSizeGroupType", Flags).GetValue(view) });
            int count = (int)group.GetType().GetMethod("GetTotalCount", Flags).Invoke(group, null);
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                var size = group.GetType().GetMethod("GetGameViewSize", Flags).Invoke(group, new object[] { i });
                if ((int)size.GetType().GetProperty("width", Flags).GetValue(size) == width && (int)size.GetType().GetProperty("height", Flags).GetValue(size) == height) { index = i; break; }
            }
            if (index < 0)
            {
                var sizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
                var enumType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, Flags, null, new[] { Enum.Parse(enumType, "FixedResolution"), (object)width, height, "ZRES " + width + "x" + height }, null);
                group.GetType().GetMethod("AddCustomSize", Flags).Invoke(group, new[] { size }); index = count;
            }
            type.GetProperty("selectedSizeIndex", Flags).SetValue(view, index);
            type.GetField("m_Gizmos", Flags).SetValue(view, false);
            type.GetField("m_ClearInEditMode", Flags).SetValue(view, false);
            Camera.main.aspect = (float)width / height;
            var modes = Camera.main.GetComponent<BusJam.Core.LayoutModeController>();
            if (modes != null) modes.ApplyViewport(width, height, new Rect(0, 0, width, height));
            var fitter = Camera.main.GetComponent<CameraFitter>();
            if (fitter != null) fitter.Fit();
            view.Focus(); view.Repaint();
        }

        public static string Prepare(string screen, int width, int height)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required.");
            Cleanup();
            ZLightPreview.Prepare("ZRES", screen == "Start" || screen == "NoLives" ? "Start" : screen == "Complete" ? "Complete" : "HUD");
            if (screen == "LevelEditor")
            {
                Cleanup();
                EditorSceneManager.OpenScene("Assets/_BusJam/Scenes/LevelEditor.unity");
                var source = UnityEngine.Object.FindObjectOfType<BoardView>(true);
                var fixture = UnityEngine.Object.Instantiate(source);
                fixture.name = "ZRES disposable editor board"; fixture.gameObject.hideFlags = HideFlags.DontSave;
                var level = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/_BusJam/Data/Levels/Level_07.asset");
                var board = new Board(); board.Setup(level); fixture.Build(level, board);
            }
            else
            {
                var canvas = GameObject.Find("Canvas");
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "FailPanel") t.gameObject.SetActive(screen == "Fail");
                    if (t.name == "SettingsPanel") t.gameObject.SetActive(screen == "Settings");
                    if (t.name == "NoLivesPanel") t.gameObject.SetActive(screen == "NoLives");
                }
                foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.name == "TimerText") text.text = "60";
                    if (text.name == "ReasonText") text.text = "Time is up!";
                }
            }
            Size(width, height);
            return screen + " prepared in Edit mode at " + width + "x" + height;
        }

        public static string Capture(string path)
        {
            typeof(ZLightPreview).GetMethod("RepaintGameView", Flags).Invoke(null, null);
            Canvas.ForceUpdateCanvases();
            foreach (var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
                if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate();
            foreach (var graphic in UnityEngine.Object.FindObjectsOfType<Graphic>(true))
                if (graphic.gameObject.activeInHierarchy) { graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender); graphic.Rebuild(CanvasUpdate.LatePreRender); }
            Canvas.ForceUpdateCanvases();
            var render = typeof(ZLightPreview).GetMethod("Render", Flags);
            render.Invoke(null, null); render.Invoke(null, null);
            var rt = (RenderTexture)render.Invoke(null, null);
            var overlayMeshes = DrawUnbatchedOverlay(rt);
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply(); RenderTexture.active = previous;
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                var pixels = texture.GetPixels32(); var flipped = new Color32[pixels.Length];
                for (int y = 0; y < rt.height; y++) Array.Copy(pixels, y * rt.width, flipped, (rt.height - y - 1) * rt.width, rt.width);
                texture.SetPixels32(flipped); texture.Apply();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            if (overlayMeshes != null) foreach (var mesh in overlayMeshes) UnityEngine.Object.DestroyImmediate(mesh);
            return path + " " + rt.width + "x" + rt.height;
        }

        // Unfocused Edit-mode windows can omit the native overlay batch. Reuse the current UI meshes.
        private static List<Mesh> DrawUnbatchedOverlay(RenderTexture target)
        {
            var canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
            var graphics = canvas.GetComponentsInChildren<Graphic>(false);
            if (graphics.Any(g => g.depth >= 0)) return null;
            var meshes = new List<Mesh>();
            var commands = new CommandBuffer { name = "ZRES Edit overlay capture" };
            commands.SetRenderTarget(target);
            commands.SetViewport(new Rect(0, 0, target.width, target.height));
            commands.SetViewProjectionMatrices(Matrix4x4.identity, GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(0, target.width, 0, target.height, -10, 10), true));
            commands.SetGlobalFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
            foreach (var graphic in graphics)
            {
                var source = graphic.canvasRenderer.GetMesh();
                if (source == null || source.vertexCount == 0) continue;
                var mesh = new Mesh { vertices = source.vertices, uv = source.uv, uv2 = source.uv2, colors32 = source.colors32, normals = source.normals, tangents = source.tangents };
                var triangles = new int[source.vertexCount / 4 * 6];
                for (int q = 0; q < source.vertexCount / 4; q++)
                {
                    int v = q * 4, t = q * 6;
                    triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                    triangles[t + 3] = v + 2; triangles[t + 4] = v + 3; triangles[t + 5] = v;
                }
                mesh.triangles = triangles; meshes.Add(mesh);
                var properties = new MaterialPropertyBlock();
                var text = graphic as TMP_Text;
                properties.SetTexture("_MainTex", text != null ? text.font.atlasTexture : graphic.mainTexture);
                commands.DrawMesh(mesh, graphic.transform.localToWorldMatrix, graphic.materialForRendering, 0, 0, properties);
            }
            Graphics.ExecuteCommandBuffer(commands); commands.Release();
            return meshes;
        }

        public static string Cleanup()
        {
            ZLightPreview.Cleanup();
            foreach (var root in Resources.FindObjectsOfTypeAll<GameObject>())
                if (!EditorUtility.IsPersistent(root) && root.transform.parent == null &&
                    (root.name.StartsWith("ZRES disposable", StringComparison.Ordinal) || root.name.StartsWith("ZLIGHT disposable", StringComparison.Ordinal)))
                    UnityEngine.Object.DestroyImmediate(root);
            return "Disposable previews removed; saved Game scene restored; Edit mode.";
        }
    }
}
