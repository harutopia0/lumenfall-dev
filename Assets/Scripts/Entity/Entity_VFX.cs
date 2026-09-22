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
    [SerializeField] private GameObject superDashBurstPrefab;
    [SerializeField] private Vector2 superDashBurstOffset = Vector2.zero;
    [SerializeField] private GameObject superDashTrailEndPrefab;
    [SerializeField] private Vector2 superDashTrailEndOffset = Vector2.zero;
    [SerializeField] private GameObject superDashBreakPrefab;
    [SerializeField] private Vector2 superDashBreakOffset = Vector2.zero;
    [SerializeField] private GameObject superDashChargeObj; 
    [SerializeField] private Animator superDashBlingAnim;

    private enum CornerType { DropCliff, ClimbWall }

    private struct CornerInfo
    {
        public CornerType type;
        public float distance;
        public float edgeCoordinate;
    }

    [Header("Super Dash Extreme Criss-Cross U-Arch")]
    [SerializeField] private GameObject crystalPrefab;
    [SerializeField] private int crystalCount = 20;
    [SerializeField] private float groundSpreadWidth = 5f;
    [SerializeField] private float wallSpreadHeight = 5f;
    [SerializeField] private float waveInterval = 0.018f;
    [SerializeField] private Vector2 centerScale = new Vector2(0.3f, 0.45f);
    [SerializeField] private Vector2 edgeScale = new Vector2(1.75f, 3.5f);
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
        if (vfxContainer != null) Destroy(vfxContainer.gameObject);
    }

    [ContextMenu("Apply Extreme Criss-Cross Arch Settings")]
    private void ApplyExtremeCrissCrossSettings()
    {
        crystalCount = 14;
        groundSpreadWidth = 2.4f;
        wallSpreadHeight = 2.0f;
        waveInterval = 0.018f;
        centerScale = new Vector2(0.45f, 0.4f);
        edgeScale = new Vector2(1.65f, 2.6f);
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

        Vector2 centerPoint;
        bool hasLeftCorner = false, hasRightCorner = false;
        CornerInfo leftCorner = default, rightCorner = default;

        bool hasCornerUp = false, hasCornerDown = false;
        CornerInfo cornerUp = default, cornerDown = default;

        if (!isWallCharge)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 2.5f, mask);
            if (hit.collider == null) yield break;
            centerPoint = hit.point;

            hasLeftCorner = TryFindGroundCorner(centerPoint, -1f, groundSpreadWidth * 0.5f, mask, out leftCorner);
            hasRightCorner = TryFindGroundCorner(centerPoint, 1f, groundSpreadWidth * 0.5f, mask, out rightCorner);
        }
        else
        {
            Vector2 wallDir = Vector2.right * facingDir;
            RaycastHit2D hit = Physics2D.Raycast(origin, wallDir, 2.0f, mask);
            if (hit.collider == null) yield break;
            centerPoint = hit.point;

            hasCornerUp = TryFindWallCorner(centerPoint, 1f, facingDir, wallSpreadHeight * 0.5f, mask, out cornerUp);
            hasCornerDown = TryFindWallCorner(centerPoint, -1f, facingDir, wallSpreadHeight * 0.5f, mask, out cornerDown);
        }

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

            float heightJitter = Random.Range(-0.25f, 0.25f);
            float widthJitter = Random.Range(-0.12f, 0.12f);

            float finalScaleY = Mathf.Max(0.35f, baseScaleY + heightJitter);
            float finalScaleX = Mathf.Max(0.4f, baseScaleX + widthJitter);

            bool randomFlip = Random.value > 0.5f;
            crystalAnim.transform.localScale = new Vector3(randomFlip ? -finalScaleX : finalScaleX, finalScaleY, 1f);

            float crossDirection = (i % 2 == 0) ? 1f : -1f;
            float crossAngle = crossDirection * Random.Range(12f, 24f);
            float finalAngle = (-t * 10f) + crossAngle;

            if (crystalSr != null)
            {
                bool isBehind = (i % 2 == 0);
                crystalSr.sortingOrder = isBehind ? -1 : 1;
                crystalSr.color = isBehind ? new Color(0.82f, 0.82f, 0.88f, 1f) : Color.white;
            }

            Vector3 targetPos;
            Quaternion targetRot;

            float rootJitter = Random.Range(-0.04f, 0.04f);
            float totalTargetDist = Mathf.Abs(t) * (!isWallCharge ? groundSpreadWidth : wallSpreadHeight) + rootJitter;

            if (!isWallCharge)
            {
                float sign = Mathf.Sign(t);
                if (t == 0) sign = 0;

                bool hasCorner = (t < 0) ? hasLeftCorner : hasRightCorner;
                CornerInfo corner = (t < 0) ? leftCorner : rightCorner;
                float cornerBuffer = 0.08f;

                if (!hasCorner || totalTargetDist < corner.distance - cornerBuffer)
                {
                    targetPos = new Vector3(centerPoint.x + (sign * totalTargetDist), centerPoint.y, transform.position.z);
                    targetRot = Quaternion.Euler(0, 0, finalAngle);
                }
                else if (totalTargetDist <= corner.distance + cornerBuffer)
                {
                    targetPos = new Vector3(corner.edgeCoordinate, centerPoint.y, transform.position.z);
                    Vector2 cornerNormal = (corner.type == CornerType.DropCliff)
                        ? (Vector2.up + (Vector2.right * sign)).normalized
                        : (Vector2.up - (Vector2.right * sign)).normalized;
                    targetRot = Quaternion.FromToRotation(Vector2.up, cornerNormal) * Quaternion.Euler(0, 0, finalAngle);
                }
                else
                {
                    float overflow = totalTargetDist - corner.distance;

                    if (corner.type == CornerType.DropCliff)
                    {
                        targetPos = new Vector3(corner.edgeCoordinate, centerPoint.y - overflow, transform.position.z);
                        Vector2 cliffNormal = Vector2.right * sign;
                        targetRot = Quaternion.FromToRotation(Vector2.up, cliffNormal) * Quaternion.Euler(0, 0, finalAngle);
                    }
                    else
                    {
                        targetPos = new Vector3(corner.edgeCoordinate, centerPoint.y + overflow, transform.position.z);
                        Vector2 wallNormal = -Vector2.right * sign;
                        targetRot = Quaternion.FromToRotation(Vector2.up, wallNormal) * Quaternion.Euler(0, 0, finalAngle);
                    }
                }
            }
            else
            {
                float sign = Mathf.Sign(t);
                if (t == 0) sign = 0;
                Vector2 wallOutNormal = -Vector2.right * facingDir;
                float cornerBuffer = 0.08f;

                bool hasCorner = (t > 0) ? hasCornerUp : hasCornerDown;
                CornerInfo corner = (t > 0) ? cornerUp : cornerDown;

                if (!hasCorner || totalTargetDist < corner.distance - cornerBuffer)
                {
                    targetPos = new Vector3(centerPoint.x, centerPoint.y + (sign * totalTargetDist), transform.position.z);
                    targetRot = Quaternion.FromToRotation(Vector2.up, wallOutNormal) * Quaternion.Euler(0, 0, finalAngle);
                }
                else if (totalTargetDist <= corner.distance + cornerBuffer)
                {
                    targetPos = new Vector3(centerPoint.x, corner.edgeCoordinate, transform.position.z);
                    Vector2 cornerNormal = (wallOutNormal + Vector2.up).normalized;
                    targetRot = Quaternion.FromToRotation(Vector2.up, cornerNormal) * Quaternion.Euler(0, 0, finalAngle);
                }
                else
                {
                    float overflow = totalTargetDist - corner.distance;

                    if (t < 0)
                    {
                        targetPos = new Vector3(centerPoint.x - (facingDir * overflow), corner.edgeCoordinate, transform.position.z);
                    }
                    else
                    {
                        targetPos = new Vector3(centerPoint.x + (facingDir * overflow), corner.edgeCoordinate, transform.position.z);
                    }

                    targetRot = Quaternion.Euler(0, 0, finalAngle);
                }
            }

            crystalAnim.transform.position = targetPos;
            crystalAnim.transform.rotation = targetRot;
            crystalAnim.SetBool("charging", true);
        }
    }

    private bool TryFindGroundCorner(Vector2 startPos, float dirX, float maxRange, LayerMask mask, out CornerInfo corner)
    {
        corner = default;
        float step = 0.08f;
        float currentDist = 0f;

        while (currentDist < maxRange)
        {
            float nextDist = currentDist + step;
            float checkX = startPos.x + (dirX * nextDist);

            RaycastHit2D wallCheck = Physics2D.Raycast(new Vector2(startPos.x + (dirX * currentDist), startPos.y + 0.2f), Vector2.right * dirX, step + 0.05f, mask);
            if (wallCheck.collider != null)
            {
                corner.type = CornerType.ClimbWall;
                corner.distance = currentDist;
                corner.edgeCoordinate = wallCheck.point.x;
                return true;
            }

            RaycastHit2D floorCheck = Physics2D.Raycast(new Vector2(checkX, startPos.y + 0.4f), Vector2.down, 0.8f, mask);
            if (floorCheck.collider == null || Mathf.Abs(floorCheck.point.y - startPos.y) > 0.35f)
            {
                corner.type = CornerType.DropCliff;
                corner.distance = currentDist;
                corner.edgeCoordinate = startPos.x + (dirX * currentDist);
                return true;
            }

            currentDist += step;
        }

        return false;
    }

    private bool TryFindWallCorner(Vector2 startPos, float dirY, int facingDir, float maxRange, LayerMask mask, out CornerInfo corner)
    {
        corner = default;
        Vector2 wallDir = Vector2.right * facingDir;

        if (dirY < 0)
        {
            RaycastHit2D floorHit = Physics2D.Raycast(new Vector2(startPos.x - facingDir * 0.15f, startPos.y), Vector2.down, maxRange, mask);
            if (floorHit.collider != null)
            {
                corner.type = CornerType.ClimbWall;
                corner.distance = startPos.y - floorHit.point.y;
                corner.edgeCoordinate = floorHit.point.y;
                return true;
            }
        }
        else
        {
            float step = 0.08f;
            float currentDist = 0f;
            while (currentDist < maxRange)
            {
                float checkY = startPos.y + (currentDist + step);
                RaycastHit2D wallCheck = Physics2D.Raycast(new Vector2(startPos.x - facingDir * 0.2f, checkY), wallDir, 0.5f, mask);

                if (wallCheck.collider == null)
                {
                    corner.type = CornerType.DropCliff;
                    corner.distance = currentDist;
                    corner.edgeCoordinate = checkY - step * 0.5f;
                    return true;
                }
                currentDist += step;
            }
        }

        return false;
    }


    public void PlaySuperDashBlingVfx()
    {
        if (superDashBlingAnim != null)
        {
            superDashBlingAnim.Play("sdFxBling", 0, 0f);
        }
    }

    public void PlaySuperDashTrailEndVfx(Vector3 position, Quaternion rotation, int facingDir)
    {
        if (superDashTrailEndPrefab == null) return;
        Vector3 spawnPos = position + new Vector3(superDashTrailEndOffset.x * facingDir, superDashTrailEndOffset.y, 0f);
        Instantiate(superDashTrailEndPrefab, spawnPos, rotation);
    }

    public void SetSuperDashTrail(bool active)
    {
        if (superDashTrailObj != null) superDashTrailObj.SetActive(active);
    }

    public void PlaySuperDashBurstVfx(Vector3 position, Quaternion rotation, int facingDir)
    {
        if (superDashBurstPrefab == null) return;

        Vector3 spawnPos = position + new Vector3(superDashBurstOffset.x * facingDir, superDashBurstOffset.y, 0f);
        Instantiate(superDashBurstPrefab, spawnPos, rotation);
    }

    public void PlaySuperDashBreakVfx(Vector3 position, Quaternion rotation, int facingDir)
    {
        if (superDashBreakPrefab == null) return;
        Vector3 spawnPos = position + new Vector3(superDashBreakOffset.x * facingDir, superDashBreakOffset.y, 0f);
        Instantiate(superDashBreakPrefab, spawnPos, rotation);
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
