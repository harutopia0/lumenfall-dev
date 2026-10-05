using UnityEngine;

public class Player_FocusGetState : Player_GroundedState
{
    private float getDuration = 0.45f;
    private float getTimer;

    public Player_FocusGetState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        getTimer = getDuration;
        player.SetVelocity(0f, 0f);

        player.health.Heal(1);

        player.vfx?.PlayFocusBurstVfx(player.transform.position);
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

        getTimer -= Time.deltaTime;

        if (getTimer <= 0f)
        {
            if (input.Player.FocusCast.IsPressed() && player.health.CanHeal())
            {
                stateMachine.ChangeState(player.focusChargeState);
            }
            else
            {
                stateMachine.ChangeState(player.focusEndState);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
