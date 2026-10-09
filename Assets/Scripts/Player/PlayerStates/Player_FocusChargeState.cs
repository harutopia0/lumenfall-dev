using UnityEngine;

public class Player_FocusChargeState : PlayerState
{
    public override bool CanDashDuringState => false;

    public Player_FocusChargeState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(0f, 0f);
        player.vfx?.SetFocusCharging(true);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        player.SetVelocity(0f, 0f);
    }

    public override void Update()
    {
        base.Update();

        if (input.Player.Focus.WasReleasedThisFrame())
        {
            stateMachine.ChangeState(player.focusEndState);
            return;
        }

        if (triggerCalled)
        {
            player.health.Heal(1);
            player.vfx?.PlayFocusBurstVfx(player.transform.position);
            stateMachine.ChangeState(player.focusGetOnceState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        player.vfx?.SetFocusCharging(false);
    }
}
