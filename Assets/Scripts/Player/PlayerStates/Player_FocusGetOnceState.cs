using UnityEngine;

public class Player_FocusGetOnceState : Player_GroundedState
{
    private float duration = 0.417f;
    private float timer;

    public Player_FocusGetOnceState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        timer = duration;
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

        timer -= Time.deltaTime;

        if (timer <= 0f)
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

    public override void Exit()
    {
        base.Exit();
    }
}
