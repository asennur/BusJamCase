using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BusJam.EditorTools
{
    /// <summary>Checks framing, resize events and fixed-world anchors without running gameplay.</summary>
    public static class WorldResolutionChecks
    {
        public static readonly Vector2Int[] Sizes = {
            new Vector2Int(1080,1920), new Vector2Int(1080,2400), new Vector2Int(1170,2532),
            new Vector2Int(720,1280), new Vector2Int(1536,2048), new Vector2Int(1920,1080),
            new Vector2Int(2560,1080), new Vector2Int(1080,1080), new Vector2Int(1768,2208), new Vector2Int(2208,1768)
        };

        public static string Run()
        {
            ZResVerification.Cleanup();
            int assertions = 0;
            var rows = new List<string> { "scene|resolution|mode|camera|viewport min/max|lane anchor offset|door marker XZ offset" };
            Action<bool, string> check = (condition, message) => { assertions++; if (!condition) throw new InvalidOperationException(message); };
            check(LayoutModeController.Classify(.7999f) == LayoutMode.Portrait, "Portrait boundary");
            check(LayoutModeController.Classify(.8f) == LayoutMode.Square, "Square lower boundary");
            check(LayoutModeController.Classify(1.25f) == LayoutMode.Square, "Square upper boundary");
            check(LayoutModeController.Classify(1.2501f) == LayoutMode.Landscape, "Landscape boundary");
            foreach (string scene in new[] { "Game", "LevelEditor" })
            {
                EditorSceneManager.OpenScene("Assets/_BusJam/Scenes/" + scene + ".unity");
                var camera = Camera.main; var fitter = camera.GetComponent<CameraFitter>(); var layout = camera.GetComponent<LayoutModeController>();
                var world = UnityEngine.Object.FindObjectOfType<WorldBackground>();
                Vector3 reference = fitter.ReferencePosition;
                var maxLevel = ScriptableObject.CreateInstance<LevelData>(); maxLevel.width = 7; maxLevel.height = 9;
                int events = 0; Action<LayoutMode> onChange = _ => events++; layout.Changed += onChange;
                foreach (var size in Sizes)
                {
                    camera.aspect = (float)size.x / size.y;
                    layout.ApplyViewport(size.x, size.y, new Rect(0, 0, size.x, size.y)); fitter.SetLevel(maxLevel);
                    Bounds bounds = fitter.CurrentBounds; Vector3 minimum = Vector3.one * float.MaxValue, maximum = Vector3.one * float.MinValue;
                    for (int i = 0; i < 8; i++)
                    {
                        var p = camera.WorldToViewportPoint(new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x, (i & 2) == 0 ? bounds.min.y : bounds.max.y, (i & 4) == 0 ? bounds.min.z : bounds.max.z));
                        minimum = Vector3.Min(minimum, p); maximum = Vector3.Max(maximum, p);
                        check(p.z > camera.nearClipPlane && p.x >= .0149f && p.x <= .9851f && p.y >= .0599f && p.y <= .8551f, scene + " " + size + " bounds " + p);
                    }
                    check(Vector3.Cross(camera.transform.position - reference, camera.transform.forward).sqrMagnitude < .000001f, "Camera moved off view axis");
                    check(camera.fieldOfView == 40, "FOV changed");
                    check(world.LaneOffset == 0 && world.StopOffset == 0, "World anchors drifted");
                    if (camera.aspect > .5626f)
                        check(maximum.y <= 1 - 310f / 1920 * Mathf.Sqrt(camera.aspect / .5625f) - .0149f, "World overlaps reserved HUD strip");
                    if (size == Sizes[0] || size == Sizes[3]) check(camera.transform.position == reference, "Reference composition moved");
                    rows.Add(scene + "|" + size.x + "x" + size.y + "|" + layout.Mode + "|" + camera.transform.position.ToString("F5") + "|" + minimum.ToString("F5") + " / " + maximum.ToString("F5") + "|" + world.LaneOffset + "|" + world.StopOffset);
                }
                check(events == Sizes.Length, "A resize did not publish its viewport event"); layout.Changed -= onChange;
                maxLevel.width = 9; maxLevel.height = 12; camera.aspect = .45f;
                layout.ApplyViewport(1080, 2400, new Rect(36, 96, 1008, 2208)); fitter.SetLevel(maxLevel);
                var large = fitter.CurrentBounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = camera.WorldToViewportPoint(new Vector3((i & 1) == 0 ? large.min.x : large.max.x, (i & 2) == 0 ? large.min.y : large.max.y, (i & 4) == 0 ? large.min.z : large.max.z));
                    check(p.x >= layout.SafeViewport.xMin && p.x <= layout.SafeViewport.xMax && p.y >= .045f && p.y <= .87f, "Large level / inset fit failed " + p);
                }
                check(camera.transform.position != reference, "Large-level framing did not react");
                UnityEngine.Object.DestroyImmediate(maxLevel);
            }
            File.WriteAllLines("Docs/ZRES-Captures/Geometry.txt", rows);
            EditorSceneManager.OpenScene("Assets/_BusJam/Scenes/Game.unity"); ZResVerification.Size(1080, 1920);
            string result = assertions + " Edit-mode assertions passed; 7x9 maximum and synthetic 9x12/inset framing; both scenes; all ten resolutions.";
            File.WriteAllText("Docs/ZRES-Captures/Checks.txt", result); return result;
        }
    }
}
