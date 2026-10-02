using Cysharp.Threading.Tasks;
using UnityEngine;

public class CheckDailyGift : UIEventBase
{
    private bool _hasShownThisSession = false;

    public override UniTask<bool> CheckRequire()
    {
        var dailyGiftConfig = ConfigHolder.Instance.DailyGiftConfigSO;
        if(DataManager.PlayerData.LevelDisplay < dailyGiftConfig.LevelOpen) return UniTask.FromResult(false);
        // Return true only once per game session
        if (_hasShownThisSession)
        {
            return UniTask.FromResult(false);
        }

        _hasShownThisSession = true;
        return UniTask.FromResult(true);
    }

    public override void Play()
    {
        // Show popup daily gift using LayerManager
        LayerManager.Instance.ShowPopupDailyGift();
    }
}
