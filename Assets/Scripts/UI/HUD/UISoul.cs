using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using Lumenfall.Core;
using Lumenfall.UI;

public class UISoul : MonoBehaviour
{
    [SerializeField] SoulParticles soulParticles;
    [SerializeField] Image eyes;
    [SerializeField] Material soulMaterial;
    [SerializeField] private Image soulImage;
    private Material soulMaterialInstance;
    private Material eyesMaterialInstance;
    [SerializeField] float newFill;
    [SerializeField] float currentFill;
    [SerializeField] VisualEffect bubbles;
    [SerializeField] VisualEffect soulIncrease;
    [SerializeField] float waveLength = 1f;
    [SerializeField] float glowStrenght = 0f;
    [SerializeField] float eyeEdgeStrenght = 1f;
    [SerializeField] float glowSpeed = 0.2f;
    [SerializeField] float glowPulsingSpeed = 0.3f;
    [SerializeField] float waveLenghtSpeed = 0.1f;
    [Range(0, 1)][SerializeField] float eyeAppearThreshold = 0.33f;
    [Range(0, 1)][SerializeField] float transparencyThreshold;

    Sequence waveAnimation;
    Sequence glowAnimation;
    Sequence pulseAnimation;
    Sequence eyeAnimation;
    Tween fillTween;
    float maxSoul = 99f;
    float currentSoul = 0f;
    bool hasSoulCharge = false;
    bool isInitialized = false;

    [Header("Fill Mapping")]
    [Tooltip("Shader _fillAmount at low soul level (lifted to prevent soul from appearing too low).")]
    [SerializeField] private float minShaderFill = 0.05f;
    [Tooltip("Shader _fillAmount around 88% soul (keeps waves visible before max).")]
    [SerializeField] private float midShaderFill = 0.72f;
    [Tooltip("Shader _fillAmount when soul is 100% max (submerges vessel so waves completely disappear).")]
    [SerializeField] private float maxShaderFill = 1.02f;
    [Tooltip("Curvature exponent (< 1.0 lifts 33-soul / spell charges to match spherical volume).")]
    [SerializeField] private float fillCurvePower = 0.85f;

    [Header("Liquid Skin")]
    [SerializeField] private Texture2D liquidTexture;
    private readonly SoulFluidTextureGenerator fluidGenerator = new SoulFluidTextureGenerator();

    public float RemapSoulToShaderFill(float soulRatio)
    {
        if (soulRatio <= 0.001f) return minShaderFill - 0.10f;
        float r = Mathf.Clamp01(soulRatio);
        if (r < 0.88f)
        {
            float t = r / 0.88f;
            float curvedT = Mathf.Pow(t, fillCurvePower);
            return Mathf.Lerp(minShaderFill, midShaderFill, curvedT);
        }

        float s = (r - 0.88f) / 0.12f;
        float smoothS = s * s * (3f - 2f * s);
        return Mathf.Lerp(midShaderFill, maxShaderFill, smoothS);
    }

    private void Awake()
    {
        if (soulMaterial != null)
        {
            soulMaterialInstance = new Material(soulMaterial);
            soulMaterialInstance.name = soulMaterial.name + " (Instance)";
            soulMaterial = soulMaterialInstance;

            if (soulImage == null)
            {
                Image[] images = GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img != null && img != eyes && img.material != null &&
                        (img.material.shader == soulMaterialInstance.shader || img.name.Contains("Soul") || img.name == "Image"))
                    {
                        soulImage = img;
                        break;
                    }
                }
            }

