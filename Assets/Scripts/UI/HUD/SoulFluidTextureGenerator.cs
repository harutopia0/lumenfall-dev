using UnityEngine;

namespace Lumenfall.UI
{
    public class SoulFluidTextureGenerator
    {
        public const int FluidTexSize = 128;

        private Texture2D generatedFluidTexture;
        private Color[] pixelBuffer;
        private Color cachedShadow = new Color(0.1f, 0.15f, 0.3f);
        private Color cachedBody = new Color(0.15f, 0.25f, 0.5f);
        private Color cachedHighlight = Color.white;
        private bool colorsCached = false;

        public Texture2D GeneratedTexture => generatedFluidTexture;

        public void UpdateFluidColor(Material soulMaterial, Texture2D liquidTexture, float fillRatio, System.Func<float, float> remapFillFunc)
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

            UpdateFluidTexturePixels(fillRatio, remapFillFunc);
        }

        private void UpdateFluidTexturePixels(float fillRatio, System.Func<float, float> remapFillFunc)
        {
            if (generatedFluidTexture == null || pixelBuffer == null) return;

            float actualWaterLevel = Mathf.Clamp01(remapFillFunc != null ? remapFillFunc(fillRatio) : fillRatio);
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

        public static Color SampleLiquidColor(float v, Color shadow, Color body, Color highlight)
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

        public static void ExtractLiquidGradient(Texture2D source, out Color shadow, out Color body, out Color highlight)
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
                    Object.Destroy(temp);
                }
                else
                {
                    Object.DestroyImmediate(temp);
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
                Debug.LogWarning($"[SoulFluidTextureGenerator] Could not extract gradient: {ex.Message}");
            }
        }

        public void InvalidateCache()
        {
            colorsCached = false;
        }

        public void Cleanup()
        {
            if (generatedFluidTexture != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(generatedFluidTexture);
                }
                else
                {
                    Object.DestroyImmediate(generatedFluidTexture);
                }
                generatedFluidTexture = null;
            }
            pixelBuffer = null;
        }
    }
}
