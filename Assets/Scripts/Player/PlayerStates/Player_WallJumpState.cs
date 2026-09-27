using UnityEngine;

public class Player_WallJumpState : PlayerState
{
    private int jumpDir;
    private bool isJumpCut;
    private float pushOffTimer;

    public Player_WallJumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.canAirDash = true;
        player.canDoubleJump = true;

        jumpDir = -player.facingDir;
        pushOffTimer = player.wallJumpPushOffDuration;
        isJumpCut = false;

        player.SetVelocity(jumpDir * player.wallJumpForce.x, player.wallJumpForce.y);

        player.vfx?.PlayWallJumpPuffVfx(player.transform.position, jumpDir);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        pushOffTimer -= Time.fixedDeltaTime;

        if (!isJumpCut && !input.Player.Jump.IsPressed() && rb.linearVelocity.y > 0)
        {
            isJumpCut = true;
            player.SetVelocity(rb.linearVelocity.x, rb.linearVelocity.y * player.jumpCutMultiplier);
        }

        if (pushOffTimer > 0)
        {
            player.SetVelocity(jumpDir * player.wallJumpForce.x, rb.linearVelocity.y);
        }
        else
        {
            if (player.moveInput.x != 0)
            {
                player.SetVelocity(player.moveInput.x * player.moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                player.SetVelocity(0, rb.linearVelocity.y);
            }
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (input.Player.Attack.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.slashState);
            return;
        }

        if (pushOffTimer <= 0 && player.wallDetected && rb.linearVelocity.y < 0)
        {
            stateMachine.ChangeState(player.wallSlideState);
            return;
        }

        if (rb.linearVelocity.y < 0)
        {
            stateMachine.ChangeState(player.fallState);
        }
    }
}
