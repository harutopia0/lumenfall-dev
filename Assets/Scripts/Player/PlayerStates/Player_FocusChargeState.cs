using UnityEngine;

public class Player_FocusChargeState : PlayerState
{
    private float chargeTimer;

    public Player_FocusChargeState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        chargeTimer = player.focusChargeTime;
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

        if (input.Player.FocusCast.WasReleasedThisFrame())
        {
            stateMachine.ChangeState(player.focusEndState);
            return;
        }

        chargeTimer -= Time.deltaTime;

        if (chargeTimer <= 0f)
        {
            stateMachine.ChangeState(player.focusGetState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        player.vfx?.SetFocusCharging(false);
    }
}
