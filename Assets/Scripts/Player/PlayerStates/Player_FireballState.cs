using UnityEngine;

public class Player_FireballState : PlayerState
{
    public override bool CanDashDuringState => false;

    public Player_FireballState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    public override void AnimationTrigger(string triggerName)
    {
        base.AnimationTrigger(triggerName);

        if (triggerName == "FireballCast")
        {
            player.TriggerFireballCast();

            float recoilSpeed = player.groundDetected ? 4f : 5.5f;
            rb.linearVelocity = new Vector2(-player.facingDir * recoilSpeed, 0f);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0f, Time.fixedDeltaTime * 12f), 0f);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (triggerCalled)
        {
            if (player.groundDetected)
            {
                if (player.moveInput.x != 0)
                    stateMachine.ChangeState(player.runState);
                else
                    stateMachine.ChangeState(player.idleState);
            }
            else
            {
                stateMachine.ChangeState(player.fallState);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();

        rb.gravityScale = player.defaultGravityScale;
    }
}
