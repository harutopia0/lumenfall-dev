using UnityEngine;

public class Player_FocusGetOnceState : Player_GroundedState
{
    // 5 frames @ 12 FPS: 5 / 12 = ~0.417s (burst & swallow animation)
    private const float BURST_DURATION = 5f / 12f;
    private float timer;

    public Player_FocusGetOnceState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = BURST_DURATION;
        player.SetVelocity(0f, 0f);
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

        if (timer <= 0f)
        {
            if (input.Player.FocusCast.IsPressed() && player.health.CanHeal())
            {
                stateMachine.ChangeState(player.focusGetState);
            }
            else
            {
                stateMachine.ChangeState(player.idleState);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
