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
    public Player_RunState runState { get; private set; }
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
    public Player_SuperDashChargeState superDashChargeState { get; private set; }
    public Player_SuperDashChargeCancelState superDashChargeCancelState { get; private set; }
    public Player_SuperDashState superDashState { get; private set; }
    public Player_SuperDashAirBrakeState superDashAirBrakeState { get; private set; }
    public Player_SuperDashHitWallState superDashHitWallState { get; private set; }

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

    [Header("Super Dash details")]
    public float superDashSpeed = 35f;
    public float superDashChargeTime = 0.8f;
    public float defaultGravityScale { get; private set; }

    protected override void Awake()
    {
        defaultFacing = FacingDirection.Left;
        base.Awake();

        input = new PlayerInputSet();
        defaultGravityScale = rb.gravityScale;

        idleState = new Player_IdleState(this, stateMachine, "idle");
        runState = new Player_RunState(this, stateMachine, "run");
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
        superDashChargeState = new Player_SuperDashChargeState(this, stateMachine, "superDashCharge");
        superDashChargeCancelState = new Player_SuperDashChargeCancelState(this, stateMachine, "superDashChargeCancel");
        superDashState = new Player_SuperDashState(this, stateMachine, "superDash");
        superDashAirBrakeState = new Player_SuperDashAirBrakeState(this, stateMachine, "superDashAirBrake");
        superDashHitWallState = new Player_SuperDashHitWallState(this, stateMachine, "superDashHitWall");
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
