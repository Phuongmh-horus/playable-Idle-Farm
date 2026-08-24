using System;

public interface ICTAPresenter
{
    event Action CtaButtonClicked;
    bool IsVisible { get; }
    void Show(CtaReason reason);
    void Hide();
}
