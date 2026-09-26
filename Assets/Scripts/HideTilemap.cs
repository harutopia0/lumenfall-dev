using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class HideTilemap : MonoBehaviour
{
    [Tooltip("Nếu bật, sẽ ẩn cả Renderer của các GameObject con (nếu có)")]
    [SerializeField] private bool hideChildren = false;

    private void Awake()
    {
        Hide();
    }

    public void Hide()
    {
        // Ẩn TilemapRenderer nếu có trên GameObject này
        if (TryGetComponent<TilemapRenderer>(out var tilemapRenderer))
        {
            tilemapRenderer.enabled = false;
        }
        // Hỗ trợ Renderer chung (nếu gắn vào SpriteRenderer hoặc MeshRenderer)
        else if (TryGetComponent<Renderer>(out var genericRenderer))
        {
            genericRenderer.enabled = false;
        }

        // Ẩn thêm các Renderer con nếu tùy chọn được bật
        if (hideChildren)
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in childRenderers)
            {
                r.enabled = false;
            }
        }
    }
}
