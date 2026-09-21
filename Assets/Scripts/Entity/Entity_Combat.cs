using UnityEngine;

public class Entity_Combat : MonoBehaviour
{
    private Entity entity;

    public enum AttackDirection
    {
        Side,
        Up,
        Down
    }

    public float damage = 1f;

    [Header("Attack Directions Setup")]
    public AttackDirection currentAttackDir = AttackDirection.Side;
    [SerializeField] private Transform targetCheckSide;
    [SerializeField] private Vector2 boxSizeSide;
    [SerializeField] private Transform targetCheckUp;
    [SerializeField] private Vector2 boxSizeUp;
    [SerializeField] private Transform targetCheckDown;
    [SerializeField] private Vector2 boxSizeDown;

    [Header("Pogo Mechanics")]
    [SerializeField] private float pogoBounceForce = 13f;

    [Header("Target detection")]
    [SerializeField] private LayerMask whatIsTarget;

    [Header("Recoil on Hit")]
    [SerializeField] private Vector2 recoilPower = new Vector2(8.25f, 0f);
    [SerializeField] private float recoilDuration = 0.1f;

    private void Awake()
    {
        entity = GetComponent<Entity>();
    }

    public void PerformAttack()
    {
        Transform checkPoint = GetCurrentCheckTransform();
        Vector2 boxSize = GetCurrentBoxSize();

        Collider2D[] targets = Physics2D.OverlapBoxAll(checkPoint.position, boxSize, 0f, whatIsTarget);

        if (targets.Length > 0)
        {
            if (currentAttackDir == AttackDirection.Side)
            {
                ApplyRecoil();
            }
            else if (currentAttackDir == AttackDirection.Down)
            {
                ApplyPogo();
            }
        }

        foreach (Collider2D target in targets)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(damage, transform);
        }
    }

    public Transform GetCurrentCheckTransform()
    {
        switch (currentAttackDir)
        {
            case AttackDirection.Up:
                return targetCheckUp != null ? targetCheckUp : transform;
            case AttackDirection.Down:
                return targetCheckDown != null ? targetCheckDown : transform;
            case AttackDirection.Side:
            default:
                return targetCheckSide != null ? targetCheckSide : transform;
        }
    }

    private Vector2 GetCurrentBoxSize()
    {
        switch (currentAttackDir)
        {
            case AttackDirection.Up:
                return boxSizeUp;
            case AttackDirection.Down:
                return boxSizeDown;
            case AttackDirection.Side:
            default:
                return boxSizeSide;
        }
    }

    private void ApplyPogo()
    {
        entity.rb.linearVelocity = new Vector2(entity.rb.linearVelocity.x, pogoBounceForce);

        if (entity is Player player)
        {
            player.canAirDash = true;
        }
    }

    private void ApplyRecoil()
    {
        if (recoilPower == Vector2.zero || entity == null) return;
        Vector2 recoil = new Vector2(-entity.facingDir * recoilPower.x, recoilPower.y);
        entity.ReceiveKnockback(recoil, recoilDuration);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (targetCheckSide != null) Gizmos.DrawWireCube(targetCheckSide.position, boxSizeSide);

        Gizmos.color = Color.cyan;
        if (targetCheckUp != null) Gizmos.DrawWireCube(targetCheckUp.position, boxSizeUp);

        Gizmos.color = Color.yellow;
        if (targetCheckDown != null) Gizmos.DrawWireCube(targetCheckDown.position, boxSizeDown);
    }
}
