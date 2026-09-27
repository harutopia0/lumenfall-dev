public class Player_FallState : Player_AiredState
{
    public Player_FallState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        UpdateGravity();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        UpdateGravity();
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (player.jumpBufferTimer > 0 && player.coyoteTimer > 0)
        {
            player.jumpBufferTimer = 0;
            player.coyoteTimer = 0;
            stateMachine.ChangeState(player.jumpState);
            return;
        }

        if (input.Player.Jump.WasPressedThisFrame() && player.canDoubleJump)
        {
            player.jumpBufferTimer = 0;
            stateMachine.ChangeState(player.doubleJumpState);
            return;
        }

        if (player.groundDetected)
        {
            if (player.moveInput.x != 0)
            {
                stateMachine.ChangeState(player.runState);
            }
            else
            {
                stateMachine.ChangeState(player.landState);
            }
            return;
        }

        if (player.wallDetected)
        {
            stateMachine.ChangeState(player.wallSlideState);
        }
    }

    private void UpdateGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = player.defaultGravityScale * player.fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = player.defaultGravityScale;
        }
    }

    public override void Exit()
    {
        base.Exit();

        rb.gravityScale = player.defaultGravityScale;
    }
}
