using UnityEngine;

public class Player_ScreamState : PlayerState
{
    public override bool CanDashDuringState => false;

    public Player_ScreamState(Player player, StateMachine stateMachine, string animBoolName)
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
        if (triggerName == "ScreamCast")
        {
            player.TriggerScreamCast();
            if (CameraController.Instance != null)
            {
                CameraController.Instance.AddTrauma(0.45f);
            }
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        rb.linearVelocity = Vector2.zero;
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
