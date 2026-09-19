using System;
using UnityEngine;

public abstract class Entity_Health : MonoBehaviour, IDamageable
{
    private Entity_VFX entityVFX;
    private Entity entity;

    [SerializeField] protected bool isDead;

    [Header("Knockback Settings")]
    [SerializeField] private Vector2 knockbackPower = new Vector2(2.5f, 2f);
    [SerializeField] private Vector2 heavyKnockbackPower = new Vector2(7.5f, 5f);
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float heavyKnockbackDuration = 0.5f;

    protected virtual void Awake()
    {
        entityVFX = GetComponent<Entity_VFX>();
        entity = GetComponent<Entity>();
    }

    public virtual void TakeDamage(float damage, Transform damageDealer)
    {
        if(isDead) return;

        Vector2 knockback = CalculateKnockback(damage, damageDealer);
        float duration = CalculateKnockbackDuration(damage);

        entity?.ReceiveKnockback(knockback, duration);
        entityVFX?.PlayOnDamageVfx();
        ReduceHp(damage);
    }

    protected abstract void ReduceHp(float damage);

    protected abstract bool IsHeavyDamage(float damage);

    protected virtual void Die()
    {
        isDead = true;
        entity?.EntityDeath();
    }

    protected virtual Vector2 CalculateKnockback(float damage, Transform damageDealer)
    {
        if (damageDealer == null) return knockbackPower;
        int direction = damageDealer.position.x > transform.position.x ? -1 : 1;
        Vector2 knockback = IsHeavyDamage(damage) ? heavyKnockbackPower : knockbackPower;
        knockback.x *= direction;
        return knockback;
    }

    protected virtual float CalculateKnockbackDuration(float damage) => IsHeavyDamage(damage) ? heavyKnockbackDuration : knockbackDuration;
}
