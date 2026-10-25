using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Transition")]
    [Tooltip("Target gameplay scene to load after the menu screen.")]
    [SerializeField] private string targetSceneName = "Moss_Grotto_01";

    [Tooltip("Persistent core scene containing Player, CameraController, and Core Managers.")]
    [SerializeField] private string persistentCoreSceneName = "Core_Persistent";

    [Tooltip("Spawn point name or hierarchy path in the target scene.")]
    [SerializeField] private string targetSpawnPointName = "_Spawns/Left_Door";

    [Header("UI References")]
    [Tooltip("Full-screen button to capture mouse clicks anywhere on the screen.")]
    [SerializeField] private Button fullScreenButton;

    [Tooltip("Optional blinking text prompt on the menu screen.")]
    [SerializeField] private TextMeshProUGUI promptText;

    [Tooltip("CanvasGroup controlling fade-out transition to black.")]
    [SerializeField] private CanvasGroup screenFadeGroup;

    [Header("Transition Settings")]
    [Tooltip("Duration of the fade transition in seconds.")]
    [SerializeField] private float fadeDuration = 0.8f;

    [Tooltip("Blinking speed of the prompt text.")]
    [SerializeField] private float promptBlinkSpeed = 2.0f;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (screenFadeGroup != null)
        {
            screenFadeGroup.alpha = 0f;
            screenFadeGroup.blocksRaycasts = false;
        }

        if (fullScreenButton != null)
        {
            fullScreenButton.onClick.RemoveAllListeners();
            fullScreenButton.onClick.AddListener(OnScreenClicked);
        }
    }

    private void Update()
    {
        if (promptText != null && !isTransitioning)
        {
            float alpha = (Mathf.Sin(Time.time * promptBlinkSpeed) + 1f) * 0.5f;
            Color col = promptText.color;
            col.a = alpha;
            promptText.color = col;
        }

        if (!isTransitioning)
        {
            bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                escapePressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                escapePressed = true;
#endif
            if (escapePressed)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                return;
#else
                Application.Quit();
                return;
#endif
            }

            bool clickDetected = false;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                clickDetected = true;
            else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                clickDetected = true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                clickDetected = true;
            }
#endif

            if (clickDetected)
            {
                OnScreenClicked();
            }
        }
    }

    public void OnScreenClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        StartCoroutine(TransitionToGameRoutine());
    }

    private IEnumerator TransitionToGameRoutine()
    {
        if (fullScreenButton != null)
        {
            fullScreenButton.interactable = false;
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }

        if (screenFadeGroup != null)
        {
            screenFadeGroup.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                screenFadeGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            screenFadeGroup.alpha = 1f;
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        bool isCoreLoaded = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == persistentCoreSceneName)
            {
                isCoreLoaded = true;
                break;
            }
        }

        if (!isCoreLoaded && !string.IsNullOrEmpty(persistentCoreSceneName))
        {
            AsyncOperation coreOp = SceneManager.LoadSceneAsync(persistentCoreSceneName, LoadSceneMode.Additive);
            while (coreOp != null && !coreOp.isDone) yield return null;
        }

        AsyncOperation roomOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        while (roomOp != null && !roomOp.isDone) yield return null;

        Scene targetScene = SceneManager.GetSceneByName(targetSceneName);
        if (targetScene.IsValid() && targetScene.isLoaded)
        {
            SceneManager.SetActiveScene(targetScene);
        }

        // Delegate spawn point search, player positioning, and camera lock to SceneTransitionManager
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.InitializePlayerPosition(targetSpawnPointName, targetScene);
        }

        Scene menuScene = gameObject.scene;
        if (menuScene.IsValid() && menuScene.isLoaded && menuScene.name != targetSceneName)
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(menuScene);
            while (unloadOp != null && !unloadOp.isDone) yield return null;
        }

        yield return new WaitForEndOfFrame();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshHUD();
        }
    }
}
