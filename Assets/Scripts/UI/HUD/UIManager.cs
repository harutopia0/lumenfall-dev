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

    private void SetupCanvasCamera()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
            canvas.planeDistance = 100f;
        }
    }

    private void OnEnable()
    {
        Player_Health.OnPlayerHealthChanged += HandlePlayerHealthChanged;
        Player_Soul.OnPlayerSoulChanged += HandlePlayerSoulChanged;
    }

    private void OnDisable()
    {
        Player_Health.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
        Player_Soul.OnPlayerSoulChanged -= HandlePlayerSoulChanged;
    }

    private void Start()
    {
        SetupCanvasCamera();

        if (fadeIn != null)
        {
            fadeIn.DOFade(0, 1);
        }

        // Failsafe: Query player if already active in scene
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
        float soulModifier = 33f / maxVal; // e.g. 0.333f for Focus or spell cost

        bool isIncrease = (lastSoul == -1) || (currentSoul >= lastSoul);
        lastSoul = currentSoul;

        updateSoul.Invoke(normalizedCurrent, normalizedMax, soulModifier);

        if (isIncrease)
        {
            updateSoulAnimation.Invoke(10f); // Wave splash
        }
        else
        {
            updateSoulAnimation.Invoke(0.4f); // Wave drain
        }
    }
}
