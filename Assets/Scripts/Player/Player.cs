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
    public Player_MoveState moveState { get; private set; }
    public Player_JumpState jumpState { get; private set; }
    public Player_FallState fallState { get; private set; }
    public Player_WallSlideState wallSlideState { get; private set; }
    public Player_WallJumpState wallJumpState { get; private set; }
    public Player_DashState dashState { get; private set; }
    public Player_BasicAttackState basicAttackState { get; private set; }
    public Player_PlungeAttackState plungeAttackState { get; private set; }
    public Player_DeadState deadState { get; private set; }

    [Header("Attack details")]
    public Vector2[] attackVelocity = new Vector2[]
{
        new Vector2(3f, 1.5f),
        new Vector2(1f, 1.25f),
        new Vector2(2.75f, 1f)
};
    public Vector2 plungeAttackVelocity = new Vector2(3f, -15f);
    public float plungePrepJumpForce = 7.5f;
    public float attackVelocityDuration = 0.1f;
    public float comboResetTime = 1;
    private Coroutine queuedAttackCo;

    [Header("Movements details")]
    public float moveSpeed;
    public float jumpForce = 12;
    public Vector2 wallJumpForce;
    public float inAirMoveMultiplier = 0.75f;
    public float wallSlideSlowMultiplier = 0.3f;
    public float dashDuration = 0.25f;
    public float dashSpeed = 20;
    public Vector2 moveInput { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        input = new PlayerInputSet();

        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        jumpState = new Player_JumpState(this, stateMachine, "isMidAir");
        fallState = new Player_FallState(this, stateMachine, "isMidAir");
        wallSlideState = new Player_WallSlideState(this, stateMachine, "wallSlide");
        wallJumpState = new Player_WallJumpState(this, stateMachine, "isMidAir");
        dashState = new Player_DashState(this, stateMachine, "dash");
        basicAttackState = new Player_BasicAttackState(this, stateMachine, "basicAttack");
        plungeAttackState = new Player_PlungeAttackState(this, stateMachine, "plungeAttack");
        deadState = new Player_DeadState(this, stateMachine, "dead");
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

    public void EnterAttackStateWithDelay()
    {
        if (queuedAttackCo != null)
        {
            StopCoroutine(queuedAttackCo);
        }

        queuedAttackCo = StartCoroutine(EnterAttackStateWithDelayCo());
    }

    private IEnumerator EnterAttackStateWithDelayCo()
    {
        yield return new WaitForEndOfFrame();
        stateMachine.ChangeState(basicAttackState);
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
