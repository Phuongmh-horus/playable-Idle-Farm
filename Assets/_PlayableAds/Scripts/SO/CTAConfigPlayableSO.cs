using System.Collections;

using UnityEngine;

[CreateAssetMenu(fileName = "CTAConfigPlayableSO", menuName = "PLA/CTAConfigPlayableSO")]
public class CTAConfigPlayableSO : ScriptableObject
{    
    [Header ("Transition Level")]
    public float transitionFadeDuration = 1f;
    public float transitionDelay = 0.5f;

    [Header("Tutorial Layer")]
    public float textScaleUp = 1.08f;
    public float textScaleDuration = 0.7f;

    public float handScaleUp = 1.2f;
    public float handMoveDuration = 0.7f;

    [Header("Endcard")]

    public float endcardDelay = 0.5f;
    public float endcardFadeDuration = 0.4f;
    [Header("CTA Buttons")]
    public float buttonPulseScale = 1.08f;
    public float buttonPulseDuration = 0.6f;
    public float ctaOnlyPulseScale = 1.1f;
    public float ctaOnlyStartScale = 0.9f;
    public float ctaImpactStartScale = 1.7f;
    public float ctaImpactStartAngle = -18f;
    public float ctaImpactDuration = 0.18f;

    [Header("SFX Praise user")]

    public float praiseVfxDuration = 1f;
    public float praiseFadeDuration = 0.3f;
    public float praiseFadeEndScale = 0.5f;
    public float praiseVfxStartAngle = -12f;
    public float praiseVfxImpactDuration = 0.2f;


}
