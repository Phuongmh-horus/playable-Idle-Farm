using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;


public class CheckShowOfferLayer : UIEventBase
{
    private void Start()
    {
        GameEventBus.OnOfferPackClosed += OnOfferPackClosed;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameEventBus.OnOfferPackClosed -= OnOfferPackClosed;
    }
    private async void OnOfferPackClosed()
    {
        await UniTask.NextFrame();
        EndEvent?.Invoke();
    }
    public override UniTask<bool> CheckRequire()
    {
        return UniTask.FromResult(OfferPackManager.Instance.GetValidOfferPacks().Any());
    }
    
    public override void Play()
    {
        OfferPackManager.Instance.ShowOfferPack(false);
    }
}
