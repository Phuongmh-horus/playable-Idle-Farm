using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CheckDifficultyWarningLevel : UIEventBase
{
    [SerializeField] private LevelType LevelWarningType = LevelType.Hard;
    public override UniTask<bool> CheckRequire()
    {
        var levelConfigSO = ConfigHolder.Instance.LevelConfigSO;
        var level = DataManager.PlayerData.LevelProgress;
        var levelConfig = levelConfigSO.GetLevelConfigData(level);
        if(levelConfig == null) return UniTask.FromResult(false);
        
        return UniTask.FromResult(levelConfig.LevelType >= LevelWarningType);
    }
    public override void Play()
    {
        LayerManager.Instance.ShowDifficultyWarningLayer();
    }
}
