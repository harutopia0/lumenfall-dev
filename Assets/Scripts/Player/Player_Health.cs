using System;
using System.Collections;
using UnityEngine;

public class Player_Health : Entity_Health
{
    public static event Action<int, int> OnPlayerHealthChanged;

    [Header("Mask Details")]
    [SerializeField] private int maxMasks = 5;
    [SerializeField] private int currentMasks;
    [SerializeField] private int heavyDamageThreshold = 2;

    [Header("I-Frames (Invulnerability)")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    private bool isInvincible;
    private SpriteRenderer sr;

    protected override void Awake()
    {
        base.Awake();
        sr = GetComponentInChildren<SpriteRenderer>();
        currentMasks = maxMasks;
    }

    private void Start()
    {
        OnPlayerHealthChanged?.Invoke(currentMasks, maxMasks);
    }

    public override void TakeDamage(float damage, Transform damageDealer)
    {
        if (isDead || isInvincible) return;

        base.TakeDamage(damage, damageDealer);

        if (!isDead)
        {
            StartCoroutine(InvincibilityCo());
        }
    }

    protected override void ReduceHp(float damage)
    {
        int masksToLose = Mathf.Max(1, Mathf.RoundToInt(damage));
        currentMasks -= masksToLose;
        currentMasks = Mathf.Clamp(currentMasks, 0, maxMasks);

        OnPlayerHealthChanged?.Invoke(currentMasks, maxMasks);

        if (currentMasks <= 0)
        {
            Die();
        }
    }

    public void Heal(int maskAmount)
    {
        if (isDead || currentMasks >= maxMasks) return;

        currentMasks = Mathf.Min(currentMasks + maskAmount, maxMasks);
        OnPlayerHealthChanged?.Invoke(currentMasks, maxMasks);
    }

    private IEnumerator InvincibilityCo()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }


    protected override void Die()
    {
        StopAllCoroutines();
        if (sr != null)
        {
            sr.enabled = true;
        }

        base.Die();
    }

    protected override bool IsHeavyDamage(float damage) => damage >= heavyDamageThreshold;
}
