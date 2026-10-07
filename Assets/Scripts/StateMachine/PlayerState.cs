using UnityEngine;

public abstract class PlayerState : EntityState
{
    protected Player player;
    protected PlayerInputSet input;

    public virtual bool CanDashDuringState => true;

    public PlayerState(Player player, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.player = player;

        this.anim = player.anim;
        this.rb = player.rb;
        this.input = player.input;
    }

    public override void Update()
    {
        base.Update();

        if (input.Player.Dash.WasPressedThisFrame() && CanDash())
        {
            stateMachine.ChangeState(player.dashState);
        }
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();

        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private bool CanDash()
    {
        if (stateMachine.currentState is PlayerState playerState && !playerState.CanDashDuringState)
        {
            return false;
        }

        int intendedDashDir = player.moveInput.x != 0 ? (int)Mathf.Sign(player.moveInput.x) : player.facingDir;
        if (player.wallDetected && intendedDashDir == player.facingDir)
        {
            return false;
        }

        if (Time.time < player.lastDashTime + player.dashCooldown)
        {
            return false;
        }

        if (!player.groundDetected && !player.canAirDash)
        {
            return false;
        }

        return true;
    }
}
