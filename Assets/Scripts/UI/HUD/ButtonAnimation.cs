using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour
{
    [SerializeField] Image image;
    Tween buttonScale;
    public void ScaleButton()
    {
        buttonScale.Kill();
        image.rectTransform.localScale = Vector3.one;

        image.rectTransform.DOScale(new Vector3(1.1f, 1.1f, 1), 0.2f);
    }

    public void ResetButton()
    {
        image.rectTransform.DOScale(new Vector3(1f, 1f, 1), 0.2f);
    }

    public void PressButton()
    {
        buttonScale.Kill();
        image.rectTransform.localScale =new Vector3(1.1f, 1.1f, 1);
        image.rectTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.2f,1);
    }
}
