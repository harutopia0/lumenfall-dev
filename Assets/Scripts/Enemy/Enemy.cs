using UnityEngine;

public class Enemy : Entity
{
    public Enemy_IdleState idleState { get; protected set; }
    public Enemy_MoveState moveState { get; protected set; }
    public Enemy_AttackState attackState { get; protected set; }
    public Enemy_BattleState battleState { get; protected set; }
    public Enemy_DeadState deadState { get; protected set; }

    [Header("Battle details")]
    public float attackCooldown = 1f;
    [HideInInspector] public float lastTimeAttacked;
    public float battleMoveSpeed = 3f;
    public float attackDistance = 2f;
    public float battleTimeDuration = 3f;
    public float minRetreatDistance = 1f;
    public Vector2 retreatVelocity;

    [Header("Movement details")]
    public float idleTime = 2;
    public float moveSpeed = 1.5f;
    [Range(0,2)]
    public float moveAnimSpeedMultiplier = 1f;

    [Header("Player detection")]
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private Transform playerCheck;
    [SerializeField] private float playerCheckDistance = 10f;
    public Transform player { get; private set; }

    public override void EntityDeath()
    {
        base.EntityDeath();

        stateMachine.ChangeState(deadState);
    }

    private void HandlePlayerDeath()
    {
        if (stateMachine.currentState == battleState
            || stateMachine.currentState == attackState)
        {
            stateMachine.ChangeState(idleState);
        }
    }

    public void TryToEnterBattleState(Transform player)
    {
        if (stateMachine.currentState == battleState
            || stateMachine.currentState == attackState
            || stateMachine.currentState == deadState)
        {
            return;
        }

        this.player = player;
        stateMachine.ChangeState(battleState);
    }

    public Transform GetPlayerReference()
    {
        if (player == null)
        {
            player = PlayerDetected().transform;
        }

        return player;
    }

    public RaycastHit2D PlayerDetected()
    {
        RaycastHit2D hit = Physics2D.Raycast(playerCheck.position, Vector2.right * facingDir, playerCheckDistance, whatIsPlayer | whatIsGround);

        if (hit.collider == null || hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            return default;
        }

        return hit;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        GizmosDrawLine(playerCheck.position, new Vector3(playerCheck.position.x + (facingDir * playerCheckDistance), playerCheck.position.y), Color.lightSalmon);
        GizmosDrawLine(playerCheck.position, new Vector3(playerCheck.position.x + (facingDir * attackDistance), playerCheck.position.y), Color.mediumSpringGreen);
        GizmosDrawLine(playerCheck.position, new Vector3(playerCheck.position.x + (facingDir * minRetreatDistance), playerCheck.position.y), Color.hotPink);
    }

    private void OnEnable()
    {
        Player.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        Player.OnPlayerDeath -= HandlePlayerDeath;
    }
}
