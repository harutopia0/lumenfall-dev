using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Lumenfall.Core;

public class SceneTransitionManager : PersistentSingleton<SceneTransitionManager>
{
    [Header("Scene Configuration")]
    [Tooltip("Scene name identifying the Main Menu / Non-gameplay screen.")]
    [SerializeField] private string menuSceneName = SceneNames.MainMenu;

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Spawn Settings")]
    [Tooltip("Default spawn point identifier to search for when entering a room.")]
    [SerializeField] private string defaultSpawnPointName = SceneNames.DefaultSpawnPoint;

    [Header("Startup")]
    [Tooltip("Initial room scene to load when starting from persistent core.")]
    [SerializeField] private string initialRoomScene = SceneNames.MainMenu;

    private string currentLoadedRoomScene;
    private string persistentSceneName;

    protected override void Awake()
    {
        persistentSceneName = gameObject.scene.name;
        base.Awake();

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.name != persistentSceneName)
        {
            currentLoadedRoomScene = activeScene.name;
            bool isGameplay = IsGameplayScene(activeScene.name);

            SetPlayerActive(isGameplay);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetHUDActive(isGameplay);
            }

            if (isGameplay)
            {
                InitializePlayerPosition(defaultSpawnPointName, activeScene);
            }
        }
        else if (!string.IsNullOrEmpty(initialRoomScene))
        {
            currentLoadedRoomScene = initialRoomScene;
            if (IsMenuScene(initialRoomScene))
            {
                SetPlayerActive(false);
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetHUDActive(false);
                }
            }
            StartCoroutine(LoadInitialRoomRoutine(initialRoomScene, defaultSpawnPointName));
        }
    }

    public bool IsMenuScene(string sceneName)
    {
        return !string.IsNullOrEmpty(sceneName) &&
               string.Equals(sceneName, menuSceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    public bool IsGameplayScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        if (IsMenuScene(sceneName)) return false;
        if (string.Equals(sceneName, persistentSceneName, System.StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    public void SetPlayerActive(bool active)
    {
        Player player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        if (player != null)
        {
            player.gameObject.SetActive(active);
            if (active && player.input != null)
            {
                player.input.Enable();
            }
            else if (!active && player.input != null)
            {
                player.input.Disable();
            }
        }
    }

    public void TransitionToScene(string nextSceneName, string targetSpawnPointName)
    {
        StartCoroutine(TransitionRoutine(nextSceneName, targetSpawnPointName));
    }

    public void InitializePlayerPosition(string spawnPointName, Scene targetScene)
    {
        currentLoadedRoomScene = targetScene.name;

        GameObject spawnPoint = null;

        if (!string.IsNullOrEmpty(spawnPointName))
        {
            spawnPoint = FindObjectInScene(targetScene, spawnPointName);
        }

        if (spawnPoint == null)
        {
            if (!string.IsNullOrEmpty(defaultSpawnPointName))
            {
                spawnPoint = FindObjectInScene(targetScene, defaultSpawnPointName);
            }

            if (spawnPoint == null)
            {
                spawnPoint = FindObjectInScene(targetScene, "Spawn_Default");
            }
        }

        if (spawnPoint == null && targetScene.IsValid())
        {
            foreach (GameObject root in targetScene.GetRootGameObjects())
            {
                SpawnPoint sp = root.GetComponentInChildren<SpawnPoint>(true);
                if (sp != null)
                {
                    spawnPoint = sp.gameObject;
                    break;
                }
            }
        }

        Player player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);

        Vector2 targetPosition = Vector2.zero;

        if (player != null)
        {
            if (!player.gameObject.activeSelf)
            {
                player.gameObject.SetActive(true);
            }

            if (spawnPoint != null)
            {
                player.transform.position = spawnPoint.transform.position;
                targetPosition = spawnPoint.transform.position;
                if (player.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.position = spawnPoint.transform.position;
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                targetPosition = player.transform.position;
            }
        }

        Physics2D.SyncTransforms();

        CameraLockArea area = FindTargetAreaInScene(targetScene, targetPosition);
        if (CameraController.Instance != null && area != null)
        {
            CameraController.Instance.ForceSetArea(area);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetupCanvasCamera();
        }
    }

    private IEnumerator LoadInitialRoomRoutine(string sceneName, string spawnPointName)
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (loadOp != null && !loadOp.isDone)
        {
            yield return null;
        }

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
        }

        bool isGameplay = IsGameplayScene(sceneName);
        SetPlayerActive(isGameplay);

        if (isGameplay)
        {
            InitializePlayerPosition(spawnPointName, loadedScene);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetHUDActive(isGameplay);
        }
    }

    private IEnumerator TransitionRoutine(string nextSceneName, string targetSpawnPointName)
    {
        Player player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        if (player != null && player.input != null)
        {
            player.input.Disable();
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
        }

        yield return StartCoroutine(FadeRoutine(0f, 1f));

        if (!string.IsNullOrEmpty(currentLoadedRoomScene) && currentLoadedRoomScene != nextSceneName)
        {
            Scene oldScene = SceneManager.GetSceneByName(currentLoadedRoomScene);
            if (oldScene.IsValid() && oldScene.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(oldScene);
                while (unloadOp != null && !unloadOp.isDone)
                {
                    yield return null;
                }
            }
        }

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
        while (loadOp != null && !loadOp.isDone)
        {
            yield return null;
        }

        currentLoadedRoomScene = nextSceneName;
        Scene newScene = SceneManager.GetSceneByName(nextSceneName);
        if (newScene.IsValid() && newScene.isLoaded)
        {
            SceneManager.SetActiveScene(newScene);
        }

        bool isGameplay = IsGameplayScene(nextSceneName);
        SetPlayerActive(isGameplay);

        if (isGameplay)
        {
            InitializePlayerPosition(targetSpawnPointName, newScene);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetHUDActive(isGameplay);
        }

        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(FadeRoutine(1f, 0f));

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (isGameplay)
        {
            player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
            if (player != null && player.input != null)
            {
                player.input.Enable();
            }
        }
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = toAlpha;
    }

    private CameraLockArea FindTargetAreaInScene(Scene scene, Vector2 position)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;

        CameraLockArea fallbackArea = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CameraLockArea[] areas = root.GetComponentsInChildren<CameraLockArea>(true);
            foreach (CameraLockArea area in areas)
            {
                if (fallbackArea == null) fallbackArea = area;

                if (position.x >= area.MinX && position.x <= area.MaxX &&
                    position.y >= area.MinY && position.y <= area.MaxY)
                {
                    return area;
                }
            }
        }

        return fallbackArea;
    }

    private GameObject FindObjectInScene(Scene scene, string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || !scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        string cleanPath = objectName.TrimStart('/');

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == cleanPath)
            {
                return root;
            }

            if (cleanPath.StartsWith(root.name + "/"))
            {
                string subPath = cleanPath.Substring(root.name.Length + 1);
                Transform target = root.transform.Find(subPath);
                if (target != null)
                {
                    return target.gameObject;
                }
            }

            Transform directTarget = root.transform.Find(cleanPath);
            if (directTarget != null)
            {
                return directTarget.gameObject;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == cleanPath)
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }
}
