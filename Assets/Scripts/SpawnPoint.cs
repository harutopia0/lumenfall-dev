using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Gizmos")]
    [Tooltip("Toggles rendering of spawn point gizmos in the Scene view.")]
    [SerializeField] private bool showGizmos = true;

    [Tooltip("Color and opacity of the character silhouette gizmo.")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 0.35f);

    [Header("Player Silhouette")]
    [Tooltip("Sprite used to render the player silhouette shape.")]
    [SerializeField] private Sprite playerSprite;

    [Tooltip("Scale multiplier matching the Player visual transform scale.")]
    [SerializeField] private Vector2 visualScale = new Vector2(1.25f, 1.25f);

    [Tooltip("Offset relative to spawn position to match character visual anchor.")]
    [SerializeField] private Vector2 visualOffset = Vector2.zero;

    private Mesh previewMesh;
    private Material silhouetteMaterial;
    private Sprite lastSprite;

#if UNITY_EDITOR
    private void Reset()
    {
        AutoAssignPlayerSprite();
    }

    private void OnValidate()
    {
        if (playerSprite == null)
        {
            AutoAssignPlayerSprite();
        }
        RebuildMesh();
    }

    private void AutoAssignPlayerSprite()
    {
        if (playerSprite != null) return;

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab Player");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in renderers)
                {
                    if (sr.sprite != null)
                    {
                        playerSprite = sr.sprite;
                        return;
                    }
                }
            }
        }
    }
#endif

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

#if UNITY_EDITOR
        if (playerSprite == null)
        {
            AutoAssignPlayerSprite();
        }
#endif

        EnsureResources();

        if (playerSprite != null && previewMesh != null && silhouetteMaterial != null)
        {
            silhouetteMaterial.mainTexture = playerSprite.texture;
            silhouetteMaterial.SetColor("_Color", gizmoColor);

            Vector3 renderPos = transform.position + (Vector3)visualOffset;
            Vector3 renderScale = new Vector3(visualScale.x, visualScale.y, 1f);
            Matrix4x4 matrix = Matrix4x4.TRS(renderPos, transform.rotation, renderScale);

            for (int i = 0; i < silhouetteMaterial.passCount; i++)
            {
                if (silhouetteMaterial.SetPass(i))
                {
                    Graphics.DrawMeshNow(previewMesh, matrix);
                }
            }
        }

        // Ground pivot marker
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.08f);
    }

    private void EnsureResources()
    {
        if (silhouetteMaterial == null)
        {
            Shader shader = Shader.Find("Custom/SpriteSilhouette");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            silhouetteMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        if (playerSprite != null && (previewMesh == null || lastSprite != playerSprite))
        {
            RebuildMesh();
        }
    }

    private void RebuildMesh()
    {
        if (playerSprite == null) return;

        lastSprite = playerSprite;

        Rect rect = playerSprite.rect;
        Texture2D tex = playerSprite.texture;
        if (tex == null) return;

        float ppu = playerSprite.pixelsPerUnit;
        float w = rect.width / ppu;
        float h = rect.height / ppu;

        Vector2 pivot = playerSprite.pivot;
        float px = pivot.x / rect.width;
        float py = pivot.y / rect.height;

        float minX = -px * w;
        float maxX = (1f - px) * w;
        float minY = -py * h;
        float maxY = (1f - py) * h;

        float uMin = rect.xMin / tex.width;
        float uMax = rect.xMax / tex.width;
        float vMin = rect.yMin / tex.height;
        float vMax = rect.yMax / tex.height;

        if (previewMesh == null)
        {
            previewMesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
        }

        previewMesh.Clear();
        previewMesh.vertices = new Vector3[]
        {
            new Vector3(minX, minY, 0f),
            new Vector3(maxX, minY, 0f),
            new Vector3(maxX, maxY, 0f),
            new Vector3(minX, maxY, 0f)
        };

        previewMesh.uv = new Vector2[]
        {
            new Vector2(uMin, vMin),
            new Vector2(uMax, vMin),
            new Vector2(uMax, vMax),
            new Vector2(uMin, vMax)
        };

        previewMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        previewMesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (previewMesh != null) DestroyImmediate(previewMesh);
        if (silhouetteMaterial != null) DestroyImmediate(silhouetteMaterial);
    }
}