            if (soulImage != null)
            {
                soulImage.material = soulMaterialInstance;
            }
        }

        if (eyes != null && eyes.material != null)
        {
            eyesMaterialInstance = new Material(eyes.material);
            eyesMaterialInstance.name = eyes.material.name + " (Instance)";
            eyes.material = eyesMaterialInstance;
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerSoulChangedEvent>(OnSoulChanged);
    }

    private void Start()
    {
        UpdateFluidColor(0f);

        if (soulMaterial != null)
        {
            currentFill = 0f;
            newFill = 0f;
            soulMaterial.SetFloat("_fillAmount", RemapSoulToShaderFill(0f));
            soulMaterial.SetFloat("_fillAlpha", 0f);
            soulMaterial.SetFloat("_glowMultiplier", 0f);
            soulMaterial.SetFloat("_waveMultiplier", 1f);
        }

        if (pulseAnimation == null || !pulseAnimation.IsActive())
        {
            glowStrenght = 0f;
            eyeEdgeStrenght = 1f;
            if (eyes != null && eyes.material != null)
            {
                UpdateMaterial("_edgeMultiplier", eyeEdgeStrenght, eyes.material);
            }
        }
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerSoulChangedEvent>(OnSoulChanged);

        DOTween.Kill("SoulPulse");
        fillTween?.Kill();
        waveAnimation?.Kill();
        glowAnimation?.Kill();
        pulseAnimation?.Kill();
        pulseAnimation = null;
        eyeAnimation?.Kill();
    }

    private void OnSoulChanged(PlayerSoulChangedEvent evt)
    {
        UpdateSoulFill(evt.NormalizedCurrent, evt.NormalizedMax, evt.SoulModifier, evt.HasCharge, evt.IsFullSoul);
        LenghtenWaves(evt.WaveAffector);
    }

    private void Update()
    {
        if (soulMaterial != null)
        {
            UpdateMaterial("_waveMultiplier", waveLength, soulMaterial);
            UpdateMaterial("_glowMultiplier", glowStrenght, soulMaterial);
        }

        if (eyes != null && eyes.material != null)
        {
            UpdateMaterial("_edgeMultiplier", eyeEdgeStrenght, eyes.material);
        }
    }

    void GlowPulsing()
    {
        DOTween.Kill("SoulPulse");
        pulseAnimation?.Kill();
        glowAnimation?.Kill();
        pulseAnimation = DOTween.Sequence();
        pulseAnimation.SetId("SoulPulse");
        pulseAnimation.SetLoops(-1);

        float start = 1f;
        float end = 0f;
        glowStrenght = 1f;
        eyeEdgeStrenght = 0f;

        pulseAnimation.Append(DOTween.To(() => glowStrenght, x => glowStrenght = x, end, glowPulsingSpeed));
        pulseAnimation.Join(DOTween.To(() => eyeEdgeStrenght, x => eyeEdgeStrenght = x, start, glowPulsingSpeed));
        pulseAnimation.Append(DOTween.To(() => glowStrenght, x => glowStrenght = x, start, glowPulsingSpeed));
        pulseAnimation.Join(DOTween.To(() => eyeEdgeStrenght, x => eyeEdgeStrenght = x, end, glowPulsingSpeed));
    }

    void UpdateMaterial(string _name, float _value, Material _material)
    {
        if (_material != null && !Mathf.Approximately(_material.GetFloat(_name), _value))
        {
            _material.SetFloat(_name, _value);
        }
    }

    private void UpdateSoulFill(float _currentSoul, float _maxSoul, float _soulModifier, bool? explicitHasCharge = null, bool? explicitIsFull = null)
    {
        float prevSoul = currentSoul;
        currentSoul = _currentSoul;
        maxSoul = _maxSoul;

        if (eyes != null)
        {
            if (_currentSoul >= eyeAppearThreshold)
            {
                if (eyes.transform.localScale == new Vector3(1f, 0f, 1f) || eyes.transform.localScale.y < 0.5f)
                {
                    eyeAnimation?.Kill();
                    eyes.transform.localScale = new Vector3(1f, 0f, 1f);
                    eyeAnimation = DOTween.Sequence();
                    eyeAnimation.Append(eyes.transform.DOScale(new Vector3(1f, 1f, 1f), 0.13f).SetEase(Ease.OutSine));
                }
                else
                {
                    eyeAnimation?.Kill();
                }
            }
            else
            {
                if (eyes.transform.localScale == new Vector3(1f, 1f, 1f) || eyes.transform.localScale.y > 0.5f)
                {
                    eyeAnimation?.Kill();
                    eyes.transform.localScale = new Vector3(1f, 1f, 1f);
                    eyeAnimation = DOTween.Sequence();
                    eyeAnimation.Append(eyes.transform.DOScale(new Vector3(1f, 0f, 1f), 0.15f).SetEase(Ease.OutElastic));
                }
                else
                {
                    eyeAnimation?.Kill();
                }
            }
        }

        bool chargeAvailable = explicitHasCharge.HasValue ? explicitHasCharge.Value : (_currentSoul >= (_soulModifier - 0.001f));

        if (!isInitialized)
        {
            isInitialized = true;
            hasSoulCharge = chargeAvailable;
        }
        else
        {
            if (chargeAvailable && !hasSoulCharge)
            {
                hasSoulCharge = true;
                if (soulParticles != null)
                {
                    soulParticles.Spawn();
                }
                else if (soulIncrease != null)
                {
                    soulIncrease.Play();
                }
            }
            else if (!chargeAvailable)
            {
                hasSoulCharge = false;
            }
        }

        if (soulMaterial != null)
        {
            if (_currentSoul > 0.001f)
            {
                soulMaterial.SetFloat("_fillAlpha", 1f);
            }

            newFill = _maxSoul > 0 ? Mathf.Clamp01(_currentSoul / _maxSoul) : 0f;
            float startFill = currentFill;

            fillTween?.Kill();
            fillTween = DOTween.To(() => startFill, x =>
            {
                currentFill = x;
                if (soulMaterial != null)
                {
                    soulMaterial.SetFloat("_fillAmount", RemapSoulToShaderFill(currentFill));
                }
                UpdateFluidColor(currentFill);
            }, newFill, 0.2f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                currentFill = newFill;
                if (soulMaterial != null)
                {
                    soulMaterial.SetFloat("_waveMultiplier", 1f);
                    soulMaterial.SetFloat("_fillAmount", RemapSoulToShaderFill(newFill));
                    if (_currentSoul <= 0.001f)
                    {
                        soulMaterial.SetFloat("_fillAlpha", 0f);
                    }
                }
                UpdateFluidColor(newFill);
            });
        }

        bool isFull = explicitIsFull.HasValue
            ? explicitIsFull.Value
            : (_maxSoul > 0 && (_currentSoul >= (_maxSoul - 0.001f)));

        if (isFull)
        {
            if (pulseAnimation == null || !pulseAnimation.IsActive() || !pulseAnimation.IsPlaying())
            {
                GlowPulsing();
            }
        }
        else
        {
            DOTween.Kill("SoulPulse");
            pulseAnimation?.Kill();
            pulseAnimation = null;
            glowAnimation?.Kill();
            glowAnimation = null;
            eyeEdgeStrenght = 1f;
            glowStrenght = 0f;
            if (eyes != null && eyes.material != null)
            {
                UpdateMaterial("_edgeMultiplier", 1f, eyes.material);
            }
            if (soulMaterial != null)
            {
                UpdateMaterial("_glowMultiplier", 0f, soulMaterial);
            }
        }
    }

    private void GlowPulse(float _waveAffector)
    {
        if (_waveAffector > 1f && currentSoul != maxSoul)
        {
            glowAnimation?.Kill();
            glowAnimation = DOTween.Sequence();
            float start = 1f;
            float end = 0f;
            glowStrenght = 0f;
            eyeEdgeStrenght = 1f;

            glowAnimation.Append(DOTween.To(() => glowStrenght, x => glowStrenght = x, start, glowSpeed / 2f));
            glowAnimation.Join(DOTween.To(() => eyeEdgeStrenght, x => eyeEdgeStrenght = x, end, glowSpeed / 2f));
            glowAnimation.Append(DOTween.To(() => glowStrenght, x => glowStrenght = x, end, glowSpeed));
            glowAnimation.Join(DOTween.To(() => eyeEdgeStrenght, x => eyeEdgeStrenght = x, start, glowSpeed));
        }
    }

    private void LenghtenWaves(float _waveAffector)
    {
        waveAnimation?.Kill();
        waveAnimation = DOTween.Sequence();
        float start = 1f;
        waveLength = 1f;

        if (_waveAffector < 1f && currentSoul != 0)
        {
            if (bubbles != null)
            {
                bubbles.Play();
            }
        }

        waveAnimation.Append(DOTween.To(() => waveLength, x => waveLength = x, _waveAffector, waveLenghtSpeed / 2f));
        waveAnimation.Append(DOTween.To(() => waveLength, x => waveLength = x, start, waveLenghtSpeed));
    }

    public void UpdateFluidColor(float fillRatio = 1f)
    {
        fluidGenerator.UpdateFluidColor(soulMaterial, liquidTexture, fillRatio, RemapSoulToShaderFill);
    }

