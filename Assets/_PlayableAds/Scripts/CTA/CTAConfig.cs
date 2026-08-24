using UnityEngine;

[CreateAssetMenu(menuName = "Playable/CTA Config", fileName = "CTAConfig")]
public sealed class CTAConfig : ScriptableObject
{
    [Header("Global Policy")]
    public bool enableCtaSystem = true;
    public bool useLunaAdapter = true;
    public bool sendGameEndedWhenShowCTA = true;
    [Min(0f)] public float debounceSeconds = 0.1f;
    public bool allowMultipleInstallCalls = false;

    [Header("Endgame Policy")]
    public bool autoEndGameStoreOnWin = true;
    public bool autoEndGameStoreOnLose = true;
    public CtaMode winFallbackMode = CtaMode.ShowUIThenInstallOnClick;
    public CtaMode loseFallbackMode = CtaMode.ShowUIThenInstallOnClick;

    [Header("Mid-game CTA")]
    public bool enableClickThresholdCta = false;
    [Min(1)] public int clickThreshold = 8;
    public bool enableContinuousClickCta = false;
    public CtaMode clickThresholdMode = CtaMode.ShowUIThenInstallOnClick;

    [Header("Tutorial Policy")]
    public bool suppressCtaInTutorial = false;
    public bool suppressThresholdCTAInTutorial = true;

    [Header("UI Policy")]
    [Min(0f)] public float ctaAppearDelay = 0f;

    public CtaMode ResolveWinMode()
    {
        return autoEndGameStoreOnWin ? CtaMode.ImmediateInstall : winFallbackMode;
    }

    public CtaMode ResolveLoseMode()
    {
        return autoEndGameStoreOnLose ? CtaMode.ImmediateInstall : loseFallbackMode;
    }
}
