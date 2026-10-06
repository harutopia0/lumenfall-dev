using UnityEngine;

public class Player_FocusEndState : Player_GroundedState
{
    // 3 frames @ 12 FPS: 3 / 12 = 0.25s
    private const float END_DURATION = 3f / 12f;
    private float timer;

    public Player_FocusEndState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = END_DURATION;
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

        timer -= Time.deltaTime;

        if (timer <= 0f || triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
