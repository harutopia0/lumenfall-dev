#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class EditorBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitCoreScene()
    {
        string coreSceneName = "Core_Persistent";

        // Neu khoi dong tu MainMenu (hoac Tester Mode bat dau tu MainMenu), KHONG spawn Core_Persistent
        if (EditorSceneManager.playModeStartScene != null && EditorSceneManager.playModeStartScene.name == "MainMenu")
        {
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name == "MainMenu")
        {
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            if (loadedScene.name == coreSceneName)
            {
                return;
            }
        }

        SceneManager.LoadScene(coreSceneName, LoadSceneMode.Additive);
    }
}
#endif
