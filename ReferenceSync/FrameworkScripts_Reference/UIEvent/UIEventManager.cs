using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Inspector;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UIEventManager : MonoSingleton<UIEventManager>
{
    [SerializeField] private List<UIEventBase> gameEvents;

    private UIEventBase _firstEvent;
    private readonly List<UIEventBase> _eventBases = new();
    private readonly List<UIEventBase> _gameEventSelects = new();

    public async UniTask CheckRequireAndStart(bool isActive = true)
    {
        await UniTask.Yield();
        _firstEvent = null;
        _eventBases.Clear();
        _gameEventSelects.Clear();

        var currentState = GameStateManager.Instance.CurrentGameState;
        var showingLayerType = LayerManager.Instance.GetShowingLayerTypes();

        // if (!IsValidLayerForState(currentState, showingLayerType)) return;

        // Debug.Log($"[UIEventManager] CheckRequireAndStart - State: {currentState}");

        _gameEventSelects.AddRange(
            gameEvents
                /*.Where(x => x.State == currentState)*/
                .OrderBy(x => x.Priority)
        );

        foreach (var UIEventBase in _gameEventSelects)
        {
            try
            {
                if (await UIEventBase.CheckRequire())
                {
                    _eventBases.Add(UIEventBase);
                    ChainOrPlay(UIEventBase, isActive);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    private bool IsValidLayerForState(GameState state, HashSet<LayerType> showingLayer)
    {
        return state switch
        {
            GameState.InGame => showingLayer.Contains(LayerType.GamePlayLayer),
            GameState.MainMenu => showingLayer.Contains(LayerType.MainMenuLayer),
            _ => true
        };
    }

    private void ChainOrPlay(UIEventBase UIEventBase, bool isActive)
    {
        if (_eventBases.Count > 1)
        {
            _eventBases[^2].EndEvent = UIEventBase.PlayWithCheckMainMenu;
            return;
        }

        if (isActive || _eventBases[0].Priority < 0)
        {
            _eventBases[0].PlayWithCheckMainMenu();
        }
        else
        {
            _firstEvent = _eventBases[0];
        }
    }

    public async UniTaskVoid PlayFirstEvent()
    {
        await UniTask.Yield();
        _firstEvent?.PlayWithCheckMainMenu();
    }

#if UNITY_EDITOR
    [Button]
    public void GetAllEvents()
    {
        gameEvents = GetComponentsInChildren<UIEventBase>().ToList();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
