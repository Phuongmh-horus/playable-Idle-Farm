using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteShowInterLoseGame
{
    public static bool CheckShowAds(int loseStreak)
    {
        var zipData = FirebaseManager.Instance.GetRemoteInterLoseGame();
        if (string.IsNullOrEmpty(zipData))
        {
            PlayerPrefs.DeleteKey(DataManager.LoseStreakKey);
            return false;
        }
        if (DataManager.HadRemoveImperativeAds()) return false;
        Debug.Log($"CheckShowAds: {zipData} // {loseStreak}");
        // Bỏ {} nếu có
        string trimmed = zipData
            .Trim()
            .TrimStart('{')
            .TrimEnd('}');
        // Tách các cặp key:value
        var pairs = trimmed.Split(',');

        foreach (var pair in pairs)
        {
            var kv = pair.Split(':');
            if (kv.Length != 2) continue;
            if (int.TryParse(kv[0]
                    .Trim()
                    .Trim('"'), out int level) &&
                int.TryParse(kv[1]
                    .Trim()
                    .Trim('"'), out int loseStreakParse))
            {
                if (level <= 0 || loseStreakParse <= 0) continue;
                if (level <= DataManager.PlayerData.LevelProgress)
                {
                    if (loseStreak >= loseStreakParse)
                    {
                        return true;
                    }
                }
                else
                {
                    PlayerPrefs.DeleteKey(DataManager.LoseStreakKey);
                }
            }
        }
        return false;
    }
}
