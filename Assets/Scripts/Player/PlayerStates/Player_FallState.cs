using UnityEngine;

public class Player_FallState : Player_AiredState
{
    public Player_FallState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

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
}
