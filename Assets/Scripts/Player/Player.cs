using System;
using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;

public class Player : Entity
{
    public static event Action OnPlayerDeath;

    public PlayerInputSet input { get; private set; }

    public Player_IdleState idleState { get; private set; }
    public Player_RunState moveState { get; private set; }
    public Player_JumpState jumpState { get; private set; }
    public Player_FallState fallState { get; private set; }
    public Player_WallSlideState wallSlideState { get; private set; }
    public Player_WallJumpState wallJumpState { get; private set; }
    public Player_DashState dashState { get; private set; }
    public Player_SlashState slashState { get; private set; }
    public Player_PlungeAttackState plungeAttackState { get; private set; }
    public Player_DeadState deadState { get; private set; }
    public Player_DashToIdleState dashToIdleState { get; private set; }
    public Player_LandState landState { get; private set; }

    [Header("Attack details")]
    public Vector2 plungeAttackVelocity = new Vector2(3f, -15f);
    public float plungePrepJumpForce = 7.5f;

    [Header("Movements details")]
    public float moveSpeed;
    public float jumpForce = 12;
    public Vector2 wallJumpForce;
    public float inAirMoveMultiplier = 0.75f;
    public float wallSlideSlowMultiplier = 0.3f;
    public float dashDuration = 0.25f;
    public float dashSpeed = 20f;
    public float dashCooldown = 0.6f;
    public float lastDashTime { get; set; }
    public bool canAirDash { get; set; } = true;
    public Vector2 moveInput { get; private set; }

    protected override void Awake()
    {
        defaultFacing = FacingDirection.Left;
        base.Awake();

        input = new PlayerInputSet();

        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_RunState(this, stateMachine, "run");
        jumpState = new Player_JumpState(this, stateMachine, "isMidAir");
        fallState = new Player_FallState(this, stateMachine, "isMidAir");
        wallSlideState = new Player_WallSlideState(this, stateMachine, "wallSlide");
        wallJumpState = new Player_WallJumpState(this, stateMachine, "isMidAir");
        dashState = new Player_DashState(this, stateMachine, "dash");
        slashState = new Player_SlashState(this, stateMachine, "slash");
        plungeAttackState = new Player_PlungeAttackState(this, stateMachine, "plungeAttack");
        deadState = new Player_DeadState(this, stateMachine, "dead");
        dashToIdleState = new Player_DashToIdleState(this, stateMachine, "dashToIdle");
        landState = new Player_LandState(this, stateMachine, "land");
    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initalize(idleState);
    }

    public override void EntityDeath()
    {
        base.EntityDeath();

        OnPlayerDeath?.Invoke();
        stateMachine.ChangeState(deadState);
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Movement.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnDisable()
    {
        input.Disable();
    }
}
