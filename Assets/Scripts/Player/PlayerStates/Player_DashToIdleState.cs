using UnityEngine;

public class Player_DashToIdleState : Player_GroundedState
{
    public Player_DashToIdleState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
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

        player.SetVelocity(0, 0);

        if (player.moveInput.x != 0)
        {
            stateMachine.ChangeState(player.moveState);
            return;
        }

        if (!player.groundDetected)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        if (triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
