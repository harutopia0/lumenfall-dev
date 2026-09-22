using UnityEngine;

public class Player_WallSlideState : PlayerState
{
    public Player_WallSlideState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.canAirDash = true;
        anim.Play("playerWallSlide", 0, 0f);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        HandleWallSlide();

        if (input.Player.Jump.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.wallJumpState);
            return;
        }

        if (!player.wallDetected)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        if (player.groundDetected)
        {
            stateMachine.ChangeState(player.idleState);

            if (player.moveInput.x != 0 && player.facingDir != player.moveInput.x)
            {
                player.Flip();
            }
        }

        if (input.Player.SuperDash.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.superDashChargeState);
            return;
        }
    }

    private void HandleWallSlide()
    {
        if (player.moveInput.y < 0)
        {
            player.SetVelocity(player.moveInput.x, rb.linearVelocity.y);
        }
        else
        {
            player.SetVelocity(player.moveInput.x, rb.linearVelocity.y * player.wallSlideSlowMultiplier);
        }
    }
}
