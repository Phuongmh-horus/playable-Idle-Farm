using System;
using UnityEngine;

public sealed class CTAOrchestrator : ICTAOrchestrator
{
    private readonly CTAConfig _config;
    private readonly IPlayablePlatformAdapter _adapter;
    private readonly Func<bool> _isTutorialModeGetter;

    private ICTAPresenter _presenter;
    private bool _sessionStarted;
    private bool _gameEndedSent;
    private bool _storeOpened;
    private bool _ctaVisible;
    private bool _clickThresholdTriggered;
    private int _clickCount;
    private float _lastCtaRequestTime = float.NegativeInfinity;

    public CtaSessionState State
    {
        get
        {
            if (_storeOpened) return CtaSessionState.StoreOpened;
            if (_gameEndedSent) return CtaSessionState.Ended;
            if (_ctaVisible) return CtaSessionState.CtaVisible;
            return _sessionStarted ? CtaSessionState.Running : CtaSessionState.Idle;
        }
    }

    public CTAOrchestrator(
        CTAConfig config,
        IPlayablePlatformAdapter adapter,
        ICTAPresenter presenter = null,
        Func<bool> isTutorialModeGetter = null)
    {
        _config = config;
        _adapter = adapter ?? new NoOpPlayableAdapter();
        _isTutorialModeGetter = isTutorialModeGetter;
        RegisterPresenter(presenter);
    }

    public void RegisterPresenter(ICTAPresenter presenter)
    {
        if (_presenter != null) _presenter.CtaButtonClicked -= OnManualCtaButtonClick;

        _presenter = presenter;

        if (_presenter != null) _presenter.CtaButtonClicked += OnManualCtaButtonClick;
    }

    public void StartSession()
    {
        if (_sessionStarted) return;

        _sessionStarted = true;
        _adapter.GameStarted();
        Debug.Log("[CTA] Session started.");
    }

    public void RequestCTA(CtaReason reason, CtaMode mode)
    {
        if (!IsCtaEnabled()) return;
        if (ShouldSuppress(reason)) return;
        if (!IsDebouncePassed()) return;

        if (!_sessionStarted) StartSession();

        if (mode == CtaMode.ImmediateInstall)
        {
            SendGameEndedIfNeeded();
            TriggerInstall(reason);
            return;
        }

        if (_presenter != null)
        {
            _presenter.Show(reason);
            _ctaVisible = _presenter.IsVisible;
        }
        else if (mode == CtaMode.ShowUIThenInstallOnClick)
        {
            Debug.LogWarning("[CTA] ShowUIThenInstallOnClick requested but no presenter is assigned. Installing immediately.");
            SendGameEndedIfNeeded();
            TriggerInstall(reason);
            return;
        }

        if (ShouldSendGameEndedWhenShowCta())
        {
            SendGameEndedIfNeeded();
        }
    }

    public void OnManualCtaButtonClick()
    {
        if (!IsCtaEnabled()) return;
        if (ShouldSuppress(CtaReason.ManualButton)) return;

        SendGameEndedIfNeeded();
        TriggerInstall(CtaReason.ManualButton);
    }

    public void NotifySelectableClick()
    {
        if (_config == null || !_config.enableClickThresholdCta) return;
        if (ShouldSuppress(CtaReason.ClickThreshold)) return;
        if (_clickThresholdTriggered && !_config.enableContinuousClickCta) return;

        _clickCount++;
        var threshold = Mathf.Max(1, _config.clickThreshold);

        if (_clickCount < threshold) return;

        _clickCount = 0;
        _clickThresholdTriggered = true;
        RequestCTA(CtaReason.ClickThreshold, _config.clickThresholdMode);
    }

    private bool IsCtaEnabled()
    {
        return _config == null || _config.enableCtaSystem;
    }

    private bool ShouldSuppress(CtaReason reason)
    {
        if (_config == null) return false;
        if (_isTutorialModeGetter == null) return false;

        var isTutorialMode = _isTutorialModeGetter.Invoke();
        if (!isTutorialMode) return false;

        if (_config.suppressCtaInTutorial) return true;
        if (reason == CtaReason.ClickThreshold && _config.suppressThresholdCTAInTutorial) return true;

        return false;
    }

    private bool IsDebouncePassed()
    {
        if (_config == null) return true;

        var debounce = Mathf.Max(0f, _config.debounceSeconds);
        if (debounce <= 0f)
        {
            _lastCtaRequestTime = Time.unscaledTime;
            return true;
        }

        var now = Time.unscaledTime;
        if (now - _lastCtaRequestTime < debounce) return false;

        _lastCtaRequestTime = now;
        return true;
    }

    private bool ShouldSendGameEndedWhenShowCta()
    {
        return _config == null || _config.sendGameEndedWhenShowCTA;
    }

    private void SendGameEndedIfNeeded()
    {
        if (_gameEndedSent) return;

        _gameEndedSent = true;
        _adapter.GameEnded();
    }

    private void TriggerInstall(CtaReason reason)
    {
        if (_storeOpened && !AllowMultipleInstallCalls())
        {
            Debug.Log($"[CTA] Duplicate install prevented. reason={reason}");
            return;
        }

        _storeOpened = true;
        _adapter.InstallFullGame();
        Debug.Log($"[CTA] Install requested. reason={reason}");
    }

    private bool AllowMultipleInstallCalls()
    {
        return _config != null && _config.allowMultipleInstallCalls;
    }
}
