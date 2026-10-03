using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TransitionPoint : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Target scene name to load upon trigger contact.")]
    [SerializeField] private string targetSceneName;

    [Tooltip("Target spawn point identifier in the destination scene.")]
    [SerializeField] private string targetSpawnPointName;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;

        if (other.CompareTag("Player") || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")))
        {
            isTransitioning = true;
            SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnPointName);
        }
    }
}
