using UnityEngine;

public class Player_JumpState : Player_AiredState
{
    public Player_JumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.SetVelocity(rb.linearVelocity.x, player.jumpForce);
    }

    public override void Update()
    {
        base.Update();

        // We need to be sure we are not in the plunge attack state before changing to fall state, otherwise we'll get stuck in the plunge attack state.
        if (rb.linearVelocity.y < 0 && stateMachine.currentState != player.plungeAttackState)
        {
            stateMachine.ChangeState(player.fallState);
        }
    }
}
