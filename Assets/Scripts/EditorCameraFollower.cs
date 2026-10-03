#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class EditorPreviewCamera : MonoBehaviour
{
    [Header("Tracking")]
    [Tooltip("Toggles automatic tracking of Scene view center while in Edit mode.")]
    [SerializeField] private bool followSceneView = true;

    [Tooltip("Camera distance on Z-axis relative to target plane.")]
    [SerializeField] private float cameraDistanceZ = -10f;

    private void OnEnable()
    {
        EditorApplication.update += SyncWithSceneView;
    }

    private void OnDisable()
    {
        EditorApplication.update -= SyncWithSceneView;
    }

    private void SyncWithSceneView()
    {
        if (Application.isPlaying || !followSceneView) return;

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            Vector3 pivot = sceneView.pivot;
            transform.position = new Vector3(pivot.x, pivot.y, cameraDistanceZ);
        }
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            DestroyImmediate(gameObject);
        }
    }
}
#endif
