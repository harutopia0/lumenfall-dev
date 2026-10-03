using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TransitionPoint : MonoBehaviour
{
    public enum GizmoDrawMode
    {
        Wireframe,
        Fill,
        Both
    }

    [Header("Destination")]
    [Tooltip("Target scene name to load upon trigger contact.")]
    [SerializeField] private string targetSceneName;

    [Tooltip("Target spawn point identifier in the destination scene.")]
    [SerializeField] private string targetSpawnPointName;

    [Header("Gizmos")]
    [Tooltip("Toggles rendering of gate trigger gizmos in the Scene view.")]
    [SerializeField] private bool showGizmos = true;

    [Tooltip("Gizmo rendering style (Wireframe, Fill, or Both).")]
    [SerializeField] private GizmoDrawMode drawMode = GizmoDrawMode.Both;

    [Tooltip("Color and opacity of the gate trigger gizmo.")]
    [SerializeField] private Color gizmoColor = new Color(0f, 0.7f, 1f, 0.25f);

    private Collider2D col;

    private Collider2D Col
    {
        get
        {
            if (col == null) col = GetComponent<Collider2D>();
            return col;
        }
    }

    private bool isTransitioning = false;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;

        if (other.CompareTag("Player") || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")))
        {
            isTransitioning = true;
            SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnPointName);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (Col != null)
        {
            if (drawMode == GizmoDrawMode.Fill || drawMode == GizmoDrawMode.Both)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawCube(Col.bounds.center, Col.bounds.size);
            }

            if (drawMode == GizmoDrawMode.Wireframe || drawMode == GizmoDrawMode.Both)
            {
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.85f);
                Gizmos.DrawWireCube(Col.bounds.center, Col.bounds.size);
            }
        }
    }
}
