using System.Collections.Generic;
using UnityEngine;
using Lumenfall.Core;

[RequireComponent(typeof(PooledObject))]
public class Fireball_Projectile : MonoBehaviour, IPoolable
{
    [Header("Movement")]
    [SerializeField] private float speed = 32f;
    [SerializeField] private float lifetime = 1.25f;

    [Header("Damage & Collision")]
    [SerializeField] private float damage = 3f;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask whatIsTarget;
    [SerializeField] private float perTargetHitCooldown = 0.2f;

    [Header("Impact VFX Prefabs")]
    [SerializeField] private GameObject hitImpactPrefab;
    [SerializeField] private GameObject wallImpactPrefab;
    [SerializeField] private GameObject expireImpactPrefab;

    private Rigidbody2D rb;
    private int direction = 1;
    private float lifetimeTimer;
    private bool isDestroyed;
    private PooledObject pooledObject;

    private Dictionary<Collider2D, float> hitCooldownDict = new Dictionary<Collider2D, float>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pooledObject = GetComponent<PooledObject>();
    }

    public void Initialize(int shootDirection)
    {
        direction = shootDirection;
        transform.localScale = new Vector3(direction, 1f, 1f);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * speed, 0f);
        }

        lifetimeTimer = lifetime;
        isDestroyed = false;
        hitCooldownDict.Clear();
    }

    private void Update()
    {
        if (isDestroyed) return;

        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            Despawn(expireImpactPrefab);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) => HandleCollision(collision);
    private void OnTriggerStay2D(Collider2D collision) => HandleCollision(collision);

    private void HandleCollision(Collider2D collision)
    {
        if (isDestroyed) return;

        if (((1 << collision.gameObject.layer) & whatIsGround) != 0)
        {
            Despawn(wallImpactPrefab);
            return;
        }

        if (((1 << collision.gameObject.layer) & whatIsTarget) != 0)
        {
            if (hitCooldownDict.TryGetValue(collision, out float lastHitTime))
            {
                if (Time.time < lastHitTime + perTargetHitCooldown) return;
            }

            hitCooldownDict[collision] = Time.time;

            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform);

                if (hitImpactPrefab != null)
                {
                    SpawnImpactVfx(hitImpactPrefab, collision.bounds.center);
                }
            }
        }
    }

    private void Despawn(GameObject vfxPrefab)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (vfxPrefab != null)
        {
            SpawnImpactVfx(vfxPrefab, transform.position);
        }

        if (pooledObject == null)
        {
            pooledObject = GetComponent<PooledObject>();
        }

        if (pooledObject != null && PoolManager.HasInstance)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SpawnImpactVfx(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        if (PoolManager.HasInstance)
        {
            PoolManager.Instance.Spawn(prefab, position, Quaternion.identity);
        }
        else
        {
            Instantiate(prefab, position, Quaternion.identity);
        }
    }

    public void OnSpawnFromPool()
    {
        isDestroyed = false;
        hitCooldownDict.Clear();
    }

    public void OnReturnToPool()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
