using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BusJam.LevelEditor
{
    /// <summary>
    /// Test Play sırasında oyun sahnesine "Editor" butonu ekler; buton ya da Esc editör sahnesine,
    /// test edilen levele döner. Oynanan level taslağın kopyasıdır (GameManager.TestLevel); asset'e ve LevelDatabase'e
    /// dokunulmaz, kopya bu nesneyle birlikte silinir. Yalnız editörden açılan testte vardır.
    /// </summary>
    public sealed class EditorReturn : MonoBehaviour
    {
        private string editorScene;
        private Button button;
        private BusJam.Level.LevelData testLevel;

        public static void Begin(string editorSceneName, BusJam.Level.LevelData level, int levelNumber)
        {
            // Sahne geçişindeki UnloadUnusedAssets kopyayı silmesin.
            level.hideFlags = HideFlags.DontUnloadUnusedAsset;
            BusJam.Core.GameManager.TestLevel = level;
            BusJam.Core.GameManager.TestLevelNumber = levelNumber;
            var go = new GameObject("EditorReturn");
            DontDestroyOnLoad(go);
            var editorReturn = go.AddComponent<EditorReturn>();
            editorReturn.editorScene = editorSceneName;
            editorReturn.testLevel = level;
        }

        private void OnDestroy()
        {
            if (BusJam.Core.GameManager.TestLevel == testLevel) BusJam.Core.GameManager.TestLevel = null;
            if (testLevel != null) Destroy(testLevel);
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
