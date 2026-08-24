using UnityEngine;
using TMPro;
using GameAds;
using System.Collections.Generic;
public class UIManager : MonoSingleton<UIManager>
{    
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI endGameButtonText;
    public List<SpriteData> spriteDatas;

    
    
    void OnEnable()
    {
        GameEventBus.OnWinGame += () => ShowEndGamePanel(EndGameState.Win);
        GameEventBus.OnLoseGame += () => ShowEndGamePanel(EndGameState.Lose);
        GameEventBus.OnPauseGame += () => ShowEndGamePanel(EndGameState.Pause);
        // GameEventBus.OnTimeUpdate += UpdateRemainingTime;
    }
    void OnDisable()
    {
        GameEventBus.OnWinGame -= () => ShowEndGamePanel(EndGameState.Win);
        GameEventBus.OnLoseGame -= () => ShowEndGamePanel(EndGameState.Lose);
        GameEventBus.OnPauseGame -= () => ShowEndGamePanel(EndGameState.Pause);
        // GameEventBus.OnTimeUpdate -= UpdateRemainingTime;
    }


    void Start()
    {
        HideEndGamePanel();
        // GameEvent.OnStartGame
    }

    // private int remainTime = ConstGame.TIME_LIMIT;
    // // format time: 02:00
    // public void UpdateRemainingTime(float remainingTime)
    // {
    //     int current = Mathf.FloorToInt(remainingTime);
    //     if (current == remainTime) return;
    //     remainTime = current;
    //     int minutes = Mathf.FloorToInt(remainingTime / 60);
    //     int seconds = Mathf.FloorToInt(remainingTime % 60);
    //     if (remainingTime <= 0)
    //     {
    //         remainingTimeText.SetText("00:00");
    //         return;
    //     }
    //     remainingTimeText.SetText("{0:00}:{1:00}", minutes, seconds);
    //     if(remainTime <= ConstGame.WARNING_TIME && isWarningTime == false)
    //     {
    //         isWarningTime = true;
    //         // SoundManager.Instance.PlayOneShot(SoundName.WarningTime);
    //         remainingTimeText.gameObject.GetComponent<Animator>().SetTrigger("WarningTime");
    //     }
    // }

    public void HideEndGamePanel()
    {
        endGamePanel.SetActive(false);
    }

    public void ShowEndGamePanel(EndGameState state)
    {
        switch (state)
        {
            case EndGameState.Win:
                endGameButtonText.SetText(ConstUI.WIN_GAME_TEXT_BUTTON);
                break;
            case EndGameState.Lose:
                endGameButtonText.SetText(ConstUI.LOSE_GAME_TEXT_BUTTON);
                break;
            case EndGameState.Pause:
                endGameButtonText.SetText(ConstUI.PAUSE_GAME_TEXT_BUTTON);
                break;
        }
        endGamePanel.SetActive(true);
    }

    public Sprite GetSpriteByType(SpriteType type)
    {
        SpriteData data = spriteDatas.Find(s => s.type == type);
        return data != null ? data.sprite : null;
    }


    public Sprite GetSpriteByType(int type)
    {
        SpriteData data = spriteDatas.Find(s => (int)s.type == type);
        return data != null ? data.sprite : null;
    }

    public Sprite GetSpriteByType(string type)
    {
        SpriteData data = spriteDatas.Find(s => s.type.ToString().Contains(type));
        return data != null ? data.sprite : null;
    }


}
