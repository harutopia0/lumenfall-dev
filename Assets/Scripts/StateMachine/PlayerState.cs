using UnityEngine;

public abstract class PlayerState : EntityState
{
    protected Player player;
    protected PlayerInputSet input;

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
        if (player.wallDetected || stateMachine.currentState == player.dashState || stateMachine.currentState == player.deadState)
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
