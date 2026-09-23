public class Player_JumpState : Player_AiredState
{
    private bool isJumpCut;

    public Player_JumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        isJumpCut = false;

        if (!input.Player.Jump.IsPressed())
        {
            isJumpCut = true;
            player.SetVelocity(rb.linearVelocity.x, player.jumpForce * player.jumpCutMultiplier);
        }
        else
        {
            player.SetVelocity(rb.linearVelocity.x, player.jumpForce);
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (!isJumpCut && !input.Player.Jump.IsPressed() && rb.linearVelocity.y > 0)
        {
            isJumpCut = true;
            player.SetVelocity(rb.linearVelocity.x, rb.linearVelocity.y * player.jumpCutMultiplier);
        }

        if (rb.linearVelocity.y < 0 && stateMachine.currentState != player.plungeAttackState)
        {
            stateMachine.ChangeState(player.fallState);
        }
    }
}
