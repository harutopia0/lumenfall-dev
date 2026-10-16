using UnityEngine;

public class UIHealth : MonoBehaviour
{
    private int currentHealth;

    private void Start()
    {
        UIManager.updateHealth.AddListener(UpdateHealthUI);
    }

    private void OnDisable()
    {
        UIManager.updateHealth.RemoveListener(UpdateHealthUI);
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
