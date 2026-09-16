using UnityEngine;

public class Player_PlungeAttackState : EntityState
{

    private bool touchedGround;
    private bool isPlunging;

    public Player_PlungeAttackState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        touchedGround = false;
        isPlunging = false;

        player.SetVelocity(0f, player.plungePrepJumpForce);
    }

    public override void Update()
    {
        base.Update();

        if (!isPlunging && rb.linearVelocity.y <= 0f)
        {
            isPlunging = true;
            player.SetVelocity(player.plungeAttackVelocity.x * player.facingDir, player.plungeAttackVelocity.y);
        }

        if (player.groundDetected && !touchedGround)
        {
            touchedGround = true;

            anim.SetTrigger("plungeAttackTrigger");
            player.SetVelocity(0, rb.linearVelocity.y);
        }

        if (touchedGround)
        {
            player.SetVelocity(0, rb.linearVelocity.y);
        }

        if (triggerCalled && player.groundDetected)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        base.Exit();


    }
}
