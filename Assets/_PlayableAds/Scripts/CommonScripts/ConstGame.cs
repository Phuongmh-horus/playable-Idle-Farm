using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameAds
{
    public static class ConstGame
    {
        public const int TIME_LIMIT = 60;
        public const int WARNING_TIME = 10;
    }

    public static class ConstUI
    {
        public const string WIN_GAME_TEXT_BUTTON = "Next Level";
        public const string LOSE_GAME_TEXT_BUTTON = "Try Again";
        
        public const string PAUSE_GAME_TEXT_BUTTON = "Continue";

        public const string PAUSE_TITLE_TEXT = "";

        

    }


}

public enum EndGameState
{
    Pause,
    Win,
    Lose,
}
