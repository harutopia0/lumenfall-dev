using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("1. Tracking Target & Offset")]
    [Tooltip("Target transform that the camera follows.")]
    [SerializeField] private Transform target;

    [Tooltip("World-space offset applied to the target focus point.")]
    [SerializeField] private Vector2 targetOffset = new Vector2(0f, 3f);

    [Header("2. Cinemachine Damping")]
    [Tooltip("Horizontal damping time using Cinemachine exponential decay.")]
    [SerializeField] private float dampingX = 1f;

    [Tooltip("Vertical damping time for normal traversal.")]
    [SerializeField] private float dampingY = 1f;

    [Header("3. Vertical Asymmetry")]
    [Tooltip("Enables distinct vertical damping and look-ahead logic for jumping and falling.")]
    [SerializeField] private bool enableVerticalAsymmetry = true;

    [Tooltip("Vertical damping time when the target is ascending (jumping).")]
    [SerializeField] private float jumpDampingY = 1.4f;

    [Tooltip("Base vertical damping time when the target begins falling.")]
    [SerializeField] private float fallDampingY = 0.25f;

    [Tooltip("Tight vertical damping time during fast free fall to stay glued to the target.")]
    [SerializeField] private float fastFallDampingY = 0.05f;

    [Tooltip("Maximum allowed vertical distance the target can drop below camera view before hard-locking.")]
    [SerializeField] private float maxFallLagDistance = 2.5f;

    [Tooltip("Downwards look-ahead distance applied during rapid descent.")]
    [SerializeField] private float fallLeadDistance = 2.5f;

    [Tooltip("Speed at which the fall lead offset smoothly glides back to zero upon landing.")]
    [SerializeField] private float fallLeadReturnSpeed = 6f;

    [Tooltip("Downward vertical velocity threshold required to trigger falling camera behavior.")]
    [SerializeField] private float fallVelocityThreshold = -4f;

    [Header("4. Look Up & Look Down Scouting")]
    [Tooltip("Enables looking up or down by holding vertical input while standing still.")]
    [SerializeField] private bool enableScouting = true;

    [Tooltip("Vertical camera pan distance when looking up.")]
    [SerializeField] private float lookUpDistance = 3.5f;

    [Tooltip("Vertical camera pan distance when looking down.")]
    [SerializeField] private float lookDownDistance = 3.5f;

    [Tooltip("Hold duration in seconds before scouting pan begins.")]
    [SerializeField] private float scoutHoldTime = 0.5f;

    [Tooltip("Speed at which the camera pans into and returns from the scouting offset.")]
    [SerializeField] private float scoutSmoothSpeed = 4f;

    [Header("5. Room Transition Smoothing")]
    [Tooltip("Duration in seconds for smooth blending when transitioning between camera zones.")]
    [SerializeField] private float transitionDuration = 1.0f;

    [Tooltip("Animation curve defining the easing profile when blending between camera zones.")]
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("6. Trauma Shake Model")]
    [Tooltip("Maximum translational offset (X, Y) applied during maximum trauma shake.")]
    [SerializeField] private Vector2 maxShakeOffset = new Vector2(0.6f, 0.6f);

    [Tooltip("Maximum rotational angle (degrees Z) applied during maximum trauma shake.")]
    [SerializeField] private float maxShakeAngle = 1.8f;

    [Tooltip("Frequency of Perlin noise sampling for camera trauma shake.")]
    [SerializeField] private float shakeFrequency = 28f;

    [Tooltip("Rate at which trauma decays per second.")]
    [SerializeField] private float traumaDecay = 1.6f;

    [Header("7. Anti-Jitter")]
    [Tooltip("Automatically sets Rigidbody2D interpolation to Interpolate on target to eliminate physics jitter.")]
    [SerializeField] private bool autoEnableRigidbodyInterpolation = true;

    [Header("8. Camera Distance")]
    [Tooltip("Z-axis world position of the camera relative to target.")]
    [SerializeField] private float cameraDistanceZ = -10f;

    private Camera cam;
    private CameraLockArea currentArea;
    private readonly List<CameraLockArea> activeAreas = new List<CameraLockArea>();

    private Rigidbody2D targetRb;
    private Player player;

    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private Vector2 transitionStartPos;

    private float lookUpTimer = 0f;
    private float lookDownTimer = 0f;
    private float currentScoutOffsetY = 0f;
    private float currentFallLeadOffset = 0f;

    private float trauma = 0f;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target != null)
        {
            targetRb = target.GetComponentInParent<Rigidbody2D>();
            player = target.GetComponentInParent<Player>();

            if (autoEnableRigidbodyInterpolation && targetRb != null)
            {
                if (targetRb.interpolation == RigidbodyInterpolation2D.None)
                {
                    targetRb.interpolation = RigidbodyInterpolation2D.Interpolate;
                }
            }

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

        if (newArea == null)
        {
            isTransitioning = false;
        }
        else if (currentArea != null)
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

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void Shake(float intensity, float duration)
    {
        AddTrauma(Mathf.Clamp01(intensity * 0.5f));
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (activeAreas.Count > 1)
        {
            UpdateCurrentArea();
        }

        float targetScoutY = 0f;
        if (enableScouting)
        {
            Vector2 inputDir = Vector2.zero;
            bool isGrounded = true;

            if (player != null)
            {
                inputDir = player.moveInput;
                isGrounded = player.groundDetected;
            }
            else
            {
                inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                if (targetRb != null) isGrounded = Mathf.Abs(targetRb.linearVelocity.y) < 0.1f;
            }

            bool isStandingStill = isGrounded && (targetRb == null || Mathf.Abs(targetRb.linearVelocity.x) < 0.3f);

            if (isStandingStill && inputDir.y > 0.5f)
            {
                lookUpTimer += Time.deltaTime;
                lookDownTimer = 0f;
                if (lookUpTimer >= scoutHoldTime)
                {
                    targetScoutY = lookUpDistance;
                }
            }
            else if (isStandingStill && inputDir.y < -0.5f)
            {
                lookDownTimer += Time.deltaTime;
                lookUpTimer = 0f;
                if (lookDownTimer >= scoutHoldTime)
                {
                    targetScoutY = -lookDownDistance;
                }
            }
            else
            {
                lookUpTimer = 0f;
                lookDownTimer = 0f;
                targetScoutY = 0f;
            }

            currentScoutOffsetY = Mathf.MoveTowards(currentScoutOffsetY, targetScoutY, scoutSmoothSpeed * Time.deltaTime);
        }
        else
        {
            currentScoutOffsetY = 0f;
        }

        float activeDampingY = dampingY;
        float targetFallLeadOffset = 0f;

        if (enableVerticalAsymmetry && targetRb != null)
        {
            float vy = targetRb.linearVelocity.y;

            if (vy > 1.5f)
            {
                activeDampingY = jumpDampingY;
            }
            else if (vy < fallVelocityThreshold)
            {
                float fallFactor = Mathf.Clamp01((Mathf.Abs(vy) - Mathf.Abs(fallVelocityThreshold)) / 20f);
                activeDampingY = Mathf.Lerp(fallDampingY, fastFallDampingY, fallFactor);
                targetFallLeadOffset = -fallLeadDistance * fallFactor;
            }
        }

        currentFallLeadOffset = Mathf.MoveTowards(currentFallLeadOffset, targetFallLeadOffset, fallLeadReturnSpeed * Time.deltaTime);

        Vector2 desiredCameraPos = (Vector2)target.position + targetOffset + new Vector2(0f, currentScoutOffsetY + currentFallLeadOffset);

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
            float transitionSpeedMult = 1f;
            if (targetRb != null)
            {
                float vy = targetRb.linearVelocity.y;
                float vx = Mathf.Abs(targetRb.linearVelocity.x);
                if (vy < fallVelocityThreshold)
                {
                    transitionSpeedMult += Mathf.Abs(vy) * 0.08f;
                }
                if (vx > 15f)
                {
                    transitionSpeedMult += vx * 0.08f;
                }
            }

            transitionTimer += Time.deltaTime * transitionSpeedMult;
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

            float activeDampingX = dampingX;
            if (targetRb != null && Mathf.Abs(targetRb.linearVelocity.x) > 20f)
            {
                float speedX = Mathf.Abs(targetRb.linearVelocity.x);
                float dashFactor = Mathf.Clamp01((speedX - 20f) / 15f);
                activeDampingX = Mathf.Lerp(dampingX, 0.15f, dashFactor);
            }

            newX = transform.position.x + CinemachineDamp(deltaX, activeDampingX, Time.deltaTime);
            newY = transform.position.y + CinemachineDamp(deltaY, activeDampingY, Time.deltaTime);
        }

        if (enableVerticalAsymmetry && targetRb != null && targetRb.linearVelocity.y < 0f)
        {
            float targetFocusY = target.position.y + targetOffset.y;
            if (newY > targetFocusY + maxFallLagDistance)
            {
                newY = targetFocusY + maxFallLagDistance;
            }
        }

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
            float minY = currentArea.MinY + halfHeight;
            float maxY = currentArea.MaxY - halfHeight;

            if (minY <= maxY)
            {
                newY = Mathf.Clamp(newY, minY, maxY);
            }
            else
            {
                newY = (currentArea.MinY + currentArea.MaxY) * 0.5f;
            }
        }

        Vector3 shakePosOffset = Vector3.zero;
        float shakeAngleZ = 0f;

        if (trauma > 0f)
        {
            float shake = trauma * trauma;
            float noiseTime = Time.time * shakeFrequency;

            float offsetX = (Mathf.PerlinNoise(noiseTime, 1.5f) * 2f - 1f) * maxShakeOffset.x * shake;
            float offsetY = (Mathf.PerlinNoise(noiseTime, 2.5f) * 2f - 1f) * maxShakeOffset.y * shake;
            shakePosOffset = new Vector3(offsetX, offsetY, 0f);

            shakeAngleZ = (Mathf.PerlinNoise(noiseTime, 3.5f) * 2f - 1f) * maxShakeAngle * shake;

            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.deltaTime);
        }

        transform.position = new Vector3(newX, newY, cameraDistanceZ) + shakePosOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, shakeAngleZ);
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
