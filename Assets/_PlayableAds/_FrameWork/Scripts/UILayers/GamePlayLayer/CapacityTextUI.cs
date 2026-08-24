using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class CapacityTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI capacityText;

    public void SetCapacity(int capacity, int maxCapacity)
    {
        capacityText.text = $"{capacity}/{maxCapacity}";
        
    }
}