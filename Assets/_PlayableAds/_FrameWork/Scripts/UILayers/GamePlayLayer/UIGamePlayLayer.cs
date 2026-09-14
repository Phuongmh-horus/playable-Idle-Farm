using UnityEngine;


public class UIGamePlayLayer : LayerBase
{
    [SerializeField] private CapacityUI capacityUI;
    [SerializeField] private CapacityUI successCapacityUI;
    [SerializeField] private CapacityTextUI capacityTextUI;

    private int doneColor = 0;
    private int maxColor = 0;

    private void Awake()
    {
    }

    private void OnDestroy()
    {
    }


    private void Start()
    {
    }
    public void EnsureUI()
    {
        // if(capacityUI != null)
        // {
        //     capacityUI.gameObject.SetActive(LunaUIManager.Instance.IsShowConveyorCapacity);
            
        // }
        
        // if(successCapacityUI != null)
        // {
        //     successCapacityUI.gameObject.SetActive(LunaUIManager.Instance.IsShowConveyorWin);
        // }
        // if (capacityTextUI != null) 
        // {
        //     capacityTextUI.gameObject.SetActive(LunaUIManager.Instance.IsShowConveyorCapacityText);
        // }
    }


    private void UpdateCapacityUI(int capacity, int maxCapacity)
    {
        capacityUI.SetCapacity(capacity, maxCapacity);
        capacityTextUI.SetCapacity(capacity, maxCapacity);
    }


    private void UpdateSuccessCapacityUI()
    {
        doneColor = Mathf.Min(doneColor + 1, maxColor);
        successCapacityUI.SetCapacity(doneColor, maxColor);
    }




    private void Update()
    {
// #if UNITY_EDITOR
//         if (!GameStateManager.Instance.IsState(GameState.InGame)) return;

//         if (Input.GetKeyDown(KeyCode.F1))
//         {
//             LayerManager.Instance.ShowEndGameLayer(new UIEndGameData()
//             {
//                 IsWinMatch = true
//             });
//         }

//         if (Input.GetKeyDown(KeyCode.F3))
//         {
//             LayerManager.Instance.ShowEndGameLayer(new UIEndGameData()
//             {
//                 IsWinMatch = false
//             });
//         }
        
//         if (Input.GetKeyDown(KeyCode.F4))
//         {
//             GameEventBus.OnLoseTrigger?.Invoke(ELoseReason.CapacityFull);
//         }
// #endif
    }


  
}