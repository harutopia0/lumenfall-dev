using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private Image fadeIn;

    [Header("UI Events")]
    public static UnityEvent<int, bool> updateHealth = new UnityEvent<int, bool>();
    public static UnityEvent<float, float, float> updateSoul = new UnityEvent<float, float, float>();
    public static UnityEvent<float> updateSoulAnimation = new UnityEvent<float>();

    private int lastHealth = -1;
    private int lastSoul = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupCanvasCamera();
    }

    public void SetupCanvasCamera()
    {
        Camera targetCamera = null;

        if (CameraController.Instance != null)
        {
            targetCamera = CameraController.Instance.GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            targetCamera = FindAnyObjectByType<Camera>();
        }

        if (targetCamera == null) return;

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas c in canvases)
        {
            if (c.renderMode != RenderMode.ScreenSpaceCamera)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
            }

            if (c.worldCamera != targetCamera)
            {
                c.worldCamera = targetCamera;
            }
        }

        RenderSettings.fog = false;
    }

    public void SetHUDActive(bool active)
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas c in canvases)
        {
            c.enabled = active;
        }

        if (active)
        {
            RefreshHUD();
        }
    }

    public void RefreshHUD()
    {
        SetupCanvasCamera();

        Player player = FindAnyObjectByType<Player>();
        if (player != null)
        {
            Player_Health hp = player.GetComponent<Player_Health>();
            if (hp != null)
            {
                HandlePlayerHealthChanged(hp.CurrentMasks, hp.MaxMasks);
            }
            Player_Soul sl = player.GetComponent<Player_Soul>();
            if (sl != null)
            {
                HandlePlayerSoulChanged(sl.CurrentSoul, sl.MaxSoul);
            }
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        Player_Health.OnPlayerHealthChanged += HandlePlayerHealthChanged;
        Player_Soul.OnPlayerSoulChanged += HandlePlayerSoulChanged;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        Player_Health.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
        Player_Soul.OnPlayerSoulChanged -= HandlePlayerSoulChanged;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        RefreshHUD();
    }

    private void Start()
    {
        SetupCanvasCamera();

        if (fadeIn != null)
        {
            fadeIn.DOFade(0, 1);
        }

        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsMenuScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
        {
            SetHUDActive(false);
            return;
        }

        Player player = FindAnyObjectByType<Player>();
        if (player != null)
        {
            Player_Health hp = player.GetComponent<Player_Health>();
            if (hp != null)
            {
                HandlePlayerHealthChanged(hp.CurrentMasks, hp.MaxMasks);
            }
            Player_Soul sl = player.GetComponent<Player_Soul>();
            if (sl != null)
            {
                HandlePlayerSoulChanged(sl.CurrentSoul, sl.MaxSoul);
            }
        }
    }

    private void HandlePlayerHealthChanged(int currentHealth, int maxHealth)
    {
        bool isIncrease = (lastHealth == -1) || (currentHealth >= lastHealth);
        lastHealth = currentHealth;
        updateHealth.Invoke(currentHealth, isIncrease);
    }

    private void HandlePlayerSoulChanged(int currentSoul, int maxSoul)
    {
        float maxVal = maxSoul > 0 ? (float)maxSoul : 99f;
        float normalizedCurrent = Mathf.Clamp01((float)currentSoul / maxVal);
        float normalizedMax = 1f;
        float soulModifier = 33f / maxVal;

        bool isIncrease = (lastSoul == -1) || (currentSoul >= lastSoul);
        lastSoul = currentSoul;

        updateSoul.Invoke(normalizedCurrent, normalizedMax, soulModifier);

        if (isIncrease)
        {
            updateSoulAnimation.Invoke(1.2f);
        }
        else
        {
            updateSoulAnimation.Invoke(0.4f);
        }
    }
}
