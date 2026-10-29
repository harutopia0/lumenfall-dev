using UnityEngine;

public class Enemy_Health : Entity_Health
{
    private Enemy enemy => GetComponent<Enemy>();

    [Header("Enemy HP")]
    [SerializeField] private float maxHp = 20f;
    [SerializeField] private float currentHp;
    [SerializeField] private float heavyDamageThreshold = 0.4f;

    protected override void Awake()
    {
        base.Awake();
        currentHp = maxHp;
    }

    public override void TakeDamage(float damage, Transform damageDealer)
    {
        base.TakeDamage(damage, damageDealer);

        if (isDead) return;

        if (damageDealer != null && damageDealer.GetComponent<Player>() != null)
        {
            enemy.TryToEnterBattleState(damageDealer);
        }
    }

    protected override void ReduceHp(float damage)
    {
        currentHp -= damage;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    protected override bool IsHeavyDamage(float damage) => (damage / maxHp) >= heavyDamageThreshold;
}
