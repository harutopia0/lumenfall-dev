public class Player_SuperDashAirBrakeState : PlayerState
{
    public Player_SuperDashAirBrakeState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.SetVelocity(player.facingDir * (player.moveSpeed * 0.4f), rb.linearVelocity.y);
        player.vfx?.PlaySuperDashTrailEndVfx(player.transform.position, player.transform.rotation, player.facingDir);
    }

    public override void Update()
    {
        base.Update();

        if (triggerCalled)
        {
            stateMachine.ChangeState(player.fallState);
        }
    }
}
