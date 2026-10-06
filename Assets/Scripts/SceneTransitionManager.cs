using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [Tooltip("CanvasGroup controlling transition overlay opacity.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Tooltip("Duration in seconds for the screen fade transition.")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Spawn Settings")]
    [Tooltip("Default spawn point identifier to search for when entering a room.")]
    [SerializeField] private string defaultSpawnPointName;

    [Header("Startup")]
    [Tooltip("Initial room scene to load when starting from persistent core.")]
    [SerializeField] private string initialRoomScene;

    private string currentLoadedRoomScene;
    private string persistentSceneName;

    private void Awake()
    {
        persistentSceneName = gameObject.scene.name;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.name != persistentSceneName)
        {
            currentLoadedRoomScene = activeScene.name;
            InitializePlayerPosition(defaultSpawnPointName, activeScene);
        }
        else if (!string.IsNullOrEmpty(initialRoomScene))
        {
            currentLoadedRoomScene = initialRoomScene;
            StartCoroutine(LoadInitialRoomRoutine(initialRoomScene, defaultSpawnPointName));
        }
    }

    public void TransitionToScene(string nextSceneName, string targetSpawnPointName)
    {
        StartCoroutine(TransitionRoutine(nextSceneName, targetSpawnPointName));
    }

    private void InitializePlayerPosition(string spawnPointName, Scene targetScene)
    {
        GameObject spawnPoint = FindObjectInScene(targetScene, spawnPointName);
        Player player = Object.FindAnyObjectByType<Player>();

        Vector2 targetPosition = Vector2.zero;

        if (spawnPoint != null && player != null)
        {
            player.transform.position = spawnPoint.transform.position;
            targetPosition = spawnPoint.transform.position;
            if (player.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.position = spawnPoint.transform.position;
                rb.linearVelocity = Vector2.zero;
            }
        }
        else if (player != null)
        {
            targetPosition = player.transform.position;
        }

        Physics2D.SyncTransforms();

        CameraLockArea area = FindTargetAreaInScene(targetScene, targetPosition);
        if (CameraController.Instance != null && area != null)
        {
            CameraController.Instance.ForceSetArea(area);
        }
    }

    private IEnumerator LoadInitialRoomRoutine(string sceneName, string spawnPointName)
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone)
        {
            yield return null;
        }

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);
        InitializePlayerPosition(spawnPointName, loadedScene);
    }

    private IEnumerator TransitionRoutine(string nextSceneName, string targetSpawnPointName)
    {
        Player player = Object.FindAnyObjectByType<Player>();
        if (player != null && player.input != null)
        {
            player.input.Disable();
        }

        yield return StartCoroutine(FadeRoutine(0f, 1f));

        if (!string.IsNullOrEmpty(currentLoadedRoomScene))
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentLoadedRoomScene);
            while (unloadOp != null && !unloadOp.isDone)
            {
                yield return null;
            }
        }

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone)
        {
            yield return null;
        }

        currentLoadedRoomScene = nextSceneName;
        Scene newScene = SceneManager.GetSceneByName(nextSceneName);
        SceneManager.SetActiveScene(newScene);

        GameObject spawnPoint = FindObjectInScene(newScene, targetSpawnPointName);
        Vector2 targetPosition = Vector2.zero;

        if (spawnPoint != null && player != null)
        {
            player.transform.position = spawnPoint.transform.position;
            targetPosition = spawnPoint.transform.position;
            if (player.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.position = spawnPoint.transform.position;
                rb.linearVelocity = Vector2.zero;
            }
        }
        else if (player != null)
        {
            targetPosition = player.transform.position;
        }

        Physics2D.SyncTransforms();

        CameraLockArea newArea = FindTargetAreaInScene(newScene, targetPosition);
        if (CameraController.Instance != null && newArea != null)
        {
            CameraController.Instance.ForceSetArea(newArea);
        }

        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(FadeRoutine(1f, 0f));

        if (player != null && player.input != null)
        {
            player.input.Enable();
        }
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha)
    {
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
