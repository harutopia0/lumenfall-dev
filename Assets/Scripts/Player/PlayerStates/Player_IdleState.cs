using UnityEngine;

public class Player_IdleState : Player_GroundedState
{
    public Player_IdleState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.SetVelocity(0, rb.linearVelocity.y);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (player.moveInput.x == player.facingDir && player.wallDetected)
        {
            return;
        }

        if (player.moveInput.x != 0)
        {
            stateMachine.ChangeState(player.runState);
        }
    }
}
