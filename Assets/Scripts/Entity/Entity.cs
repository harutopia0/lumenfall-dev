using System.Collections;
using UnityEngine;

public enum FacingDirection
{
    Left = -1,
    Right = 1
}

public class Entity : MonoBehaviour
{
    public Entity_VFX vfx { get; private set; }
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }

    protected StateMachine stateMachine;

    [Header("Facing Direction")]
    [SerializeField] protected FacingDirection defaultFacing = FacingDirection.Right;

    public FacingDirection facingDirection { get; private set; } = FacingDirection.Right;
    public int facingDir => (int)facingDirection;

    [Header("Collision detection")]
    [SerializeField] protected LayerMask whatIsGround;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 1.3f;
    [SerializeField] private Vector2 groundBoxSize = new Vector2(0.7f, 0.1f);

    [Header("Ceiling Check")]
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private float ceilingCheckDistance = 0.85f;
    [SerializeField] private Vector2 ceilingBoxSize = new Vector2(0.7f, 0.1f);

    [Header("Wall Check")]
    [SerializeField] private Transform primaryWallCheck;
    [SerializeField] private Transform secondaryWallCheck;
    [SerializeField] private float wallCheckDistance = 0.45f;

    public bool ceilingDetected { get; private set; }
    public bool groundDetected { get; private set; }
    public bool wallDetected { get; private set; }

    private bool isKnocked;
    private Coroutine knockbackCo;

    protected virtual void Awake()
    {
        facingDirection = defaultFacing;

        vfx = GetComponent<Entity_VFX>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        stateMachine = new StateMachine();
    }

    protected virtual void Start()
    {
        
    }

    protected virtual void FixedUpdate()
    {
        HandleCollisionDetection();
        stateMachine.PhysicsUpdateActiveState();
    }

    protected virtual void Update()
    {
        stateMachine.UpdateActiveState();
    }

    public void CurrentStateAnimationTrigger()
    {
        stateMachine.currentState.AnimationTrigger();
    }

    public void CustomAnimationTrigger(string triggerName)
    {
        stateMachine.currentState.AnimationTrigger(triggerName);
    }

    public virtual void EntityDeath()
    {
        
    }

    public void ReceiveKnockback(Vector2 knockback, float duration)
    {
        if (knockbackCo != null)
        {
            StopCoroutine(knockbackCo);
        }

        knockbackCo = StartCoroutine(KnockbackCo(knockback, duration));
    }

    private IEnumerator KnockbackCo(Vector2 knockback, float duration)
    {
        isKnocked = true;
        rb.linearVelocity = knockback;

        yield return new WaitForSeconds(duration);

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isKnocked = false;
    }

    public void SetVelocity(float xVelocity, float yVelocity)
    {
        if(isKnocked) return;

        rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        HandleFlip(xVelocity);
    }

    public void HandleFlip(float xVelocity)
    {
        if ((xVelocity > 0 && facingDirection == FacingDirection.Left) || 
            (xVelocity < 0 && facingDirection == FacingDirection.Right))
        {
            Flip();
        }
    }

    public void Flip()
    {
        facingDirection = (facingDirection == FacingDirection.Right) 
            ? FacingDirection.Left 
            : FacingDirection.Right;

        float scaleX = (facingDirection == defaultFacing) ? 1f : -1f;
        transform.localScale = new Vector3(scaleX * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void HandleCollisionDetection()
    {
        Vector2 originBase = rb != null ? rb.position : (Vector2)transform.position;

        Vector2 ceilingOrigin = ceilingCheck != null
            ? originBase + (Vector2)(ceilingCheck.position - transform.position)
            : originBase;
        RaycastHit2D ceilingHit = Physics2D.BoxCast(ceilingOrigin, ceilingBoxSize, 0f, Vector2.up, ceilingCheckDistance, whatIsGround);
        ceilingDetected = ceilingHit.collider != null;

        Vector2 groundOrigin = groundCheck != null
            ? originBase + (Vector2)(groundCheck.position - transform.position)
            : originBase;
        RaycastHit2D groundHit = Physics2D.BoxCast(groundOrigin, groundBoxSize, 0f, Vector2.down, groundCheckDistance, whatIsGround);
        groundDetected = groundHit.collider != null;

        Vector2 wallOrigin = primaryWallCheck != null
            ? originBase + (Vector2)(primaryWallCheck.position - transform.position)
            : originBase;

        if (secondaryWallCheck != null)
        {
            Vector2 secondaryWallOrigin = originBase + (Vector2)(secondaryWallCheck.position - transform.position);
            wallDetected = Physics2D.Raycast(wallOrigin, Vector2.right * facingDir, wallCheckDistance, whatIsGround)
                    && Physics2D.Raycast(secondaryWallOrigin, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
        }
        else
        {
            wallDetected = Physics2D.Raycast(wallOrigin, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
        }
    }

    protected virtual void OnDrawGizmos()
    {
        Vector3 ceilingPos = ceilingCheck != null ? ceilingCheck.position : transform.position;
        Gizmos.color = Color.crimson;
        Gizmos.DrawLine(ceilingPos, ceilingPos + new Vector3(0, ceilingCheckDistance));
        Gizmos.DrawWireCube(ceilingPos + new Vector3(0, ceilingCheckDistance), ceilingBoxSize);

        Vector3 groundPos = groundCheck != null ? groundCheck.position : transform.position;
        Gizmos.color = Color.greenYellow;
        Gizmos.DrawLine(groundPos, groundPos + new Vector3(0, -groundCheckDistance));
        Gizmos.DrawWireCube(groundPos + new Vector3(0, -groundCheckDistance), groundBoxSize);

        Gizmos.color = Color.lightCoral;
        Vector3 primaryWallPos = primaryWallCheck != null ? primaryWallCheck.position : transform.position;
        Gizmos.DrawLine(primaryWallPos, primaryWallPos + new Vector3(wallCheckDistance * facingDir, 0));

        if (secondaryWallCheck != null)
        {
            Gizmos.DrawLine(secondaryWallCheck.position, secondaryWallCheck.position + new Vector3(wallCheckDistance * facingDir, 0));
        }
    }

}
