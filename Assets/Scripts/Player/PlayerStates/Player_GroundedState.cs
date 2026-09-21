using UnityEngine;

public class Player_GroundedState : PlayerState
{
    public Player_GroundedState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.canAirDash = true;
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (rb.linearVelocity.y < 0 && !player.groundDetected)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        if(input.Player.Jump.WasPerformedThisFrame() && !player.ceilingDetected)
        {
            stateMachine.ChangeState(player.jumpState);
            return;
        }

        if (input.Player.Attack.WasPerformedThisFrame())
        {
            stateMachine.ChangeState(player.slashState);
        }

        if (input.Player.SuperDash.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.superDashChargeState);
            return;
        }
    }
}
