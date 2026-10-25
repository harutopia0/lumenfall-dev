using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StellaSora.UI
{
    /// <summary>
    /// Hieu ung hat bui sao (Stardust Particles) tang 2D UI cua Stella Sora.
    /// Kep giua may phat sang (Layer 1) va tuong thanh (Layer 3).
    /// Tai hien chuan xac 1:1 chuyen dong cua hat tu source game & ban mau HTML.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class StardustUIParticles : MaskableGraphic
    {
        [Header("Particle Settings")]
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private int maxParticles = 18;
        [SerializeField] private Vector2 emitterCenter = new Vector2(84f, -368.8f);
        [SerializeField] private Vector2 emitterSize = new Vector2(1200f, 450f);
        [SerializeField] private Vector2 minSpeed = new Vector2(-70f, 30f);
        [SerializeField] private Vector2 maxSpeed = new Vector2(-35f, 80f);
        [SerializeField] private Vector2 sizeRange = new Vector2(18f, 32f);
        [SerializeField] private Vector2 lifetimeRange = new Vector2(7f, 11f);

        public override Texture mainTexture => particleSprite != null ? particleSprite.texture : s_WhiteTexture;

        public Sprite ParticleSprite
        {
            get => particleSprite;
            set
            {
                if (particleSprite != value)
                {
                    particleSprite = value;
                    SetMaterialDirty();
                    SetVerticesDirty();
                }
            }
        }

        private struct Particle
        {
            public Vector2 pos;
            public Vector2 vel;
            public float size;
            public float age;
            public float lifetime;
            public float seed;
        }

        private readonly List<Particle> particles = new List<Particle>();

        protected override void Awake()
        {
            base.Awake();
            EnsureSpriteLoaded();
            InitParticles();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureSpriteLoaded();
            if (particles.Count == 0)
            {
                InitParticles();
            }
            SetVerticesDirty();
        }

        private void EnsureSpriteLoaded()
        {
#if UNITY_EDITOR
            if (particleSprite == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("particle_stardust t:Sprite");
                if (guids != null && guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sp != null)
                    {
                        particleSprite = sp;
                    }
                }
            }
#endif
        }

        private void InitParticles()
        {
            particles.Clear();
            for (int i = 0; i < maxParticles; i++)
            {
                particles.Add(CreateParticle(true));
            }
        }

        private Particle CreateParticle(bool prewarm)
        {
            float lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
            float age = prewarm ? Random.Range(0f, lifetime) : 0f;
            float size = Random.Range(sizeRange.x, sizeRange.y);

            Vector2 vel = new Vector2(
                Random.Range(minSpeed.x, maxSpeed.x),
                Random.Range(minSpeed.y, maxSpeed.y)
            );

            Vector2 pos = new Vector2(
                emitterCenter.x + (Random.value - 0.5f) * emitterSize.x,
                emitterCenter.y + (Random.value - 0.5f) * emitterSize.y
            );

            float seed = Random.Range(0f, 100f);

            Particle p = new Particle
            {
                pos = pos,
                vel = vel,
                size = size,
                age = age,
                lifetime = lifetime,
                seed = seed
            };

            if (prewarm && age > 0f)
            {
                int steps = 14;
                float subDt = age / steps;
                for (int s = 0; s < steps; s++)
                {
                    float tSec = s * subDt;
                    float nx = Mathf.PerlinNoise(p.pos.x * 0.0012f, (tSec + p.seed) * 0.1f) * 2f - 1f;
                    float ny = Mathf.PerlinNoise((p.pos.x + 43.1f) * 0.0012f, (tSec + p.seed) * 0.1f) * 2f - 1f;
                    p.pos.x += (p.vel.x + nx * 28f) * subDt;
                    p.pos.y += (p.vel.y + ny * 28f) * subDt;
                }
            }

            return p;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            if (dt <= 0f && Application.isPlaying) return;
            if (!Application.isPlaying) dt = 0.016f;

            float tNow = Time.time;

            if (particles.Count == 0)
            {
                InitParticles();
            }

            for (int i = 0; i < particles.Count; i++)
            {
                Particle p = particles[i];
                p.age += dt;

                if (p.age >= p.lifetime || p.pos.x < -1300f || p.pos.y > 950f || p.pos.x > 1300f)
                {
                    particles[i] = CreateParticle(false);
                    continue;
                }

                float nx = Mathf.PerlinNoise(p.pos.x * 0.0012f, (tNow + p.seed) * 0.1f) * 2f - 1f;
                float ny = Mathf.PerlinNoise((p.pos.x + 43.1f) * 0.0012f, (tNow + p.seed) * 0.1f) * 2f - 1f;

                p.pos.x += (p.vel.x + nx * 28f) * dt;
                p.pos.y += (p.vel.y + ny * 28f) * dt;

                particles[i] = p;
            }

            SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            EnsureSpriteLoaded();
            if (particles.Count == 0)
            {
                InitParticles();
            }
            SetVerticesDirty();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            }
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Vector4 uv = particleSprite != null 
                ? UnityEngine.Sprites.DataUtility.GetOuterUV(particleSprite) 
                : new Vector4(0, 0, 1, 1);

            for (int i = 0; i < particles.Count; i++)
            {
                Particle p = particles[i];
                float progress = p.age / p.lifetime;
                float alpha = 0f;

                if (progress < 0.15f)
                {
                    alpha = progress / 0.15f;
                }
                else if (progress <= 0.72f)
                {
                    alpha = 1f;
                }
                else
                {
                    alpha = (1f - progress) / (1f - 0.72f);
                }

                // Chuyen doi chuan xac tu Color (float 0..1) sang Color32 (byte 0..255)
                Color c = color;
                c.a = color.a * Mathf.Clamp01(alpha * 0.95f);
                Color32 vertColor = c;

                float halfSize = p.size * 0.5f;
                float xMin = p.pos.x - halfSize;
                float xMax = p.pos.x + halfSize;
                float yMin = p.pos.y - halfSize;
                float yMax = p.pos.y + halfSize;

                int startIndex = vh.currentVertCount;

                vh.AddVert(new Vector3(xMin, yMin, 0f), vertColor, new Vector2(uv.x, uv.y));
                vh.AddVert(new Vector3(xMin, yMax, 0f), vertColor, new Vector2(uv.x, uv.w));
                vh.AddVert(new Vector3(xMax, yMax, 0f), vertColor, new Vector2(uv.z, uv.w));
                vh.AddVert(new Vector3(xMax, yMin, 0f), vertColor, new Vector2(uv.z, uv.y));

                vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
            }
        }
    }
}
