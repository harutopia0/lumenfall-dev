using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlPanel : MonoBehaviour
{
   [Range(0,1)] [SerializeField] float maxSoul;
    [SerializeField] float currentSoul;
   [Range(0,1)] [SerializeField] float soulModifier;
    [SerializeField] int maxHealth;
    [SerializeField] int currentHealth;
    [SerializeField] int healthModifier;
    [SerializeField] Transform masksGroup;
    [SerializeField] float waveLenghtMultiplier;
    [SerializeField] float waveFlattenMultiplier;


    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape)) { Application.Quit(); }
    }
    private void Start()
    {
        maxHealth = masksGroup.childCount;
        currentHealth = masksGroup.childCount;
    }
    public void ReduceHealth()
    {
        
        if (currentHealth > 1)
        {
            currentHealth -= healthModifier;
            UIManager.updateHealth.Invoke(currentHealth,false);
            
        }
       
    }
    public void IncreaseHealth()
    {
        if (currentHealth<maxHealth) 
        {
            
            currentHealth += healthModifier;
            UIManager.updateHealth.Invoke(currentHealth, true);
            
        }
        
    }

    public void IncreaseSoul()
    {
        if (currentSoul < maxSoul)
        {
            currentSoul += soulModifier;
            if (currentSoul > maxSoul)
            {
                currentSoul = maxSoul;
            }
            UIManager.updateSoul.Invoke(currentSoul, maxSoul, soulModifier);
            UIManager.updateSoulAnimation.Invoke(waveFlattenMultiplier);
        }
    }

    public void DecreaseSoul()
    {
        if(currentSoul > 0)
        {
            currentSoul -= soulModifier;
            
            UIManager.updateSoul.Invoke(currentSoul,maxSoul, soulModifier);
            UIManager.updateSoulAnimation.Invoke(waveLenghtMultiplier);
        }
    }
}
