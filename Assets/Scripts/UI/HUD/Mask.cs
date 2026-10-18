using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class Mask : MonoBehaviour
{
    public Image idleMask;
    [SerializeField] public Image emptyMask;
    [SerializeField] public VisualEffect maskIncrease;
    [SerializeField] public VisualEffect maskShakePrt;

    [Header("Sprite Animations")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private float idleFps = 30f;
    [SerializeField] public Image breakMask;
    [SerializeField] private Sprite[] breakFrames;
    [SerializeField] private float breakFps = 30f;

    private Tween maskShake;
    private bool isShaking = false;
    private Tween maskTween;
    private Coroutine breakCoroutine;

    private void Awake()
    {
        if (emptyMask == null)
        {
            Transform back = transform.Find("Back");
            if (back != null) emptyMask = back.GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (idleMask != null && idleMask.enabled && idleFrames != null && idleFrames.Length > 0)
        {
            int frameIndex = (int)(Time.time * idleFps) % idleFrames.Length;
            idleMask.sprite = idleFrames[frameIndex];
        }
    }

    public void ChangeMask(bool _enable, int _health)
    {
        if (_enable)
        {
            if (breakCoroutine != null)
            {
                StopCoroutine(breakCoroutine);
                breakCoroutine = null;
            }
            if (breakMask != null)
            {
                breakMask.enabled = false;
            }
            if (emptyMask != null)
            {
                emptyMask.enabled = true;
            }

            if (idleMask != null)
            {
                idleMask.enabled = true;
                idleMask.transform.localScale = new Vector3(0f, 0.6f, 1f);
                if (maskShake == null)
                {
                    maskTween = idleMask.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBounce).SetAutoKill(false);
                }
                else
                {
                    maskTween?.Restart();
                }
            }

            if (maskIncrease != null)
            {
                maskIncrease.Play();
            }
        }
        else
        {
            if (idleMask != null)
            {
                idleMask.enabled = false;
            }

            if (breakMask != null && breakFrames != null && breakFrames.Length > 0)
            {
                if (breakCoroutine != null) StopCoroutine(breakCoroutine);
                breakCoroutine = StartCoroutine(PlayBreakAnimation());
            }
            else
            {
                if (emptyMask != null) emptyMask.enabled = true;
            }
        }
    }

    private IEnumerator PlayBreakAnimation()
    {
        if (emptyMask != null) emptyMask.enabled = false;
        breakMask.enabled = true;
        float frameDuration = 1f / Mathf.Max(1f, breakFps);
        for (int i = 0; i < breakFrames.Length; i++)
        {
            breakMask.sprite = breakFrames[i];
            yield return new WaitForSeconds(frameDuration);
        }
        breakMask.enabled = false;
        if (emptyMask != null) emptyMask.enabled = true;
        breakCoroutine = null;
    }

    private void OnDisable()
    {
        if (breakCoroutine != null)
        {
            StopCoroutine(breakCoroutine);
            breakCoroutine = null;
        }
        if (emptyMask != null) emptyMask.enabled = true;
        if (breakMask != null) breakMask.enabled = false;
    }

    private void ResetPosition()
    {
        if (idleMask != null)
        {
            idleMask.rectTransform.localPosition = Vector3.zero;
        }
    }

    public void ShakeMask()
    {
        if (idleMask == null || isShaking) return;
        isShaking = true;

        if (maskShakePrt != null)
        {
            maskShakePrt.Play();
        }

        maskShake?.Kill();
        idleMask.rectTransform.localPosition = Vector3.zero;
        maskShake = idleMask.rectTransform.DOShakePosition(3f, 5f, 10, 150f, true, false).SetLoops(-1, LoopType.Incremental);
    }

    public void StopShaking()
    {
        if (isShaking)
        {
            if (maskShakePrt != null)
            {
                maskShakePrt.Stop();
            }
            isShaking = false;
            maskShake?.Kill();
            ResetPosition();
        }
    }
}
