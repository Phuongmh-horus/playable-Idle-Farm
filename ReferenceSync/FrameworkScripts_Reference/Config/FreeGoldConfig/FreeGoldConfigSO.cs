using UnityEngine;

[CreateAssetMenu(fileName = "FreeGoldConfigSO", menuName = "ScriptableObjects/FreeGoldConfigSO", order = 1)]
public class FreeGoldConfigSO : ScriptableObject
{
    public double FreeCashAmount = 100;
    public int FreeInterval = 1440;
    public double AdsCashAmount = 100;
    public int AdsLimitPerDay = 5;
}
