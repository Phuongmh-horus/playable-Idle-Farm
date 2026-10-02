using AdMod;
using Cysharp.Threading.Tasks;

public class MainMenuState : GameStateBase
{
    private void OnValidate()
    {
        State = GameState.MainMenu;
    }

    protected override void OnEnter()
    {
        LayerManager.Instance.ShowMainMenuLayer((layerGroup) =>
        {
            ShowBannerAds();
            UIEventManager.Instance.CheckRequireAndStart().Forget();
            GameEventBus.SetBottomBarTabActive?.Invoke(BottomBarTabType.Home, false);
        });
    }

    private void ShowBannerAds()
    {
        AdmobManager.Instance.ShowBannerAds();
    }

    protected override void OnExit()
    {

    }
}
