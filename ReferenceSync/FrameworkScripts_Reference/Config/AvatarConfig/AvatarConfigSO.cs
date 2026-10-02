using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AvatarIconSO", menuName = "ScriptableObjects/AvatarIconSO", order = 1)]
public class AvatarConfigSO : ScriptableObject
{
    [SerializeField]
    private List<AvatarConfigSOData> avatarConfigList = new();

    [NonSerialized]
    private Dictionary<int, AvatarConfigSOData> avatarConfigs;

    public Dictionary<int, AvatarConfigSOData> AvatarConfigs
    {
        get
        {
            if (avatarConfigs == null || avatarConfigs.Count == 0)
                RebuildDictionary();
            return avatarConfigs;
        }
    }

    private void OnValidate()
    {
        if (avatarConfigList == null || avatarConfigList.Count == 0) return;
        for (var i = 0; i < avatarConfigList.Count; i++)
        {
            avatarConfigList[i].ID = i;
        }

        RebuildDictionary();
    }

    private void RebuildDictionary()
    {
        avatarConfigs = new Dictionary<int, AvatarConfigSOData>();
        foreach (var data in avatarConfigList)
        {
            if (!avatarConfigs.ContainsKey(data.ID))
                avatarConfigs.Add(data.ID, data);
            else
                Debug.LogWarning($"Duplicate avatar ID: {data.ID}");
        }
    }

    public AvatarConfigSOData GetAvatarConfigByID(int id)
    {
        return AvatarConfigs.TryGetValue(id, out var avatar) ? avatar : null;
    }

    public Sprite GetAvatarIconSpriteByID(int id)
    {
        var avatar = GetAvatarConfigByID(id);
        if (avatar != null) return avatar.AvatarIcon;
        Debug.LogError($"AvatarIcon with ID {id} not found.");
        return null;
    }
}

[Serializable]
public class AvatarConfigSOData
{
    public int ID;
    public Sprite AvatarIcon;
    public Price Price;
}