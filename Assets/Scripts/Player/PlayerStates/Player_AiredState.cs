using UnityEngine;

public class Player_AiredState : PlayerState
{
    public Player_AiredState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (player.moveInput.x != 0)
        {
            player.SetVelocity(player.moveInput.x * player.moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            player.SetVelocity(0, rb.linearVelocity.y);
        }

        if (input.Player.Attack.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.slashState);
        }
    }
}
