using UnityEngine;

public class Player_FocusGetOnceState : Player_GroundedState
{
    public override bool CanDashDuringState => false;

    public Player_FocusGetOnceState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(0f, 0f);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        player.SetVelocity(0f, 0f);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (triggerCalled)
        {
            if (input.Player.FocusCast.IsPressed() && player.health.CanHeal())
            {
                stateMachine.ChangeState(player.focusGetState);
            }
            else
            {
                stateMachine.ChangeState(player.idleState);
            }
        }
    }
}
