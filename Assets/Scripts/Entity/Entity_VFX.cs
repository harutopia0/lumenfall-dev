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

    [Header("Super Dash VFX")]
    [SerializeField] private GameObject superDashTrailObj;
    [SerializeField] private Animator superDashBurstAnim;
    [SerializeField] private Animator superDashBreakAnim;
    [SerializeField] private Animator superDashCrystalAnim;
    [SerializeField] private GameObject superDashChargeObj; 
    [SerializeField] private Animator superDashBlingAnim;
    [SerializeField] private Animator superDashTrailEndAnim;

    [Header("Crystal Offset Settings")]
    [SerializeField] private Vector3 groundCrystalOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 wallCrystalOffset = new Vector3(0.2f, 0f, 0f);
    [SerializeField] private Vector3 wallCrystalRotation = new Vector3(0f, 0f, 45f);

    public void SetSuperDashCharging(bool charging, bool isWallCharge = false)
    {
        if (superDashCrystalAnim != null)
        {
            superDashCrystalAnim.gameObject.SetActive(charging);
            if (charging)
            {
                superDashCrystalAnim.transform.localPosition = isWallCharge ? wallCrystalOffset : groundCrystalOffset;
                superDashCrystalAnim.transform.localEulerAngles = isWallCharge ? wallCrystalRotation : Vector3.zero;

                superDashCrystalAnim.SetBool("charging", true);
            }
            else
            {
                superDashCrystalAnim.SetBool("charging", false);
            }
        }
        if (superDashChargeObj != null)
        {
            superDashChargeObj.SetActive(charging);
        }
    }

    public void PlaySuperDashBlingVfx()
    {
        if (superDashBlingAnim != null)
        {
            superDashBlingAnim.Play("sdFxBling", 0, 0f);
        }
    }

    public void PlaySuperDashTrailEndVfx()
    {
        if (superDashTrailEndAnim != null)
        {
            superDashTrailEndAnim.Play("sdTrailEnd", 0, 0f);
        }
    }

    public void SetSuperDashTrail(bool active)
    {
        if (superDashTrailObj != null) superDashTrailObj.SetActive(active);
    }

    public void PlaySuperDashBurstVfx(int dir)
    {
        if (superDashBurstAnim != null)
        {
            superDashBurstAnim.gameObject.SetActive(true);
            superDashBurstAnim.Play("sdFxBurst", 0, 0f);
        }
    }

    public void PlaySuperDashBreakVfx()
    {
        if (superDashBreakAnim != null)
        {
            superDashBreakAnim.gameObject.SetActive(true);
            superDashBreakAnim.Play("sdBreak", 0, 0f);
        }
    }


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
