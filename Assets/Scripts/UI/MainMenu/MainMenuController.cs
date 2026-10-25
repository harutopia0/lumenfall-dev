using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Level Target")]
    [Tooltip("Target gameplay scene to load when starting the game.")]
    [SerializeField] private string targetSceneName = SceneNames.DefaultRoom;

    [Tooltip("Spawn point identifier in the target scene.")]
    [SerializeField] private string targetSpawnPointName = SceneNames.DefaultSpawnPoint;

    [Header("UI References")]
    [Tooltip("Full-screen button to capture click/tap anywhere on screen.")]
    [SerializeField] private Button fullScreenButton;

    [Tooltip("Optional blinking prompt text (e.g. 'Press anywhere to start').")]
    [SerializeField] private TextMeshProUGUI promptText;

    [Tooltip("Blinking speed of the prompt text.")]
    [SerializeField] private float promptBlinkSpeed = 2.0f;

    private bool isTransitioning = false;

    private void Awake()
    {
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

        if (isTransitioning) return;

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
#else
            Application.Quit();
#endif
            return;
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

    public void OnScreenClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (fullScreenButton != null)
        {
            fullScreenButton.interactable = false;
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnPointName);
        }
    }
}
