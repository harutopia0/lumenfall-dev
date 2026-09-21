using UnityEngine;

public class Player_SuperDashHitWallState : PlayerState
{
    public Player_SuperDashHitWallState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.SetVelocity(0, 0);

        player.vfx?.PlaySuperDashBreakVfx();
    }

    public override void Update()
    {
        base.Update();

        player.SetVelocity(0, 0);

        if (triggerCalled)
        {
            stateMachine.ChangeState(player.wallSlideState);
        }
    }
}
