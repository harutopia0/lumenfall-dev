using UnityEngine;

public abstract class EntityState
{
    protected Player player;
    protected StateMachine stateMachine;
    protected string animBoolName;

    protected Animator anim;
    protected Rigidbody2D rb;
    protected PlayerInputSet input;

    public EntityState(Player player, StateMachine stateMachine, string animBoolName)
    {
        this.player = player;
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;

        this.anim = player.anim;
        this.rb = player.rb;
        this.input = player.input;
    }

    public virtual void Enter()
    {
        anim.SetBool(animBoolName, true);
    }

    public virtual void Update()
    {
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);
    }
}
