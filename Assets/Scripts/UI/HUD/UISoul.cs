using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class UISoul : MonoBehaviour
{
    [SerializeField] SoulParticles soulParticles;
    [SerializeField] Image eyes;
    [SerializeField] Material soulMaterial;
    bool isChanging = false;
    [SerializeField] float newFill;
    [SerializeField] float currentFill;
    [SerializeField] VisualEffect bubbles;
    [SerializeField] VisualEffect soulIncrease;
    [SerializeField] float waveLength = 1f;
    [SerializeField] float glowStrenght = 0f;
    [SerializeField] float eyeEdgeStrenght = 1f;
    [SerializeField] float glowSpeed = 0.2f;
    [SerializeField] float glowPulsingSpeed = 0.3f;
    [SerializeField] float waveLenghtSpeed = 0.1f;
    [Range(0, 1)][SerializeField] float eyeAppearThreshold = 0.33f;
    [Range(0, 1)][SerializeField] float transparencyThreshold;

    Sequence waveAnimation;
    Sequence glowAnimation;
    Sequence pulseAnimation;
    Sequence eyeAnimation;
    float maxSoul = 99f;
    float currentSoul = 0f;

    private void Start()
    {
        UIManager.updateSoul.AddListener(UpdateSoulFill);
        UIManager.updateSoulAnimation.AddListener(LenghtenWaves);
        UIManager.updateSoulAnimation.AddListener(GlowPulse);

        if (soulMaterial != null)
        {
            currentFill = soulMaterial.GetFloat("_fillAmount");
            soulMaterial.SetFloat("_fillAmount", 1f);
        }

        waveAnimation = DOTween.Sequence();
        glowAnimation = DOTween.Sequence();
        pulseAnimation = DOTween.Sequence();
        eyeAnimation = DOTween.Sequence();
        GlowPulsing();
    }

    private void OnDisable()
    {
        UIManager.updateSoul.RemoveListener(UpdateSoulFill);
        UIManager.updateSoulAnimation.RemoveListener(LenghtenWaves);
        UIManager.updateSoulAnimation.RemoveListener(GlowPulse);

        waveAnimation?.Kill();
        glowAnimation?.Kill();
        pulseAnimation?.Kill();
        eyeAnimation?.Kill();
    }

    private void FixedUpdate()
    {
        if (soulMaterial != null)
        {
            UpdateMaterial("_waveMultiplier", waveLength, soulMaterial);
            UpdateMaterial("_glowMultiplier", glowStrenght, soulMaterial);
        }

        if (eyes != null && eyes.material != null)
        {
            UpdateMaterial("_edgeMultiplier", eyeEdgeStrenght, eyes.material);
        }

        if (isChanging && soulMaterial != null)
        {
            DOTween.To(() => currentFill, x => currentFill = x, newFill, 0.2f);
            soulMaterial.SetFloat("_fillAmount", currentFill);
            if (Mathf.Approximately(soulMaterial.GetFloat("_fillAmount"), newFill))
            {
                isChanging = false;
                soulMaterial.SetFloat("_waveMultiplier", 1f);
            }
        }
    }

    void GlowPulsing()
    {
        pulseAnimation?.Kill();
        glowAnimation?.Kill();
        pulseAnimation = DOTween.Sequence();
        pulseAnimation.SetLoops(-1);

        float start = 1f;
        float end = 0f;
        glowStrenght = 1f;
        eyeEdgeStrenght = 0f;

        pulseAnimation.Append(DOTween.To(() => glowStrenght, x => glowStrenght = x, end, glowPulsingSpeed));
        pulseAnimation.Join(DOTween.To(() => eyeEdgeStrenght, x => eyeEdgeStrenght = x, start, glowPulsingSpeed));
        pulseAnimation.Append(DOTween.To(() => glowStrenght, x => glowStrenght = x, start, glowPulsingSpeed));
        pulseAnimation.Join(DOTween.To(() => eyeEdgeStrenght, x => eyeEdgeStrenght = x, end, glowPulsingSpeed));
    }

    void UpdateMaterial(string _name, float _value, Material _material)
    {
        if (_material != null && !Mathf.Approximately(_material.GetFloat(_name), _value))
        {
            _material.SetFloat(_name, _value);
        }
    }

    private void UpdateSoulFill(float _currentSoul, float _maxSoul, float _soulModifier)
    {
        currentSoul = _currentSoul;
        maxSoul = _maxSoul;

        if (eyes != null)
        {
            if (_currentSoul >= eyeAppearThreshold)
            {
                if (eyes.transform.localScale == new Vector3(1f, 0f, 1f) || eyes.transform.localScale.y < 0.5f)
                {
                    eyeAnimation?.Kill();
                    eyes.transform.localScale = new Vector3(1f, 0f, 1f);
                    eyeAnimation = DOTween.Sequence();
                    eyeAnimation.Append(eyes.transform.DOScale(new Vector3(1f, 1f, 1f), 0.13f).SetEase(Ease.OutSine));
                }
                else
                {
                    eyeAnimation?.Kill();
                }
            }
            else
            {
                if (eyes.transform.localScale == new Vector3(1f, 1f, 1f) || eyes.transform.localScale.y > 0.5f)
                {
                    eyeAnimation?.Kill();
                    eyes.transform.localScale = new Vector3(1f, 1f, 1f);
                    eyeAnimation = DOTween.Sequence();
                    eyeAnimation.Append(eyes.transform.DOScale(new Vector3(1f, 0f, 1f), 0.15f).SetEase(Ease.OutElastic));
                }
                else
                {
                    eyeAnimation?.Kill();
                }
            }
        }

        if (soulMaterial != null)
        {
            if (_currentSoul < _soulModifier && !Mathf.Approximately(soulMaterial.GetFloat("_fillAlpha"), 0.2f))
            {
                soulMaterial.SetFloat("_fillAlpha", 0.2f);
            }
            if (_currentSoul >= _soulModifier && Mathf.Approximately(soulMaterial.GetFloat("_fillAlpha"), 0.2f))
            {
                if (soulParticles != null)
                {
                    soulParticles.Spawn();
                }

                if (soulIncrease != null)
                {
                    soulIncrease.Play();
                }

                soulMaterial.SetFloat("_fillAlpha", 1f);
            }

            newFill = _maxSoul > 0 ? _currentSoul / _maxSoul : 0f;
            currentFill = soulMaterial.GetFloat("_fillAmount");
            isChanging = true;
        }

        if (Mathf.Approximately(_currentSoul, _maxSoul) && _maxSoul > 0)
        {
            GlowPulsing();
        }
        else
        {
            glowAnimation?.Kill();
            pulseAnimation?.Kill();
            eyeEdgeStrenght = 1f;
            glowStrenght = 0f;
            if (eyes != null && eyes.material != null)
            {
                UpdateMaterial("_edgeMultiplier", eyeEdgeStrenght, eyes.material);
            }
            if (soulMaterial != null)
            {
                UpdateMaterial("_glowMultiplier", glowStrenght, soulMaterial);
            }
        }
    }

    private void GlowPulse(float _waveAffector)
    {
        if (_waveAffector > 1f && currentSoul != maxSoul)
        {
            glowAnimation?.Kill();
            glowAnimation = DOTween.Sequence();
            float start = 1f;
            float end = 0f;
            glowStrenght = 0f;
            eyeEdgeStrenght = 1f;

            glowAnimation.Append(DOTween.To(() => glowStrenght, x => glowStrenght = x, start, glowSpeed / 2f));
            glowAnimation.Join(DOTween.To(() => eyeEdgeStrenght, x => eyeEdgeStrenght = x, end, glowSpeed / 2f));
            glowAnimation.Append(DOTween.To(() => glowStrenght, x => glowStrenght = x, end, glowSpeed));
            glowAnimation.Join(DOTween.To(() => eyeEdgeStrenght, x => eyeEdgeStrenght = x, start, glowSpeed));
        }
    }

    private void LenghtenWaves(float _waveAffector)
    {
        waveAnimation?.Kill();
        waveAnimation = DOTween.Sequence();
        float start = 1f;
        waveLength = 1f;

        if (_waveAffector < 1f && currentSoul != 0)
        {
            if (bubbles != null)
            {
                bubbles.Play();
            }
        }

        waveAnimation.Append(DOTween.To(() => waveLength, x => waveLength = x, _waveAffector, waveLenghtSpeed / 2f));
        waveAnimation.Append(DOTween.To(() => waveLength, x => waveLength = x, start, waveLenghtSpeed));
    }
}
