using System;

public interface IWorldLayerAnim
{
    void PlayShowAnim(Action onComplete = null);
    void PlayHideAnim(Action onComplete = null);
    void CancelAnim();
}
