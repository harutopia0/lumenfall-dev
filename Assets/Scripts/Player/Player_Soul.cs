using System;
using UnityEngine;
using Lumenfall.Core;

public class Player_Soul : MonoBehaviour
{
    [Header("Soul Settings")]
    [SerializeField] private int maxSoul = 99;
    [SerializeField] private int currentSoul = 0;

    public int CurrentSoul => currentSoul;
    public int MaxSoul => maxSoul;

    private void OnEnable()
    {
        EventBus.Publish(new PlayerSoulChangedEvent(currentSoul, maxSoul, true));
    }

    private void Start()
    {
        EventBus.Publish(new PlayerSoulChangedEvent(currentSoul, maxSoul, true));
    }

    public bool HasEnoughSoul(int amount) => currentSoul >= amount;

    public bool ConsumeSoul(int amount)
    {
        if (currentSoul < amount) return false;

        currentSoul -= amount;
        EventBus.Publish(new PlayerSoulChangedEvent(currentSoul, maxSoul, false));
        return true;
    }

    public void GainSoul(int amount)
    {
        currentSoul = Mathf.Clamp(currentSoul + amount, 0, maxSoul);
        EventBus.Publish(new PlayerSoulChangedEvent(currentSoul, maxSoul, true));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);
            EventBus.Publish(new PlayerSoulChangedEvent(currentSoul, maxSoul, true));
        }
    }
#endif
}
