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

    private void Awake()
    {
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

        if (activeScene.name != gameObject.scene.name)
        {
            currentLoadedRoomScene = activeScene.name;
            InitializePlayerPosition(defaultSpawnPointName);
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

    private void InitializePlayerPosition(string spawnPointName)
    {
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        Player player = Object.FindAnyObjectByType<Player>();

        if (spawnPoint != null && player != null)
        {
            player.transform.position = spawnPoint.transform.position;
            if (player.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        CameraLockArea area = Object.FindAnyObjectByType<CameraLockArea>();
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
        InitializePlayerPosition(spawnPointName);
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

        GameObject spawnPoint = GameObject.Find(targetSpawnPointName);
        if (spawnPoint != null && player != null)
        {
            player.transform.position = spawnPoint.transform.position;
            if (player.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        CameraLockArea newArea = Object.FindAnyObjectByType<CameraLockArea>();
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
}
