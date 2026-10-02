using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoSingleton<GameStateManager>
{
    [SerializeField] private List<GameStateBase> gameStateBases;
    private Dictionary<GameState, GameStateBase> _gameStateDict;
    public static ActionSealed<GameState> OnGameStateChange = new();

    public GameState CurrentGameState = GameState.None;

    public void SetGameState(GameState gameState)
    {
        CurrentGameState = gameState;
        Debug.Log($"[{Time.frameCount}]GameController SetGameState: {gameState}");
    }

    public void PlayState(GameState gameState)
    {
        var oldState = GetGameState(CurrentGameState);
        oldState?.TryExit();
        CurrentGameState = gameState;
        var newState = GetGameState(CurrentGameState);
        newState?.TryEnter();
        OnGameStateChange?.Invoke(gameState);
    }

    private GameStateBase GetGameState(GameState gameState)
    {
        _gameStateDict ??= new Dictionary<GameState, GameStateBase>();
        if (_gameStateDict.Count > 0)
        {
            if (_gameStateDict.TryGetValue(gameState, out var state))
            {
                return state;
            }
            Debug.LogError($"[GameStateManager] not found state: {gameState}");
            return null;
        }
        GameStateBase result = null;
        foreach (var gameStateBase in gameStateBases)
        {
            _gameStateDict.Add(gameStateBase.State, gameStateBase);
            if (gameStateBase.State == gameState)
            {
                result = gameStateBase;
            }
        }
        return result;
    }

    public bool IsState(GameState state)
    {
        return CurrentGameState == state;
    }
}
