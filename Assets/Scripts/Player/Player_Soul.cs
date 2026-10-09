using System;
using UnityEngine;

public class Player_Soul : MonoBehaviour
{
    public static event Action<int, int> OnPlayerSoulChanged;

    [Header("Soul Settings")]
    [SerializeField] private int maxSoul = 99;
    [SerializeField] private int currentSoul = 0;

    public int CurrentSoul => currentSoul;
    public int MaxSoul => maxSoul;

    private void Start()
    {
        OnPlayerSoulChanged?.Invoke(currentSoul, maxSoul);
    }

    public bool HasEnoughSoul(int amount) => currentSoul >= amount;

    public bool ConsumeSoul(int amount)
    {
        if (currentSoul < amount) return false;

        currentSoul -= amount;
        OnPlayerSoulChanged?.Invoke(currentSoul, maxSoul);
        return true;
    }

    public void GainSoul(int amount)
    {
        currentSoul = Mathf.Clamp(currentSoul + amount, 0, maxSoul);
        OnPlayerSoulChanged?.Invoke(currentSoul, maxSoul);
    }
}
