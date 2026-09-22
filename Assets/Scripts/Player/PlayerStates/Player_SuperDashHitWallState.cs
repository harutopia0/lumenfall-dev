public class Player_SuperDashHitWallState : PlayerState
{
    public Player_SuperDashHitWallState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        rb.gravityScale = 0;
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

    public override void Exit()
    {
        base.Exit();

        rb.gravityScale = player.defaultGravityScale;
    }
}
