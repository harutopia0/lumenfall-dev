using System.Collections.Generic;
using UnityEngine;

public class HowlingWraiths_Burst : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damagePerHit = 2.5f;
    [SerializeField] private int maxHitsPerTarget = 3;
    [SerializeField] private float hitInterval = 0.12f;
    [SerializeField] private LayerMask whatIsTarget;

    [Header("Feedback VFX")]
    [SerializeField] private GameObject hitImpactPrefab;

    private class TargetHitRecord
    {
        public int hitsCount;
        public float lastHitTime;
    }

    private readonly Dictionary<Collider2D, TargetHitRecord> hitRecords = new Dictionary<Collider2D, TargetHitRecord>();

    private void OnTriggerEnter2D(Collider2D collision) => ProcessHit(collision);
    private void OnTriggerStay2D(Collider2D collision) => ProcessHit(collision);

    private void ProcessHit(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & whatIsTarget) == 0) return;

        float currentTime = Time.time;

        if (!hitRecords.TryGetValue(collision, out TargetHitRecord record))
        {
            record = new TargetHitRecord { hitsCount = 0, lastHitTime = 0f };
            hitRecords[collision] = record;
        }

        if (record.hitsCount >= maxHitsPerTarget) return;

        if (currentTime >= record.lastHitTime + hitInterval)
        {
            record.hitsCount++;
            record.lastHitTime = currentTime;

            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damagePerHit, transform);

                if (hitImpactPrefab != null)
                {
                    Instantiate(hitImpactPrefab, collision.bounds.center, Quaternion.identity);
                }
            }
        }
    }
}
