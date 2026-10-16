using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class Mask : MonoBehaviour
{
    public Image idleMask;
    public VisualEffect maskBreak;
    [SerializeField] public VisualEffect maskIncrease;
    [SerializeField] public VisualEffect maskShakePrt;

    private Tween maskShake;
    private bool isShaking = false;
    private Tween maskTween;

    public void ChangeMask(bool _enable, int _health)
    {
        if (_enable)
        {
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
            if (maskBreak != null)
            {
                maskBreak.Play();
            }

            if (idleMask != null)
            {
                idleMask.enabled = false;
            }
        }
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
