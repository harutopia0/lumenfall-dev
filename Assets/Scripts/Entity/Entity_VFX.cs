using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Entity_VFX : MonoBehaviour
{
    private SpriteRenderer sr;
    private Entity_Combat combat;

    [Header("On Damage VFX")]
    [SerializeField] private Material onDamageMaterial;
    [SerializeField] private float onDamageVfxDuration = 0.15f;
    private Material originalMaterial;
    private Coroutine onDamageVfxCoroutine;

    [Header("Movement VFX")]
    [SerializeField] private GameObject dashVfxPrefab;
    [SerializeField] private Vector2 dashVfxOffset = new Vector2(0.6f, 0f);

    [Header("Slash VFX")]
    [SerializeField] private Animator slashVfxAnim;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        combat = GetComponent<Entity_Combat>();
        originalMaterial = sr.material;

        if (onDamageMaterial != null)
        {
            onDamageMaterial = new Material(onDamageMaterial);
        }
    }

    public void PlaySlashVfx(Entity_Combat.AttackDirection dir)
    {
        if (slashVfxAnim == null || combat == null) return;

        Transform currentCheck = combat.GetCurrentCheckTransform();
        if (currentCheck != null)
        {
            slashVfxAnim.transform.localPosition = currentCheck.localPosition;
        }

        switch (dir)
        {
            case Entity_Combat.AttackDirection.Side:
                slashVfxAnim.Play("slashEffect", 0, 0f);
                break;
            case Entity_Combat.AttackDirection.Up:
                slashVfxAnim.Play("upSlashEffect", 0, 0f);
                break;
            case Entity_Combat.AttackDirection.Down:
                slashVfxAnim.Play("downSlashEffect", 0, 0f);
                break;
        }
    }

    public void PlayDashVfx(Vector3 position, Quaternion rotation, int facingDir)
    {
        if (dashVfxPrefab == null) return;

        Vector3 spawnPos = position + new Vector3(dashVfxOffset.x * facingDir, dashVfxOffset.y, 0f);
        Instantiate(dashVfxPrefab, spawnPos, rotation);
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
