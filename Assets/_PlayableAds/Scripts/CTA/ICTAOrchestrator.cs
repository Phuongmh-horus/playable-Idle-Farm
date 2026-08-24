public interface ICTAOrchestrator
{
    CtaSessionState State { get; }
    void StartSession();
    void RequestCTA(CtaReason reason, CtaMode mode);
    void OnManualCtaButtonClick();
    void NotifySelectableClick();
    void RegisterPresenter(ICTAPresenter presenter);
}
