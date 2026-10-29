using System;
using System.Collections;
using UnityEngine;
using Lumenfall.Core;

public class Player_Health : Entity_Health, IHealable
{
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
        currentMasks = 5;
    }

    private void OnEnable()
    {
        EventBus.Publish(new PlayerHealthChangedEvent(currentMasks, maxMasks, true));
    }

    private void Start()
    {
        EventBus.Publish(new PlayerHealthChangedEvent(currentMasks, maxMasks, true));
    }

    public int CurrentMasks => currentMasks;
    public int MaxMasks => maxMasks;
    public bool CanHeal() => currentMasks < maxMasks && !isDead;

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
        if (damage <= 0) return;
        int masksToLose = Mathf.Max(1, Mathf.RoundToInt(damage));
        currentMasks -= masksToLose;
        currentMasks = Mathf.Clamp(currentMasks, 0, maxMasks);

        EventBus.Publish(new PlayerHealthChangedEvent(currentMasks, maxMasks, false));

        if (currentMasks <= 0)
        {
            Die();
        }
    }

    public void Heal(int maskAmount)
    {
        if (isDead || currentMasks >= maxMasks) return;

        currentMasks = Mathf.Min(currentMasks + maskAmount, maxMasks);
        EventBus.Publish(new PlayerHealthChangedEvent(currentMasks, maxMasks, true));
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
