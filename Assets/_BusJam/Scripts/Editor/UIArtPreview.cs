using UnityEditor;
using UnityEngine;

namespace BusJam.UI.Editor
{
    /// <summary>Previews the shared sprites without creating scene objects.</summary>
    public sealed class UIArtPreview : EditorWindow
    {
        private Sprite[] sprites;

        [MenuItem("Bus Jam/UI Art Preview")]
        public static void Open()
        {
            var window = GetWindow<UIArtPreview>(true, "UI Art - Edit Mode", true);
            window.minSize = new Vector2(1000, 800);
            window.maxSize = window.minSize;
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            var paths = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/_BusJam/Art/UI" });
            System.Array.Sort(paths, (a, b) => string.CompareOrdinal(
                AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));
            sprites = new Sprite[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(paths[i]));
        }

        private void OnGUI()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.12f, 0.14f, 0.2f));
            GUI.Label(new Rect(16, 8, 968, 24), "UI ART  |  25 imported sprites  |  Edit mode  |  No scene objects", EditorStyles.whiteLargeLabel);
            if (sprites == null)
                return;

            for (int i = 0; i < sprites.Length; i++)
            {
                var cell = new Rect(12 + i % 5 * 197, 40 + i / 5 * 150, 188, 140);
                EditorGUI.DrawRect(cell, new Color(0.21f, 0.23f, 0.29f));
                var sprite = sprites[i];
                if (sprite == null)
                    continue;
                var bounds = new Rect(cell.x + 8, cell.y + 5, cell.width - 16, 106);
                float scale = Mathf.Min(bounds.width / sprite.rect.width, bounds.height / sprite.rect.height);
                var image = new Rect(bounds.center.x - sprite.rect.width * scale / 2,
                    bounds.center.y - sprite.rect.height * scale / 2,
                    sprite.rect.width * scale, sprite.rect.height * scale);
                var uv = new Rect(sprite.rect.x / sprite.texture.width, sprite.rect.y / sprite.texture.height,
                    sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height);
                GUI.DrawTextureWithTexCoords(image, sprite.texture, uv, true);
                GUI.Label(new Rect(cell.x + 8, cell.y + 115, cell.width - 16, 20), sprite.name, EditorStyles.whiteLabel);
            }
        }
    }
}
