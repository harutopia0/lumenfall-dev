using UnityEngine;

public class Player_SuperDashChargeState : PlayerState
{
    private float chargeTimer;
    private bool isFromWall;
    private bool playedBling;

    public Player_SuperDashChargeState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        chargeTimer = player.superDashChargeTime;
        isFromWall = player.wallDetected;
        playedBling = false;

        rb.gravityScale = 0;
        player.SetVelocity(0, 0);

        anim.SetBool("isWallCharge", isFromWall);
        player.vfx?.SetSuperDashCharging(true, isFromWall);
    }

    public override void Update()
    {
        base.Update();

        player.SetVelocity(0, 0);

        if (input.Player.SuperDash.WasReleasedThisFrame())
        {
            rb.gravityScale = player.defaultGravityScale;

            if (isFromWall && player.wallDetected)
            {
                stateMachine.ChangeState(player.wallSlideState);
            }
            else
            {
                stateMachine.ChangeState(player.superDashChargeCancelState);
            }
            return;
        }

        chargeTimer -= Time.deltaTime;

        if (chargeTimer <= 0 && !playedBling)
        {
            playedBling = true;
            player.vfx?.PlaySuperDashBlingVfx();
        }

        if (chargeTimer <= 0)
        {
            stateMachine.ChangeState(player.superDashState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        anim.SetBool("isWallCharge", false);
        player.vfx?.SetSuperDashCharging(false);
    }
}
