#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EditorBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitCoreScene()
    {
        string coreSceneName = "Core_Persistent";

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
