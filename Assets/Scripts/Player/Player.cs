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
    public Player_DoubleJumpState doubleJumpState { get; private set; }

    [Header("Attack details")]
    public Vector2 plungeAttackVelocity = new Vector2(3f, -15f);
    public float plungePrepJumpForce = 7.5f;

    [Header("Movements details")]
    public float moveSpeed = 8.5f;
    public float jumpForce = 13.5f;
    public float jumpCutMultiplier = 0.5f;
    public float fallGravityMultiplier = 1.4f;
    public float coyoteTime = 0.1f;
    public float coyoteTimer { get; set; }
    public float jumpBufferTime = 0.1f;
    public float jumpBufferTimer { get; set; }
    public float dashDuration = 0.25f;
    public float dashSpeed = 25f;
    public float dashCooldown = 0.6f;
    public float lastDashTime { get; set; }
    public bool canAirDash { get; set; } = true;
    public Vector2 moveInput { get; private set; }

    [Header("Wall Jump Details")]
    public Vector2 wallJumpForce = new Vector2(10f, 14f);
    public float wallJumpPushOffDuration = 0.16f;
    public float wallSlideSpeed = 4.5f;
    public float wallSlideFastSpeed = 12f;

    [Header("Double Jump Details")]
    public float doubleJumpForce = 21f;
    public bool canDoubleJump { get; set; } = true;

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
        wallJumpState = new Player_WallJumpState(this, stateMachine, "wallJump");
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
        doubleJumpState = new Player_DoubleJumpState(this, stateMachine, "doubleJump");
    }
    protected override void Update()
    {
        if (groundDetected)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (input.Player.Jump.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        base.Update();
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
