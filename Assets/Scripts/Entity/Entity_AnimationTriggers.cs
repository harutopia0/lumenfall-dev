using UnityEngine;

public class Entity_AnimationTriggers : MonoBehaviour
{
    private Entity entity;
    private Entity_Combat entityCombat;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
        entityCombat = GetComponentInParent<Entity_Combat>();
    }

    private void CurrentStateTrigger()
    {
        entity.CurrentStateAnimationTrigger();
    }

    private void CustomAnimationTrigger(string triggerName)
    {
        entity.CustomAnimationTrigger(triggerName);
    }

    private void AttackTrigger()
    {
        entityCombat.PerformAttack();
    }
}
