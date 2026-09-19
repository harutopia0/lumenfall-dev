using UnityEngine;

public class Player_DeadState : PlayerState
{
    public Player_DeadState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // Disable player input and movement
        input.Disable();

        // Disable player physics
        rb.simulated = false; // Disable physics simulation for the player
    }
}
