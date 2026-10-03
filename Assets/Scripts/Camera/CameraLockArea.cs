using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraLockArea : MonoBehaviour
{
    public enum GizmoDrawMode
    {
        Wireframe,
        Fill,
        Both
    }

    [Header("Gizmos")]
    [Tooltip("Toggles rendering of boundary gizmos in the Scene view.")]
    [SerializeField] private bool showGizmos = true;

    [Tooltip("Gizmo rendering style (Wireframe, Fill, or Both).")]
    [SerializeField] private GizmoDrawMode drawMode = GizmoDrawMode.Both;

    [Tooltip("Color and opacity of the boundary gizmo.")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.2f);

    private BoxCollider2D box;

    public float MinX => box.bounds.min.x;
    public float MaxX => box.bounds.max.x;
    public float MinY => box.bounds.min.y;
    public float MaxY => box.bounds.max.y;
    public Vector2 Center => box.bounds.center;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other) && CameraController.Instance != null)
        {
            CameraController.Instance.RegisterArea(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other) && CameraController.Instance != null)
        {
            CameraController.Instance.UnregisterArea(this);
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") ||
               (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"));
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (box == null) box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            if (drawMode == GizmoDrawMode.Fill || drawMode == GizmoDrawMode.Both)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawCube(box.bounds.center, box.bounds.size);
            }

            if (drawMode == GizmoDrawMode.Wireframe || drawMode == GizmoDrawMode.Both)
            {
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);
                Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
            }
        }
    }
}
