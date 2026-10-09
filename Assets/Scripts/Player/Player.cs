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
    public Player_Health health { get; private set; }
    public Player_Soul soul { get; private set; }

    public Player_IdleState idleState { get; private set; }
    public Player_RunState runState { get; private set; }
    public Player_JumpState jumpState { get; private set; }
    public Player_FallState fallState { get; private set; }
    public Player_WallSlideState wallSlideState { get; private set; }
    public Player_WallJumpState wallJumpState { get; private set; }
    public Player_DashState dashState { get; private set; }
    public Player_SlashState slashState { get; private set; }
    public Player_DeadState deadState { get; private set; }
    public Player_DashToIdleState dashToIdleState { get; private set; }
    public Player_LandState landState { get; private set; }
    public Player_SuperDashChargeState superDashChargeState { get; private set; }
    public Player_SuperDashChargeCancelState superDashChargeCancelState { get; private set; }
    public Player_SuperDashState superDashState { get; private set; }
    public Player_SuperDashAirBrakeState superDashAirBrakeState { get; private set; }
    public Player_SuperDashHitWallState superDashHitWallState { get; private set; }
    public Player_DoubleJumpState doubleJumpState { get; private set; }
    public Player_FocusChargeState focusChargeState { get; private set; }
    public Player_FocusGetState focusGetState { get; private set; }
    public Player_FocusGetOnceState focusGetOnceState { get; private set; }
    public Player_FocusEndState focusEndState { get; private set; }
    public Player_FireballState fireballState { get; private set; }
    public Player_ScreamState screamState { get; private set; }

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

    [Header("Focus Details")]
    // 7 frames @ 8 FPS: 7 / 8 = 0.875s (base focus charge time)
    public float focusChargeTime = 7f / 8f;

    [Header("Spell Details")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Vector3 fireballOffset = Vector3.zero;

    public float defaultGravityScale { get; private set; }

    protected override void Awake()
    {
        defaultFacing = FacingDirection.Left;
        base.Awake();

        input = new PlayerInputSet();
        health = GetComponent<Player_Health>();
        soul = GetComponent<Player_Soul>();
        defaultGravityScale = rb.gravityScale;

        idleState = new Player_IdleState(this, stateMachine, "idle");
        runState = new Player_RunState(this, stateMachine, "run");
        jumpState = new Player_JumpState(this, stateMachine, "isMidAir");
        fallState = new Player_FallState(this, stateMachine, "isMidAir");
        wallSlideState = new Player_WallSlideState(this, stateMachine, "wallSlide");
        wallJumpState = new Player_WallJumpState(this, stateMachine, "wallJump");
        dashState = new Player_DashState(this, stateMachine, "dash");
        slashState = new Player_SlashState(this, stateMachine, "slash");
        deadState = new Player_DeadState(this, stateMachine, "dead");
        dashToIdleState = new Player_DashToIdleState(this, stateMachine, "dashToIdle");
        landState = new Player_LandState(this, stateMachine, "land");
        superDashChargeState = new Player_SuperDashChargeState(this, stateMachine, "superDashCharge");
        superDashChargeCancelState = new Player_SuperDashChargeCancelState(this, stateMachine, "superDashChargeCancel");
        superDashState = new Player_SuperDashState(this, stateMachine, "superDash");
        superDashAirBrakeState = new Player_SuperDashAirBrakeState(this, stateMachine, "superDashAirBrake");
        superDashHitWallState = new Player_SuperDashHitWallState(this, stateMachine, "superDashHitWall");
        doubleJumpState = new Player_DoubleJumpState(this, stateMachine, "doubleJump");
        focusChargeState = new Player_FocusChargeState(this, stateMachine, "focus");
        focusGetState = new Player_FocusGetState(this, stateMachine, "focusGet");
        focusGetOnceState = new Player_FocusGetOnceState(this, stateMachine, "focusGetOnce");
        focusEndState = new Player_FocusEndState(this, stateMachine, "focusEnd");
        fireballState = new Player_FireballState(this, stateMachine, "fireball");
        screamState = new Player_ScreamState(this, stateMachine, "scream");
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 fbPos = transform.position + new Vector3(fireballOffset.x * (facingDir != 0 ? facingDir : 1), fireballOffset.y, fireballOffset.z);
        Gizmos.DrawWireSphere(fbPos, 0.15f);
    }

    public bool TryCastSpell()
    {
        if (soul != null && !soul.HasEnoughSoul(33))
        {
            return false;
        }

        soul?.ConsumeSoul(33);

        if (moveInput.y > 0.5f)
        {
            // UPWARD SPELL (W KEY): HOWLING WRAITHS
            stateMachine.ChangeState(screamState);
        }
        else if (moveInput.y < -0.5f && !groundDetected)
        {
            // DOWNWARD AIR SPELL (S KEY IN AIR): DESOLATE DIVE / DESCENDING DARK (QUAKE)
            // Note: Only triggers while mid-air (!groundDetected) matching Hollow Knight mechanics
            // TODO: When implementing the dive/quake spell, replace with:
            // stateMachine.ChangeState(quakeAnticState);
            stateMachine.ChangeState(fireballState);
        }
        else
        {
            // DEFAULT / HORIZONTAL SPELL (NEUTRAL OR A/D): VENGEFUL SPIRIT / SHADE SOUL
            stateMachine.ChangeState(fireballState);
        }

        return true;
    }

    public void TriggerScreamCast()
    {
        vfx?.PlayScreamVfx();
    }

    public void TriggerFireballCast()
    {
        vfx?.PlayFireballMuzzleVfx();
    }

    public void SpawnFireballProjectile()
    {
        if (fireballPrefab == null) return;
        Vector3 spawnPos = transform.position + new Vector3(fireballOffset.x * facingDir, fireballOffset.y, fireballOffset.z);
        GameObject fireball = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        Fireball_Projectile proj = fireball.GetComponent<Fireball_Projectile>();
        proj?.Initialize(facingDir);
        float recoilSpeed = groundDetected ? 4f : 5.5f;
        rb.linearVelocity = new Vector2(-facingDir * recoilSpeed, 0f);
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
        input.Player.Movement.performed += OnMovementPerformed;
        input.Player.Movement.canceled += OnMovementCanceled;
    }

    private void OnDisable()
    {
        input.Player.Movement.performed -= OnMovementPerformed;
        input.Player.Movement.canceled -= OnMovementCanceled;
        input.Disable();
    }

    private void OnMovementPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnMovementCanceled(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        moveInput = Vector2.zero;
    }
}
