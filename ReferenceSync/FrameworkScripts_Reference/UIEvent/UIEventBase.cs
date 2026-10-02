using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public abstract class UIEventBase : MonoBehaviour
{
    public LayerType LayerHide;
    public GameState State;
    public int Priority;
    public Action EndEvent;
    public bool IsPlayRequireMainMenu;
    public abstract UniTask<bool> CheckRequire();

    public void PlayWithCheckMainMenu()
    {
        if (IsPlayRequireMainMenu)
        {
            if (LayerManager.Instance.GetShowingLayerTypes().Contains(LayerType.MainMenuLayer))
            {
                Play();
            }
            else
            {
                UIMainMenuLayer.OnLayerInit.Register(OnMainMenuInit);
            }
        }
        else
        {
            Play();
        }
        if (LayerHide == LayerType.None || IsRegister) return;
        GameEventBus.OnCloseLayerGroup.Register(OnHideLayer);
        IsRegister = true;
    }

    private bool IsRegister;

    private async void OnMainMenuInit()
    {
        UIMainMenuLayer.OnLayerInit.UnRegister(OnMainMenuInit);
        await UniTask.DelayFrame(5);
        Play();
    }

    public abstract void Play();

    protected virtual void Awake()
    {
        
    }

    protected virtual void OnDestroy()
    {
        GameEventBus.OnCloseLayerGroup.UnRegister(OnHideLayer);
    }
    private async void OnHideLayer(ShowLayerGroupData showLayer)
    {
        if (LayerHide == LayerType.None || !LayerManager.Instance) return;
        var lastLayerType = showLayer.LayerTypes;
        if (lastLayerType.Contains(LayerHide))
        {
            OnEndEvent();
        }
    }

    protected async void OnEndEvent()
    {
        await UniTask.NextFrame();
        EndEvent?.Invoke();
        EndEvent = null;
        GameEventBus.OnCloseLayerGroup.UnRegister(OnHideLayer);
        IsRegister = false;
    }
}