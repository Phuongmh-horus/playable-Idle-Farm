using System;


public static class GameEventBus
{

    #region CTA
    public static Action<EndGameState> OnEndGame;
    public static Action OnShowCTA;
    
    public static Action OnCTAClicked;

    public static Action OnStartGame;

    public static Action OnLoseGame;
    public static Action OnWinGame;
    public static Action OnPauseGame;
    public static Action<float> OnTimeUpdate;

    // public static Action OnMatchRowOrColumn; // position of the matched row


    #endregion
}
