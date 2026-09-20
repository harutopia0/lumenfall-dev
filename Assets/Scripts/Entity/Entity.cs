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
    [SerializeField] private float ceilingCheckDistance = 1.125f;
    [SerializeField] private float groundCheckDistance = 1.025f;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform primaryWallCheck;
    [SerializeField] private Transform secondaryWallCheck;
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

    protected virtual void Update()
    {
        HandleCollisionDetection();
        stateMachine.UpdateActiveState();
    }

    public void CurrentStateAnimationTrigger()
    {
        stateMachine.currentState.AnimationTrigger();
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

        rb.linearVelocity = Vector2.zero;
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
        transform.Rotate(0f, 180f, 0f);
        facingDirection = (facingDirection == FacingDirection.Right) 
            ? FacingDirection.Left 
            : FacingDirection.Right;
    }

    private void HandleCollisionDetection()
    {
        ceilingDetected = Physics2D.Raycast(transform.position, Vector2.up, groundCheckDistance, whatIsGround);

        groundDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);

        if (secondaryWallCheck != null)
        {
            wallDetected = Physics2D.Raycast(primaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround)
                    && Physics2D.Raycast(secondaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
        }
        else
        {
            wallDetected = Physics2D.Raycast(primaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
        }
    }

    protected virtual void OnDrawGizmos()
    {
        GizmosDrawLine(transform.position, transform.position + new Vector3(0, ceilingCheckDistance), Color.crimson);

        GizmosDrawLine(groundCheck.position, groundCheck.position + new Vector3(0, -groundCheckDistance), Color.greenYellow);

        GizmosDrawLine(primaryWallCheck.position, primaryWallCheck.position + new Vector3(wallCheckDistance * facingDir, 0), Color.lightCoral);
        if (secondaryWallCheck != null)
        {
            GizmosDrawLine(secondaryWallCheck.position, secondaryWallCheck.position + new Vector3(wallCheckDistance * facingDir, 0), Color.lightCoral);
        }
    }

    protected void GizmosDrawLine(Vector3 from, Vector3 to, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(from, to);
    }
}
