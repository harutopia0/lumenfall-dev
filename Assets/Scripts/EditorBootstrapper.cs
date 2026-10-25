#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class EditorBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitCoreScene()
    {
        string coreSceneName = SceneNames.PersistentCore;

        if (EditorSceneManager.playModeStartScene != null && EditorSceneManager.playModeStartScene.name == coreSceneName)
        {
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name == coreSceneName)
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
