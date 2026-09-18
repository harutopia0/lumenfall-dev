using UnityEngine;

public class Enemy : Entity
{
    public Enemy_IdleState idleState { get; protected set; }
    public Enemy_MoveState moveState { get; protected set; }
    public Enemy_AttackState attackState { get; protected set; }
    public Enemy_BattleState battleState { get; protected set; }

    [Header("Battle details")]
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
}
