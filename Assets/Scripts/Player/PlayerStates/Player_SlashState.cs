using UnityEngine;

public class Player_SlashState : PlayerState
{
    private Entity_Combat combat;

    public Player_SlashState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        combat = player.GetComponent<Entity_Combat>();
    }

    public override void Enter()
    {
        base.Enter();

        if (player.moveInput.y > 0.5f)
        {
            combat.currentAttackDir = Entity_Combat.AttackDirection.Up;
            anim.SetInteger("slashDirY", 1);
        }
        else if (player.moveInput.y < -0.5f && !player.groundDetected)
        {
            combat.currentAttackDir = Entity_Combat.AttackDirection.Down;
            anim.SetInteger("slashDirY", -1);
        }
        else
        {
            combat.currentAttackDir = Entity_Combat.AttackDirection.Side;
            anim.SetInteger("slashDirY", 0);
        }

        player.vfx?.PlaySlashVfx(combat.currentAttackDir);

        if (player.groundDetected)
        {
            player.SetVelocity(0, rb.linearVelocity.y);
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.currentState != this) return;

        if (player.groundDetected)
        {
            if (player.moveInput.x != 0)
                player.SetVelocity(player.moveInput.x * player.moveSpeed, rb.linearVelocity.y);
            else
                player.SetVelocity(0, rb.linearVelocity.y);
        }
        else
        {
            if (player.moveInput.x != 0)
                player.SetVelocity(player.moveInput.x * (player.moveSpeed * player.inAirMoveMultiplier), rb.linearVelocity.y);
        }

        if (triggerCalled)
        {
            if (player.groundDetected)
            {
                if (player.moveInput.x != 0)
                    stateMachine.ChangeState(player.runState);
                else
                    stateMachine.ChangeState(player.idleState);
            }
            else
            {
                stateMachine.ChangeState(player.fallState);
            }
        }

    }

}
