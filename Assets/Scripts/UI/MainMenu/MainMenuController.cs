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

    [Tooltip("Optional prompt root GameObject containing the banner and label.")]
    [SerializeField] private GameObject promptRoot;

    [Tooltip("Optional CanvasGroup on prompt root for pulsing breathing animation.")]
    [SerializeField] private CanvasGroup promptCanvasGroup;

    [Tooltip("Optional prompt text component.")]
    [SerializeField] private TextMeshProUGUI promptText;

    [Tooltip("Pulse animation speed for the prompt.")]
    [SerializeField] private float promptBlinkSpeed = 3.14159f;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (fullScreenButton != null)
        {
            fullScreenButton.onClick.RemoveAllListeners();
            fullScreenButton.onClick.AddListener(OnScreenClicked);
        }
    }

    private void OnDestroy()
    {
        if (fullScreenButton != null)
        {
            fullScreenButton.onClick.RemoveListener(OnScreenClicked);
        }
    }

    private void Update()
    {
        if (!isTransitioning)
        {
            float t = (Mathf.Sin(Time.time * promptBlinkSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.80f, 1.0f, t);

            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = alpha;
            }
            else if (promptText != null)
            {
                Color col = promptText.color;
                col.a = alpha;
                promptText.color = col;
            }
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

    }

    public void OnScreenClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (fullScreenButton != null)
        {
            fullScreenButton.interactable = false;
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 1f;
        }
        else if (promptText != null)
        {
            Color col = promptText.color;
            col.a = 1f;
            promptText.color = col;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnPointName);
        }
        else
        {
            Debug.LogError("[MainMenuController] SceneTransitionManager.Instance is null! Cannot transition.");
            isTransitioning = false;
            if (fullScreenButton != null) fullScreenButton.interactable = true;
        }
    }
}