#if UNITY_EDITOR
    [ContextMenu("Bake Liquid Color To Disk")]
    public void BakeLiquidTextureToDisk()
    {
        if (liquidTexture == null)
        {
            Debug.LogWarning("[UISoul] Cannot bake: liquidTexture is null!");
            return;
        }

        SoulFluidTextureGenerator.ExtractLiquidGradient(liquidTexture, out Color shadow, out Color body, out Color highlight);
        Texture2D baked = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        baked.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 256; y++)
        {
            float normY = y / 255f;
            Color baseColor = SoulFluidTextureGenerator.SampleLiquidColor(normY, shadow, body, highlight);
            baseColor.a = 1f;

            for (int x = 0; x < 256; x++)
            {
                baked.SetPixel(x, y, baseColor);
            }
        }
        baked.Apply();

        string assetPath = "Assets/Graphics/HUD/HUD_Soul_LiquidColor.png";
        string fullPath = System.IO.Path.Combine(Application.dataPath, "Graphics/HUD/HUD_Soul_LiquidColor.png");
        byte[] pngData = baked.EncodeToPNG();
        DestroyImmediate(baked);

        System.IO.File.WriteAllBytes(fullPath, pngData);
        UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
        Debug.Log($"[UISoul] Successfully baked new liquid gradient to {assetPath}!");
    }

    private Texture2D lastLiquidTexture;
    private void OnValidate()
    {
        if (liquidTexture != null && liquidTexture != lastLiquidTexture && !Application.isPlaying)
        {
            lastLiquidTexture = liquidTexture;
            fluidGenerator.InvalidateCache();
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && liquidTexture != null)
                {
                    BakeLiquidTextureToDisk();
                }
            };
        }
    }
#endif

    private void OnDestroy()
    {
        DOTween.Kill("SoulPulse");
        fluidGenerator?.Cleanup();

        if (soulMaterialInstance != null)
        {
            SafeDestroy(soulMaterialInstance);
            soulMaterialInstance = null;
        }

        if (eyesMaterialInstance != null)
        {
            SafeDestroy(eyesMaterialInstance);
            eyesMaterialInstance = null;
        }
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }
}
