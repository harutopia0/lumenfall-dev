using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class UISoul : MonoBehaviour
{
    [SerializeField] SoulParticles soulParticles;
    [SerializeField] Image eyes;
    [SerializeField] Material soulMaterial;
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
    private Texture2D generatedFluidTexture;
    private Color cachedShadow = new Color(0.1f, 0.15f, 0.3f);
    private Color cachedBody = new Color(0.15f, 0.25f, 0.5f);
    private Color cachedHighlight = Color.white;
    private bool colorsCached = false;
    private Color[] pixelBuffer;
    private const int FluidTexSize = 128;

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

    private void Start()
    {
        UpdateFluidColor(0f);

        UIManager.updateSoul.AddListener(UpdateSoulFill);
        UIManager.updateSoulAnimation.AddListener(LenghtenWaves);

        if (soulMaterial != null)
        {
            currentFill = 0f;
            newFill = 0f;
            soulMaterial.SetFloat("_fillAmount", RemapSoulToShaderFill(0f));
            soulMaterial.SetFloat("_fillAlpha", 0f);
            soulMaterial.SetFloat("_glowMultiplier", 0f);
            soulMaterial.SetFloat("_waveMultiplier", 1f);
        }

        waveAnimation = DOTween.Sequence();
        glowAnimation = DOTween.Sequence();
        pulseAnimation = DOTween.Sequence();
        eyeAnimation = DOTween.Sequence();
        glowStrenght = 0f;
    }

    private void OnDisable()
    {
        UIManager.updateSoul.RemoveListener(UpdateSoulFill);
        UIManager.updateSoulAnimation.RemoveListener(LenghtenWaves);

        fillTween?.Kill();
        waveAnimation?.Kill();
        glowAnimation?.Kill();
        pulseAnimation?.Kill();
        eyeAnimation?.Kill();
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
        pulseAnimation?.Kill();
        glowAnimation?.Kill();
        pulseAnimation = DOTween.Sequence();
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

    private void UpdateSoulFill(float _currentSoul, float _maxSoul, float _soulModifier)
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

        if (!isInitialized)
        {
            isInitialized = true;
            hasSoulCharge = _currentSoul >= _soulModifier;
        }
        else
        {
            if (_currentSoul >= _soulModifier && !hasSoulCharge)
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
            else if (_currentSoul < _soulModifier)
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

        if (Mathf.Approximately(_currentSoul, _maxSoul) && _maxSoul > 0)
        {
            GlowPulsing();
        }
        else
        {
            glowAnimation?.Kill();
            pulseAnimation?.Kill();
            eyeEdgeStrenght = 1f;
            glowStrenght = 0f;
            if (eyes != null && eyes.material != null)
            {
                UpdateMaterial("_edgeMultiplier", eyeEdgeStrenght, eyes.material);
            }
            if (soulMaterial != null)
            {
                UpdateMaterial("_glowMultiplier", glowStrenght, soulMaterial);
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
        if (soulMaterial == null || liquidTexture == null) return;

        if (!colorsCached)
        {
            ExtractLiquidGradient(liquidTexture, out cachedShadow, out cachedBody, out cachedHighlight);
            colorsCached = true;
        }

        if (generatedFluidTexture == null)
        {
            generatedFluidTexture = new Texture2D(FluidTexSize, FluidTexSize, TextureFormat.RGBA32, false);
            generatedFluidTexture.wrapMode = TextureWrapMode.Clamp;
            pixelBuffer = new Color[FluidTexSize * FluidTexSize];
            soulMaterial.SetTexture("_smallerTex", generatedFluidTexture);
        }

        UpdateFluidTexturePixels(fillRatio);
    }

    private Color SampleLiquidColor(float v, Color shadow, Color body, Color highlight)
    {
        if (v < 0.5f)
        {
            float t = v / 0.5f;
            return Color.Lerp(shadow, body, t);
        }
        else
        {
            float t = (v - 0.5f) / 0.5f;
            return Color.Lerp(body, highlight, t);
        }
    }

    private void UpdateFluidTexturePixels(float fillRatio)
    {
        if (generatedFluidTexture == null || pixelBuffer == null) return;

        float actualWaterLevel = Mathf.Clamp01(RemapSoulToShaderFill(fillRatio));
        float center = FluidTexSize / 2f;

        for (int y = 0; y < FluidTexSize; y++)
        {
            float normY = y / (float)(FluidTexSize - 1);
            float v = Mathf.Clamp01(1f - actualWaterLevel + normY);
            Color baseColor = SampleLiquidColor(v, cachedShadow, cachedBody, cachedHighlight);

            for (int x = 0; x < FluidTexSize; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha;
                if (dist <= 0.78f)
                {
                    alpha = 1f;
                }
                else if (dist <= 0.90f)
                {
                    float st = (dist - 0.78f) / (0.90f - 0.78f);
                    alpha = 1f - (st * st * (3f - 2f * st));
                }
                else
                {
                    alpha = 0f;
                }

                if (alpha > 0f)
                {
                    float radialGlow = Mathf.Max(0f, 1f - dist / 0.78f);
                    Color finalColor = baseColor * (1f + 0.15f * radialGlow);
                    finalColor.a = alpha;
                    pixelBuffer[y * FluidTexSize + x] = finalColor;
                }
                else
                {
                    pixelBuffer[y * FluidTexSize + x] = Color.clear;
                }
            }
        }

        generatedFluidTexture.SetPixels(pixelBuffer);
        generatedFluidTexture.Apply();
    }

    private void ExtractLiquidGradient(Texture2D source, out Color shadow, out Color body, out Color highlight)
    {
        shadow = new Color(0.118f, 0.192f, 0.345f);
        body = new Color(0.424f, 0.502f, 0.655f);
        highlight = new Color(0.796f, 0.875f, 1.0f);

        try
        {
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            RenderTexture prevRT = RenderTexture.active;
            RenderTexture.active = rt;

            int sampleH = Mathf.Max(1, rt.height / 2);
            int sampleW = Mathf.Max(1, rt.width);
            Texture2D temp = new Texture2D(sampleW, sampleH, TextureFormat.RGBA32, false);
            temp.ReadPixels(new Rect(0, 0, sampleW, sampleH), 0, 0);
            temp.Apply();

            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt);

            Color[] pixels = temp.GetPixels();
            if (Application.isPlaying)
            {
                Destroy(temp);
            }
            else
            {
                DestroyImmediate(temp);
            }

            var seen = new System.Collections.Generic.HashSet<int>();
            var uniqueLiquidColors = new System.Collections.Generic.List<Color>();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                if (c.a > 0.35f)
                {
                    if (c.r > 0.90f && c.g > 0.90f && c.b > 0.90f)
                    {
                        continue;
                    }

                    int r = Mathf.RoundToInt(c.r * 255f);
                    int g = Mathf.RoundToInt(c.g * 255f);
                    int b = Mathf.RoundToInt(c.b * 255f);
                    int key = (r << 16) | (g << 8) | b;

                    if (seen.Add(key))
                    {
                        uniqueLiquidColors.Add(c);
                    }
                }
            }

            if (uniqueLiquidColors.Count > 0)
            {
                uniqueLiquidColors.Sort((a, b) =>
                {
                    float lumA = 0.299f * a.r + 0.587f * a.g + 0.114f * a.b;
                    float lumB = 0.299f * b.r + 0.587f * b.g + 0.114f * b.b;
                    return lumA.CompareTo(lumB);
                });

                int count = uniqueLiquidColors.Count;
                shadow = uniqueLiquidColors[Mathf.Clamp((int)(count * 0.05f), 0, count - 1)];
                body = uniqueLiquidColors[Mathf.Clamp((int)(count * 0.50f), 0, count - 1)];
                highlight = uniqueLiquidColors[Mathf.Clamp((int)(count * 0.95f), 0, count - 1)];
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UISoul] Could not extract gradient colors from liquid texture: {ex.Message}");
        }
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

        ExtractLiquidGradient(liquidTexture, out Color shadow, out Color body, out Color highlight);
        Texture2D baked = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        baked.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 256; y++)
        {
            float normY = y / 255f;
            Color baseColor = SampleLiquidColor(normY, shadow, body, highlight);
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
            colorsCached = false;
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
        if (generatedFluidTexture != null)
        {
            Destroy(generatedFluidTexture);
            generatedFluidTexture = null;
        }
    }
}
