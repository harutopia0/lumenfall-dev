using UnityEngine;

public class Player_SuperDashState : PlayerState
{
    private int launchDir;

    public override bool CanDashDuringState => false;

    public Player_SuperDashState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        rb.gravityScale = 0;
        player.canAirDash = true;

        if (player.wallDetected)
        {
            player.Flip();
        }

        launchDir = player.facingDir;

        player.vfx?.PlaySuperDashBurstVfx(player.transform.position, player.transform.rotation, launchDir);
        player.vfx?.SetSuperDashTrail(true);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        player.SetVelocity(launchDir * player.superDashSpeed, 0);
    }

    public override void Update()
    {
        base.Update();

        if (input.Player.Jump.WasPressedThisFrame() || input.Player.Dash.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.superDashAirBrakeState);
            return;
        }

        if (player.wallDetected)
        {
            stateMachine.ChangeState(player.superDashHitWallState);
            return;
        }
    }

    public override void Exit()
    {
        base.Exit();

        player.vfx?.SetSuperDashTrail(false);
        rb.gravityScale = player.defaultGravityScale;
    }
}
