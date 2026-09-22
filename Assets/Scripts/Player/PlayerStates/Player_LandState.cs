using UnityEngine;

public class Player_LandState : Player_GroundedState
{
    public Player_LandState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(0, 0);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (player.moveInput.x != 0)
        {
            stateMachine.ChangeState(player.runState);
            return;
        }

        if (triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }
}
