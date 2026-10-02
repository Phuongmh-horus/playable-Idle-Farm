using TMPro;
using UnityEngine;

public class CapacityFruitTransport : WorldLayerBase
{
    [SerializeField] private TextMeshProUGUI txtCapacity;

    protected override void Awake()
    {
        base.Awake();
        blockSelectionClick = false;
        if (txtCapacity == null)
        {
            txtCapacity = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    public void InitData(int current = 0, int max = 10)
    {
        UpdateCapacity(current, max);
    }

    public void UpdateCapacity(int current, int max)
    {
        if (txtCapacity == null)
        {
            txtCapacity = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (txtCapacity != null)
        {
            if (current >= max && max > 0)
            {
                txtCapacity.text = "Cargo Full";
            }
            else
            {
                txtCapacity.text = $"{current}/{max}";
            }
        }
    }
}
