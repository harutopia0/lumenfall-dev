using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("1. Tracking Target & Offset")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 targetOffset = new Vector2(0f, 2f);

    [Header("2. Cinemachine Damping")]
    [SerializeField] private float dampingX = 1f;
    [SerializeField] private float dampingY = 1f;

    [Header("3. Room Transition Smoothing")]
    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("4. Camera Distance (Z - Perspective)")]
    [SerializeField] private float cameraDistanceZ = -10f;

    private Camera cam;
    private CameraLockArea currentArea;
    private readonly List<CameraLockArea> activeAreas = new List<CameraLockArea>();

    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private Vector2 transitionStartPos;

    private Vector3 shakeOffset = Vector3.zero;
    private float shakeTimeRemaining = 0f;
    private float shakeIntensity = 0f;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target != null)
        {
            CameraLockArea[] areas = FindObjectsByType<CameraLockArea>();
            foreach (var area in areas)
            {
                if (target.position.x >= area.MinX && target.position.x <= area.MaxX &&
                    target.position.y >= area.MinY && target.position.y <= area.MaxY)
                {
                    RegisterArea(area);
                }
            }
        }
    }

    public void RegisterArea(CameraLockArea area)
    {
        if (!activeAreas.Contains(area))
        {
            activeAreas.Add(area);
        }
        UpdateCurrentArea();
    }

    public void UnregisterArea(CameraLockArea area)
    {
        if (activeAreas.Contains(area))
        {
            activeAreas.Remove(area);
        }
        UpdateCurrentArea();
    }

    private void ChangeArea(CameraLockArea newArea)
    {
        if (newArea == currentArea) return;

        if (currentArea != null && newArea != null)
        {
            isTransitioning = true;
            transitionTimer = 0f;
            transitionStartPos = transform.position;
        }

        currentArea = newArea;
    }

    private void UpdateCurrentArea()
    {
        if (activeAreas.Count == 0)
        {
            ChangeArea(null);
            return;
        }

        if (activeAreas.Count == 1)
        {
            ChangeArea(activeAreas[0]);
            return;
        }

        CameraLockArea areaA = activeAreas[0];
        CameraLockArea areaB = activeAreas[1];

        float overlapMinX = Mathf.Max(areaA.MinX, areaB.MinX);
        float overlapMaxX = Mathf.Min(areaA.MaxX, areaB.MaxX);
        float overlapWidth = overlapMaxX - overlapMinX;

        float overlapMinY = Mathf.Max(areaA.MinY, areaB.MinY);
        float overlapMaxY = Mathf.Min(areaA.MaxY, areaB.MaxY);
        float overlapHeight = overlapMaxY - overlapMinY;

        if (overlapWidth <= overlapHeight)
        {
            float seamX = (overlapMinX + overlapMaxX) * 0.5f;
            if (target.position.x < seamX)
                ChangeArea((areaA.MinX < areaB.MinX) ? areaA : areaB);
            else
                ChangeArea((areaA.MinX < areaB.MinX) ? areaB : areaA);
        }
        else
        {
            float seamY = (overlapMinY + overlapMaxY) * 0.5f;
            if (target.position.y < seamY)
                ChangeArea((areaA.MinY < areaB.MinY) ? areaA : areaB);
            else
                ChangeArea((areaA.MinY < areaB.MinY) ? areaB : areaA);
        }
    }

    public void Shake(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeTimeRemaining = duration;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (activeAreas.Count > 1)
        {
            UpdateCurrentArea();
        }

        Vector2 desiredCameraPos = (Vector2)target.position + targetOffset;

        if (currentArea != null)
        {
            float halfHeight;
            if (cam.orthographic)
            {
                halfHeight = cam.orthographicSize;
            }
            else
            {
                float distance = Mathf.Abs(cameraDistanceZ - target.position.z);
                halfHeight = distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            }
            float halfWidth = halfHeight * cam.aspect;

            float minX = currentArea.MinX + halfWidth;
            float maxX = currentArea.MaxX - halfWidth;
            float minY = currentArea.MinY + halfHeight;
            float maxY = currentArea.MaxY - halfHeight;

            float clampedX = (minX > maxX) ? (currentArea.MinX + currentArea.MaxX) * 0.5f : Mathf.Clamp(desiredCameraPos.x, minX, maxX);
            float clampedY = (minY > maxY) ? (currentArea.MinY + currentArea.MaxY) * 0.5f : Mathf.Clamp(desiredCameraPos.y, minY, maxY);

            desiredCameraPos = new Vector2(clampedX, clampedY);
        }

        float newX;
        float newY;

        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            float curveT = (transitionCurve != null && transitionCurve.length > 0)
                ? transitionCurve.Evaluate(t)
                : Mathf.SmoothStep(0f, 1f, t);

            newX = Mathf.Lerp(transitionStartPos.x, desiredCameraPos.x, curveT);
            newY = Mathf.Lerp(transitionStartPos.y, desiredCameraPos.y, curveT);

            if (t >= 1f)
            {
                isTransitioning = false;
            }
        }
        else
        {
            float deltaX = desiredCameraPos.x - transform.position.x;
            float deltaY = desiredCameraPos.y - transform.position.y;

            newX = transform.position.x + CinemachineDamp(deltaX, dampingX, Time.deltaTime);
            newY = transform.position.y + CinemachineDamp(deltaY, dampingY, Time.deltaTime);
        }

        if (shakeTimeRemaining > 0f)
        {
            shakeOffset = Random.insideUnitSphere * shakeIntensity;
            shakeTimeRemaining -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        transform.position = new Vector3(newX, newY, cameraDistanceZ) + shakeOffset;
    }

    private float CinemachineDamp(float initial, float dampTime, float deltaTime)
    {
        if (dampTime < 0.0001f || Mathf.Abs(initial) < 0.0001f)
            return initial;
        if (deltaTime < 0.0001f)
            return 0f;

        float k = 4.60517f / dampTime;
        return initial * (1.0f - Mathf.Exp(-k * deltaTime));
    }
}
