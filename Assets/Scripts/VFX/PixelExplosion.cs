using UnityEngine;

public class PixelExplosion : MonoBehaviour
{
    [SerializeField] private ParticleSystem pixelParticlePrefab;

    public void Explode(SpriteRenderer sr, Vector2 extraVelocity)
    {
        if (sr == null || sr.sprite == null || pixelParticlePrefab == null) return;

        Sprite sprite = sr.sprite;
        Texture2D texture = sprite.texture;
        Rect rect = sprite.rect;
        float ppu = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;

        Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

        int validCount = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > 0.1f) validCount++;
        }

        if (validCount == 0) return;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[validCount];
        int index = 0;
        int width = (int)rect.width;
        int height = (int)rect.height;

        Vector3 worldPos = sr.transform.position;
        Vector3 lossyScale = sr.transform.lossyScale;
        bool flipX = sr.flipX;
        bool flipY = sr.flipY;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = pixels[y * width + x];
                if (color.a <= 0.1f) continue;

                float localX = (x - pivot.x) / ppu;
                float localY = (y - pivot.y) / ppu;

                if (flipX) localX = -localX;
                if (flipY) localY = -localY;

                Vector3 particlePos = worldPos + new Vector3(localX * lossyScale.x, localY * lossyScale.y, 0f);

                Vector2 dirFromCenter = (particlePos - worldPos).normalized;
                if (dirFromCenter == Vector2.zero)
                {
                    dirFromCenter = Random.insideUnitCircle.normalized;
                }

                Vector2 radialVelocity = (dirFromCenter + new Vector2(0f, 0.35f)).normalized * Random.Range(4f, 7.5f);

                particles[index].position = particlePos;
                particles[index].startColor = color;
                particles[index].startSize = 1.5f / ppu;
                particles[index].velocity = radialVelocity;
                particles[index].startLifetime = Random.Range(2f, 3f);
                particles[index].remainingLifetime = particles[index].startLifetime;

                index++;
            }
        }

        ParticleSystem ps = Instantiate(pixelParticlePrefab, Vector3.zero, Quaternion.identity);
        ps.SetParticles(particles, validCount);
        Destroy(ps.gameObject, 3.5f);
    }
}
