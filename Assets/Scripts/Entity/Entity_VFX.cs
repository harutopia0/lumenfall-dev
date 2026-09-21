using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Entity_VFX : MonoBehaviour
{
    private SpriteRenderer sr;
    private Entity entity;
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

    [Header("Super Dash Extreme Criss-Cross U-Arch")]
    [SerializeField] private GameObject crystalPrefab;
    [SerializeField] private int crystalCount = 20;
    [SerializeField] private float groundSpreadWidth = 2.4f;
    [SerializeField] private float wallSpreadHeight = 2.0f;
    [SerializeField] private float waveInterval = 0.018f;
    [SerializeField] private Vector2 centerScale = new Vector2(0.45f, 0.4f);
    [SerializeField] private Vector2 edgeScale = new Vector2(1.65f, 2.6f);
    [SerializeField] private LayerMask surfaceLayer;

    private List<Animator> crystalPool = new List<Animator>();
    private List<SpriteRenderer> crystalRenderers = new List<SpriteRenderer>();
    private Coroutine spawnWaveCoroutine;
    private Coroutine disableCrystalsCoroutine;
    private Transform vfxContainer;

    private void InitializeCrystalPool()
    {
        if (crystalPrefab == null) return;
        if (vfxContainer == null)
        {
            GameObject containerObj = new GameObject("_SuperDash_Crystal_Container");
            vfxContainer = containerObj.transform;
        }
        for (int i = 0; i < crystalCount; i++)
        {
            GameObject obj = Instantiate(crystalPrefab, transform.position, Quaternion.identity, vfxContainer);
            obj.SetActive(false);
            crystalPool.Add(obj.GetComponent<Animator>());
            crystalRenderers.Add(obj.GetComponent<SpriteRenderer>());
        }
    }
    private void OnDestroy()
    {
        if (vfxContainer != null)
        {
            Destroy(vfxContainer.gameObject);
        }
    }

    public void SetSuperDashCharging(bool charging, bool isWallCharge = false)
    {
        if (charging)
        {
            if (disableCrystalsCoroutine != null) StopCoroutine(disableCrystalsCoroutine);
            if (spawnWaveCoroutine != null) StopCoroutine(spawnWaveCoroutine);

            spawnWaveCoroutine = StartCoroutine(SpawnCrissCrossUArchWaveRoutine(isWallCharge));
        }
        else
        {
            if (spawnWaveCoroutine != null) StopCoroutine(spawnWaveCoroutine);

            foreach (var anim in crystalPool)
            {
                if (anim.gameObject.activeSelf)
                {
                    anim.SetBool("charging", false);
                }
            }

            if (disableCrystalsCoroutine != null) StopCoroutine(disableCrystalsCoroutine);
            disableCrystalsCoroutine = StartCoroutine(DisableCrystalsAfterShrink(0.25f));
        }

        if (superDashChargeObj != null)
        {
            superDashChargeObj.SetActive(charging);
        }
    }

    private IEnumerator DisableCrystalsAfterShrink(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var anim in crystalPool)
        {
            anim.gameObject.SetActive(false);
        }
    }

    private IEnumerator SpawnCrissCrossUArchWaveRoutine(bool isWallCharge)
    {
        if (crystalPool.Count == 0) InitializeCrystalPool();

        LayerMask mask = surfaceLayer.value != 0 ? surfaceLayer : LayerMask.GetMask("Ground");
        Vector2 origin = transform.position;

        int facingDir = entity != null ? entity.facingDir : (transform.eulerAngles.y > 90f ? -1 : 1);

        List<int> sortedIndices = new List<int>();
        for (int i = 0; i < crystalPool.Count; i++) sortedIndices.Add(i);

        int midIndex = crystalPool.Count / 2;
        sortedIndices.Sort((a, b) => Mathf.Abs(a - midIndex).CompareTo(Mathf.Abs(b - midIndex)));

        int currentStep = 0;

        foreach (int i in sortedIndices)
        {
            int distFromCenter = Mathf.Abs(i - midIndex);

            if (distFromCenter > currentStep)
            {
                yield return new WaitForSeconds(waveInterval);
                currentStep = distFromCenter;
            }

            float t = crystalPool.Count > 1 ? ((float)i / (crystalPool.Count - 1)) - 0.5f : 0f;
            Animator crystalAnim = crystalPool[i];
            SpriteRenderer crystalSr = crystalRenderers[i];
            crystalAnim.gameObject.SetActive(true);

            float normalizedDist = Mathf.Abs(t) * 2f;
            float arcCurve = Mathf.Pow(normalizedDist, 1.5f);

            float baseScaleY = Mathf.Lerp(centerScale.y, edgeScale.y, arcCurve);
            float baseScaleX = Mathf.Lerp(centerScale.x, edgeScale.x, arcCurve);

            float heightJitter = Random.Range(-0.5f, 0.5f);
            float widthJitter = Random.Range(-0.12f, 0.12f);

            float finalScaleY = Mathf.Max(0.35f, baseScaleY + heightJitter);
            float finalScaleX = Mathf.Max(0.4f, baseScaleX + widthJitter);

            bool randomFlip = Random.value > 0.5f;
            crystalAnim.transform.localScale = new Vector3(randomFlip ? -finalScaleX : finalScaleX, finalScaleY, 1f);

            float crossDirection = (i % 2 == 0) ? 1f : -1f;
            float crossAngle = crossDirection * Random.Range(15f, 45f);
            float finalAngle = (-t * 15f) + crossAngle;

            if (crystalSr != null)
            {
                bool isBehind = (i % 2 == 0);
                crystalSr.sortingOrder = isBehind ? -1 : 1;
                crystalSr.color = isBehind ? new Color(0.82f, 0.82f, 0.88f, 1f) : Color.white;
            }

            Vector3 targetPos;
            Quaternion targetRot;

            if (!isWallCharge)
            {
                float rootJitterX = Random.Range(-0.08f, 0.08f);
                RaycastHit2D centerHit = Physics2D.Raycast(origin, Vector2.down, 2.5f, mask);
                Collider2D groundCol = centerHit.collider;

                Vector2 rayStart = origin + new Vector2(t * groundSpreadWidth + rootJitterX, 0.5f);
                RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 2.5f, mask);

                if (hit.collider != null)
                {
                    targetPos = new Vector3(hit.point.x, hit.point.y, transform.position.z);
                    targetRot = Quaternion.FromToRotation(Vector2.up, hit.normal) * Quaternion.Euler(0, 0, finalAngle);
                }
                else if (groundCol != null)
                {
                    Vector2 edgePoint = groundCol.ClosestPoint(rayStart);
                    targetPos = new Vector3(edgePoint.x, edgePoint.y, transform.position.z);
                    targetRot = Quaternion.Euler(0, 0, (t > 0 ? 30f : -30f) + finalAngle);
                }
                else
                {
                    targetPos = new Vector3(rayStart.x, origin.y - 0.5f, transform.position.z);
                    targetRot = Quaternion.Euler(0, 0, finalAngle);
                }
            }
            else
            {
                float rootJitterY = Random.Range(-0.08f, 0.08f);
                Vector2 wallRayDir = Vector2.right * facingDir;
                Vector2 wallOutNormal = -wallRayDir;

                RaycastHit2D centerHit = Physics2D.Raycast(origin, wallRayDir, 2.0f, mask);
                Collider2D wallCol = centerHit.collider;

                Vector2 rayStart = origin + new Vector2(0f, t * wallSpreadHeight + rootJitterY);
                RaycastHit2D hit = Physics2D.Raycast(rayStart, wallRayDir, 2.0f, mask);

                if (hit.collider != null)
                {
                    targetPos = new Vector3(hit.point.x, hit.point.y, transform.position.z);
                    targetRot = Quaternion.FromToRotation(Vector2.up, hit.normal) * Quaternion.Euler(0, 0, finalAngle);
                }
                else if (wallCol != null)
                {
                    Vector2 edgePoint = wallCol.ClosestPoint(rayStart);
                    targetPos = new Vector3(edgePoint.x, edgePoint.y, transform.position.z);
                    targetRot = Quaternion.FromToRotation(Vector2.up, wallOutNormal) * Quaternion.Euler(0, 0, finalAngle);
                }
                else
                {
                    targetPos = new Vector3(origin.x + facingDir * 0.3f, rayStart.y, transform.position.z);
                    targetRot = Quaternion.FromToRotation(Vector2.up, wallOutNormal) * Quaternion.Euler(0, 0, finalAngle);
                }
            }

            crystalAnim.transform.position = targetPos;
            crystalAnim.transform.rotation = targetRot;
            crystalAnim.SetBool("charging", true);
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
        entity = GetComponent<Entity>();
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
