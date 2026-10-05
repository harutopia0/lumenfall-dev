using UnityEngine;

public class Player_FocusGetState : Player_GroundedState
{
    private float chewDuration = 0.417f;
    private float chewTimer;

    public Player_FocusGetState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        chewTimer = chewDuration;
        player.SetVelocity(0f, 0f);

        player.anim.Play("playerFocusGet", 0, 0.546f);
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

        if (input.Player.FocusCast.WasReleasedThisFrame())
        {
            stateMachine.ChangeState(player.focusEndState);
            return;
        }

        chewTimer -= Time.deltaTime;

        if (chewTimer <= 0f)
        {
            player.health.Heal(1);
            player.vfx?.PlayFocusBurstVfx(player.transform.position);

            if (input.Player.FocusCast.IsPressed() && player.health.CanHeal())
            {
                chewTimer = 0.917f;
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
    }
}
