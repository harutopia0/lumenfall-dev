using UnityEngine;

public class Player_FocusGetState : Player_GroundedState
{
    // Start at frame 5 of 10: 5 / 10 = 0.5f (skips initial burst to start charging immediately)
    private const float CHARGE_START_NORMALIZED_TIME = 5f / 10f;

    // Start charge VFX anim at frame 8 (index 7) of 13 total frames: 7 / 13 = ~0.5385f
    private const float CHARGE_VFX_START_NORMALIZED_TIME = 7f / 13f;

    public override bool CanDashDuringState => false;

    public Player_FocusGetState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(0f, 0f);

        player.vfx?.SetFocusCharging(true, CHARGE_VFX_START_NORMALIZED_TIME);
        player.anim.Play("playerFocusGet", 0, CHARGE_START_NORMALIZED_TIME);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        player.SetVelocity(0f, 0f);
    }

    public override void AnimationTrigger(string triggerName)
    {
        base.AnimationTrigger(triggerName);
        if (triggerName == "StartCharge")
        {
            player.vfx?.SetFocusCharging(true, CHARGE_VFX_START_NORMALIZED_TIME);
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (input.Player.FocusCast.WasReleasedThisFrame())
        {
            stateMachine.ChangeState(player.focusEndState);
            return;
        }

        if (triggerCalled)
        {
            player.health.Heal(1);
            player.vfx?.PlayFocusBurstVfx(player.transform.position);
            player.vfx?.SetFocusCharging(false);

            if (input.Player.FocusCast.IsPressed() && player.health.CanHeal())
            {
                triggerCalled = false;
                player.anim.Play("playerFocusGet", 0, 0f);
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
        player.vfx?.SetFocusCharging(false);
    }
}
