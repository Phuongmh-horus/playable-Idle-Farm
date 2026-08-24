public enum CtaReason
{
    Win,
    Lose,
    PreLose,
    ClickThreshold,
    ProgressThreshold,
    Timeout,
    ManualButton
}

public enum CtaMode
{
    ShowUIOnly,
    ShowUIThenInstallOnClick,
    ImmediateInstall
}

public enum CtaSessionState
{
    Idle,
    Running,
    CtaVisible,
    StoreOpened,
    Ended
}
