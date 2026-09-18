using UnityEngine;

public class Entity : MonoBehaviour
{
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }

    protected StateMachine stateMachine;

    private bool facingRight = true;
    public int facingDir { get; private set; } = 1;

    [Header("Collision detection")]
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] private float ceilingCheckDistance = 0.7f;
    [SerializeField] private float groundCheckDistance = 1.35f;
    [SerializeField] private float wallCheckDistance = 0.4f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform primaryWallCheck;
    [SerializeField] private Transform secondaryWallCheck;
    public bool ceilingDetected { get; private set; }
    public bool groundDetected { get; private set; }
    public bool wallDetected { get; private set; }

    protected virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        stateMachine = new StateMachine();
    }

    protected virtual void Start()
    {
        
    }

    private void Update()
    {
        HandleCollisionDetection();
        stateMachine.UpdateActiveState();
    }

    public void CallAnimationTrigger()
    {
        stateMachine.currentState.CallAnimationTrigger();
    }

    public void SetVelocity(float xVelocity, float yVelocity)
    {
        rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        HandleFlip(xVelocity);
    }

    private void HandleFlip(float xVelocity)
    {
        if ((xVelocity > 0 && !facingRight || xVelocity < 0 && facingRight))
        {
            Flip();
        }
    }

    public void Flip()
    {
        transform.Rotate(0f, 180f, 0f);
        facingRight = !facingRight;
        facingDir = facingRight ? 1 : -1;
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
        GizmosDrawLine(transform.position, transform.position + new Vector3(0, ceilingCheckDistance), Color.paleVioletRed);

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
