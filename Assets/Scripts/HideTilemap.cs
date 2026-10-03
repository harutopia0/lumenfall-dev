using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[DisallowMultipleComponent]
public class HideTilemap : MonoBehaviour
{
    [Tooltip("If enabled, hides renderers of child GameObjects as well.")]
    [SerializeField] private bool hideChildren = false;

    [Tooltip("Toggles hiding in Edit mode for previewing final visuals.")]
    [SerializeField] private bool hideInEditMode = true;

    private void Awake()
    {
        ApplyVisibility();
    }

    private void OnEnable()
    {
        ApplyVisibility();
    }

    private void OnValidate()
    {
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        if (Application.isPlaying || hideInEditMode)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Hide()
    {
        if (TryGetComponent<TilemapRenderer>(out var tilemapRenderer))
        {
            tilemapRenderer.enabled = false;
        }
        else if (TryGetComponent<Renderer>(out var genericRenderer))
        {
            genericRenderer.enabled = false;
        }

        if (hideChildren)
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in childRenderers)
            {
                r.enabled = false;
            }
        }
    }

    public void Show()
    {
        if (TryGetComponent<TilemapRenderer>(out var tilemapRenderer))
        {
            tilemapRenderer.enabled = true;
        }
        else if (TryGetComponent<Renderer>(out var genericRenderer))
        {
            genericRenderer.enabled = true;
        }

        if (hideChildren)
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in childRenderers)
            {
                r.enabled = true;
            }
        }
    }
}
