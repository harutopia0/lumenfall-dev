using UnityEngine;

namespace Lumenfall.Core
{
    public readonly struct PlayerHealthChangedEvent
    {
        public readonly int CurrentHealth;
        public readonly int MaxHealth;
        public readonly bool IsIncrease;

        public PlayerHealthChangedEvent(int currentHealth, int maxHealth, bool isIncrease)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            IsIncrease = isIncrease;
        }
    }

    public readonly struct PlayerSoulChangedEvent
    {
        public readonly int CurrentSoul;
        public readonly int MaxSoul;
        public readonly bool IsIncrease;

        public float NormalizedCurrent => MaxSoul > 0 ? Mathf.Clamp01((float)CurrentSoul / MaxSoul) : 0f;
        public float NormalizedMax => 1f;
        public float SoulModifier => MaxSoul > 0 ? 33f / MaxSoul : 0.3333f;
        public bool HasCharge => CurrentSoul >= 33;
        public bool IsFullSoul => MaxSoul > 0 && CurrentSoul >= MaxSoul;
        public float WaveAffector => IsIncrease ? 1.2f : 0.4f;

        public PlayerSoulChangedEvent(int currentSoul, int maxSoul, bool isIncrease)
        {
            CurrentSoul = currentSoul;
            MaxSoul = maxSoul;
            IsIncrease = isIncrease;
        }
    }

    public readonly struct PlayerDeathEvent
    {
        public readonly Vector3 Position;

        public PlayerDeathEvent(Vector3 position)
        {
            Position = position;
        }
    }

    public readonly struct EntityDamagedEvent
    {
        public readonly GameObject Target;
        public readonly float Damage;
        public readonly Transform DamageDealer;

        public EntityDamagedEvent(GameObject target, float damage, Transform damageDealer)
        {
            Target = target;
            Damage = damage;
            DamageDealer = damageDealer;
        }
    }

    public readonly struct SceneTransitionEvent
    {
        public readonly string SceneName;
        public readonly string SpawnPointName;

        public SceneTransitionEvent(string sceneName, string spawnPointName)
        {
            SceneName = sceneName;
            SpawnPointName = spawnPointName;
        }
    }
}
