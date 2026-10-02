using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfigSO", menuName = "ScriptableObjects/LevelConfig")]
public class LevelConfigSO : ScriptableObject
{
    public int LevelStartLoopCampaign;
    public List<LevelConfigData> LevelConfigDataList;
    public LevelConfigData GetLevelConfigData(int levelId)
    {
        levelId = GetLevelToLoad(levelId);
        var targetLevel = LevelConfigDataList.FirstOrDefault(x => x.Level == levelId);
        return targetLevel;
    }

    public int GetLevelToLoad(int playerCurrentLevel)
    {
        int levelStartLoop = LevelStartLoopCampaign;
        int levelToLoad = playerCurrentLevel;
        var levelCount = LevelConfigDataList.Count;
        if (levelToLoad > levelCount)
        {
            levelToLoad = (levelToLoad - levelStartLoop) % (LevelConfigDataList.Count - levelStartLoop + 1) + levelStartLoop;
        }

        return levelToLoad;
    }

    private void OnValidate()
    {
        if (LevelConfigDataList is not { Count: > 0 }) return;
        for (var i = 0; i < LevelConfigDataList.Count; i++)
        {
            LevelConfigDataList[i].Level = i + 1;
        }
    }
}
[Serializable]
public class LevelConfigData
{
    public int Level;
    public double CashReward;
    public Sprite LevelIcon;
    public LevelType LevelType;
}

public enum LevelType
{
    None = 0,
    Normal = 1,
    Medium = 2,
    Hard = 3,
    Extreme = 4
}