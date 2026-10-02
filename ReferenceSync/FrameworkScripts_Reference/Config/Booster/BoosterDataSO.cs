using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableObjects/Boosters/BoosterDataSO", menuName = "ScriptableObjects/Boosters/BoosterDataSO")]
public class BoosterDataSO : ScriptableObject
{
    public BoosterType BoosterType;
    public double BoosterCost;
    public int LevelOpen;
    public Sprite BoosterIcon;
}

[Serializable]
public enum BoosterType : byte
{
    None,
    SampleBooster
}
