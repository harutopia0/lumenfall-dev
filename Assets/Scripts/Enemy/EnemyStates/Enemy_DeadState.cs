using UnityEngine;

public class Enemy_DeadState : EnemyState
{
    private Collider2D enemyCollider;

    public Enemy_DeadState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
        enemyCollider = enemy.GetComponent<Collider2D>();
    }

    public override void Enter()
    {
        anim.enabled = false; // Disable animation when dead
        enemyCollider.enabled = false;

        rb.gravityScale = 12;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 15);

        stateMachine.SwitchOffStateMachine();
    }
}
