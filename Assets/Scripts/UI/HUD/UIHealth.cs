using UnityEngine;
using Lumenfall.Core;

public class UIHealth : MonoBehaviour
{
    private int currentHealth;

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
    }

    private void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        UpdateHealthUI(evt.CurrentHealth, evt.IsIncrease);
    }

    private void UpdateHealthUI(int _currentHealth, bool _increase)
    {
        currentHealth = _currentHealth;
        int childCount = transform.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Mask mask = transform.GetChild(i).GetComponent<Mask>();
            if (mask == null) continue;

            if (i >= _currentHealth)
            {
                if (mask.idleMask != null && mask.idleMask.enabled)
                {
                    mask.ChangeMask(false, currentHealth);
                }
            }
            else
            {
                if (mask.idleMask != null && !mask.idleMask.enabled)
                {
                    mask.ChangeMask(true, currentHealth);
                }
            }

            if (i == 0)
            {
                if (currentHealth == 1)
                {
                    mask.ShakeMask();
                }
                else
                {
                    mask.StopShaking();
                }
            }
        }
    }
}
