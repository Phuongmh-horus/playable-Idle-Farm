using System;


public static class GameEventBus
{

    #region CTA
    public static Action OnCTAClicked;
    public static Action OnStartGame;
    public static Action<int> ActionOnPlantSelected; // plantId

    // Farm runtime events retained for the playable UI and tutorial only.
    public static Action OnGoldChange;
    public static Action<int> OnPlotUnlockedEvent;
    public static Action<int> OnSprinklerUpgradedEvent;
    public static Action<int> OnHarvesterUpgradedEvent;
    public static Action<int, int> OnCropHarvestedEvent;
    public static Action<int, int> OnCropUpgradedEvent;
    public static Action<double> OnRevenueEarnedEvent;
    public static Action OnUpdateTransportStation;

    // Camera/selection events are lightweight hooks used by the imported
    // interaction layer; they do not introduce an account or service system.
    public static Action OnNothingClicked;

    // Compatibility with the existing global CTA/endcard manager.
    public static Action OnGameStart;
    public static Action OnGameEnd;
    public static Action OnShowCTA;
    public static Action<int> SetBottomBarTabActive;

    #endregion
}
