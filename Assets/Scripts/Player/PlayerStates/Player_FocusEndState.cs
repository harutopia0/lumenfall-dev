using UnityEngine;

public class Player_FocusEndState : Player_GroundedState
{
    private float endDuration = 0.2f;
    private float endTimer;

    public Player_FocusEndState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        endTimer = endDuration;
        player.SetVelocity(0f, 0f);
        player.vfx?.SetFocusCharging(false);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        player.SetVelocity(0f, 0f);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        endTimer -= Time.deltaTime;

        if (endTimer <= 0f || triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
