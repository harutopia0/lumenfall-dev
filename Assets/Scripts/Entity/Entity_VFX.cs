using System.Collections;
using UnityEngine;

public class Entity_VFX : MonoBehaviour
{
    private SpriteRenderer sr;

    [Header("On Damage VFX")]
    [SerializeField] private Material onDamageMaterial;
    [SerializeField] private float onDamageVfxDuration = 0.15f;
    private Material originalMaterial;
    private Coroutine onDamageVfxCoroutine;

    [Header("Movement VFX")]
    [SerializeField] private GameObject dashVfxPrefab;
    [SerializeField] private Vector2 dashVfxOffset = new Vector2(0.6f, 0f);

    public void PlayDashVfx(Vector3 position, Quaternion rotation, int facingDir)
    {
        if (dashVfxPrefab == null) return;

        Vector3 spawnPos = position + new Vector3(dashVfxOffset.x * facingDir, dashVfxOffset.y, 0f);
        Instantiate(dashVfxPrefab, spawnPos, rotation);
    }


    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        originalMaterial = sr.material;

        if (onDamageMaterial != null)
        {
            onDamageMaterial = new Material(onDamageMaterial);
        }
    }

    public void PlayOnDamageVfx()
    {
        if (onDamageVfxCoroutine != null)
        {
            StopCoroutine(onDamageVfxCoroutine);
        }

        onDamageVfxCoroutine = StartCoroutine(OnDamageVfxCo());
    }

    private IEnumerator OnDamageVfxCo()
    {
        sr.material = onDamageMaterial;

        yield return new WaitForSeconds(onDamageVfxDuration);

        sr.material = originalMaterial;
    }
}
