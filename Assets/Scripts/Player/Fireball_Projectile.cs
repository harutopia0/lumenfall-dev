using System.Collections.Generic;
using UnityEngine;

public class Fireball_Projectile : MonoBehaviour
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

    private Dictionary<Collider2D, float> hitCooldownDict = new Dictionary<Collider2D, float>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
                    Instantiate(hitImpactPrefab, collision.bounds.center, Quaternion.identity);
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
            Instantiate(vfxPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
