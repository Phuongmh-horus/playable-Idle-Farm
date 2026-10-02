using System;
using System.Linq;
using UnityEngine;

public class ConfigHolder : MonoSingleton<ConfigHolder>
{
    public LevelConfigSO LevelConfigSO;
    public AvatarConfigSO AvatarConfigSO;
    public FrameConfigSO FrameConfigSO;
    public DailyGiftConfigSO DailyGiftConfigSO;
    public FreeGoldConfigSO FreeGoldConfigSO;
    public OfferPackSO OfferPackSO;
    public ApplovinMaxAdsConfigSO ApplovinMaxAdsConfigSO;

    #region Gameplay
 
    public FarmPlotConfigSO FarmPlotConfigSO;
    public CircularFarmPlotLayoutConfigSO CircularFarmPlotLayoutConfigSO;
    public FarmConfig FarmConfig;
    public PlantConfig PlantConfig;
    public FruitConfig FruitConfig;
    public ValueUpgradePanelConfig ValueUpgradePanelConfig;
    public CollectionConfigSO CollectionConfigSO;
    public MainQuestConfig MainQuestConfig;
    public DailyWeeklyQuestConfig DailyWeeklyQuestConfig;
    public OfflineRewardConfigSO OfflineRewardConfigSO;

    #endregion
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (LevelConfigSO == null)
        {
            LevelConfigSO = FindObjectOfType<LevelConfigSO>();
        }

        if (FarmPlotConfigSO == null)
        {
            FarmPlotConfigSO = FindAssetOfType<FarmPlotConfigSO>();
        }

        if (CircularFarmPlotLayoutConfigSO == null)
        {
            CircularFarmPlotLayoutConfigSO = FindAssetOfType<CircularFarmPlotLayoutConfigSO>();
        }

        if (FarmConfig == null)
        {
            FarmConfig = FindAssetOfType<FarmConfig>();
        }

        if (PlantConfig == null)
        {
            PlantConfig = FindAssetOfType<PlantConfig>();
        }

        if (FruitConfig == null)
        {
            FruitConfig = FindAssetOfType<FruitConfig>();
        }

        if (ValueUpgradePanelConfig == null)
        {
            ValueUpgradePanelConfig = FindAssetOfType<ValueUpgradePanelConfig>();
        }

        if (CollectionConfigSO == null)
        {
            CollectionConfigSO = FindAssetOfType<CollectionConfigSO>();
        }

        if (MainQuestConfig == null)
        {
            MainQuestConfig = FindAssetOfType<MainQuestConfig>();
        }

        if (DailyWeeklyQuestConfig == null)
        {
            DailyWeeklyQuestConfig = FindAssetOfType<DailyWeeklyQuestConfig>();
        }

        if (OfflineRewardConfigSO == null)
        {
            OfflineRewardConfigSO = FindAssetOfType<OfflineRewardConfigSO>();
        }

        if (AvatarConfigSO == null)
        {
            AvatarConfigSO = FindObjectOfType<AvatarConfigSO>();
        }

        if (FrameConfigSO == null)
        {
            FrameConfigSO = FindObjectOfType<FrameConfigSO>();
        }

        if (DailyGiftConfigSO == null)
        {
            DailyGiftConfigSO = FindObjectOfType<DailyGiftConfigSO>();
        }
        if (FreeGoldConfigSO == null)
        {
            FreeGoldConfigSO = FindObjectOfType<FreeGoldConfigSO>();
        }

        if (OfferPackSO == null)
        {
            OfferPackSO = FindAssetOfType<OfferPackSO>();
        }

        if (ApplovinMaxAdsConfigSO == null)
        {
            ApplovinMaxAdsConfigSO = FindObjectOfType<ApplovinMaxAdsConfigSO>();
        }

    }

    public static T FindAssetOfType<T>() where T : UnityEngine.Object
    {
        string typeName = typeof(T).Name;
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeName}");

        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return null;
    }
#endif
}
