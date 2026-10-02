using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "HeartConfigSO", menuName = "ScriptableObjects/HeartConfig")]
public class HeartConfigSO : ScriptableObject
{
    public int MaxHeart = 5;
    public int RecoverTime = 600; // seconds
    public double CashRefill = 500;
}
