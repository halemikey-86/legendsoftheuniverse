#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LegendsOfTheUniverse.Editor
{
    [InitializeOnLoad]
    static class PlayFromMainMenu
    {
        const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        static PlayFromMainMenu()
        {
            var mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (mainMenu != null)
                EditorSceneManager.playModeStartScene = mainMenu;
        }
    }
}
#endif
