using UnityEngine;

public class Entity_Combat : MonoBehaviour
{
    private Entity entity;

    public float damage = 1f;

    [Header("Target detection")]
    [SerializeField] private Transform targetCheck;
    [SerializeField] private float targetCheckRadius;
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
        Collider2D[] targets = GetDetectedColliders();
        if (targets.Length > 0)
        {
            ApplyRecoil();
        }

        foreach (Collider2D target in targets)
        {
            
            IDamageable damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(damage, transform);
        }
    }

    private void ApplyRecoil()
    {
        if (recoilPower == Vector2.zero || entity == null) return;
        Vector2 recoil = new Vector2(-entity.facingDir * recoilPower.x, recoilPower.y);
        entity.ReceiveKnockback(recoil, recoilDuration);
    }

    private Collider2D[] GetDetectedColliders()
    {
        return Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(targetCheck.position, targetCheckRadius);
    }
}
