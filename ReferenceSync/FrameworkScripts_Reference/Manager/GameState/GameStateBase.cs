using System;
using UnityEngine;

public abstract class GameStateBase : MonoBehaviour
{
    public GameState State;
    protected abstract void OnEnter();
    protected abstract void OnExit();

    public void TryEnter()
    {
        try
        {
            OnEnter();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{Time.frameCount}][{gameObject.name}] Enter Exception: {e}");
        }
    }

    public void TryExit()
    {
        try
        {
            OnExit();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{Time.frameCount}][{gameObject.name}] Exit Exception: {e}");
        }
    }
}
public enum GameState
{
    None = 0,
    MainMenu = 1,
    InGame = 2,
}
