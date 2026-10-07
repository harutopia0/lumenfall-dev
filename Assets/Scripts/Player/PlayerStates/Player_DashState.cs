using UnityEngine;

public class Player_DashState : PlayerState
{
    private float originalGravityScale;
    private float dashTimer;
    private int dashDir;

    public override bool CanDashDuringState => false;

    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.lastDashTime = Time.time;
        if (!player.groundDetected)
        {
            player.canAirDash = false;
        }

        dashDir = player.moveInput.x != 0 ? (int)Mathf.Sign(player.moveInput.x) : player.facingDir;
        dashTimer = player.dashDuration;

        originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0;

        player.SetVelocity(player.dashSpeed * dashDir, 0);

        player.vfx?.PlayDashVfx(player.transform.position, player.transform.rotation, player.facingDir);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        dashTimer -= Time.fixedDeltaTime;

        player.SetVelocity(player.dashSpeed * dashDir, 0);
    }

    public override void Update()
    {
        base.Update();

        CancelDashIfNeeded();

        if (dashTimer < 0)
        {
            if (player.groundDetected)
            {
                if (player.moveInput.x != 0)
                    stateMachine.ChangeState(player.runState);
                else
                    stateMachine.ChangeState(player.dashToIdleState);
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

        player.SetVelocity(0, 0);

        rb.gravityScale = originalGravityScale;
    }

    private void CancelDashIfNeeded()
    {
        if (player.wallDetected)
        {
            if (player.groundDetected)
            {
                stateMachine.ChangeState(player.idleState);
            }
            else
            {
                stateMachine.ChangeState(player.wallSlideState);
            }
        }
    }
}
