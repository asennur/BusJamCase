using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BusJam.LevelEditor
{
    /// <summary>
    /// Test Play sırasında oyun sahnesine "Editor" butonu ekler; buton ya da Esc editör sahnesine,
    /// test edilen levele döner. Yalnız editörden açılan testte vardır, sahnelere dokunmaz.
    /// </summary>
    public sealed class EditorReturn : MonoBehaviour
    {
        private string editorScene;
        private Button button;

        public static void Begin(string editorSceneName)
        {
            var go = new GameObject("EditorReturn");
            DontDestroyOnLoad(go);
            go.AddComponent<EditorReturn>().editorScene = editorSceneName;
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == editorScene) { Destroy(gameObject); return; }
            var document = FindObjectOfType<UIDocument>();
            var root = document != null ? document.rootVisualElement?.Q("ui-root") : null;
            if (root == null) return;
            button = new BusJam.UI.VectorButton { text = "Editor", name = "editor-return", tooltip = "Back to the level editor (Esc)" };
            button.AddToClassList("editor-button"); button.AddToClassList("yellow"); button.AddToClassList("editor-return");
            button.clicked += Return;
            root.Add(button);
            BusJam.UI.VectorArt.Apply(button);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Return();
        }

        private void Return()
        {
            if (button != null) { button.clicked -= Return; button.RemoveFromHierarchy(); button = null; }
            SceneManager.LoadScene(editorScene);
        }
    }
}
