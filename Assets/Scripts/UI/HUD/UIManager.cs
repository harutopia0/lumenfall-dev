using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

using Lumenfall.Core;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private Image fadeIn;

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
            targetCamera = CameraController.Instance.Cam;
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

            c.worldCamera = targetCamera;
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

        Player player = FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        if (player != null)
        {
            Player_Health hp = player.GetComponent<Player_Health>();
            if (hp != null)
            {
                EventBus.Publish(new PlayerHealthChangedEvent(hp.CurrentMasks, hp.MaxMasks, true));
            }
            Player_Soul sl = player.GetComponent<Player_Soul>();
            if (sl != null)
            {
                EventBus.Publish(new PlayerSoulChangedEvent(sl.CurrentSoul, sl.MaxSoul, true));
            }
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
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

        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
        {
            SetHUDActive(true);
        }
    }
}
