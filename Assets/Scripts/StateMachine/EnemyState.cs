using UnityEngine;

public class EnemyState : EntityState
{
    protected Enemy enemy;

    public EnemyState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.enemy = enemy;

        this.rb = enemy.rb;
        this.anim = enemy.anim;
    }

    public override void Update()
    {
        base.Update();

        anim.SetFloat("moveAnimSpeedMultiplier", enemy.moveAnimSpeedMultiplier);
    }
}
