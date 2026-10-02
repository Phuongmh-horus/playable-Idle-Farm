using System;
using UnityEngine;

public class GamePlayState : GameStateBase
{
    private void OnValidate()
    {
        State = GameState.InGame;
    }

    protected override void OnEnter()
    {
        LayerManager.Instance.ShowGamePlayLayer();
    }

    protected override void OnExit()
    {

    }
}
