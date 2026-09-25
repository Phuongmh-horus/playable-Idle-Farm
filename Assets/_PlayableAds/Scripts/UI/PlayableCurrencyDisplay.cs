using TMPro;
using UnityEngine;

/// <summary>
/// Lightweight adapter between the playable's in-memory gold balance and the
/// existing CurrencyLayer artwork. It intentionally has no persistence or
/// animation dependency: every sale and upgrade uses PlayableFarmState as the source.
/// </summary>
public sealed class PlayableCurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;

    private void Awake()
    {
        if (goldText == null) goldText = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnEnable()
    {
        GameEventBus.OnGoldChange += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameEventBus.OnGoldChange -= Refresh;
    }

    public void Refresh()
    {
        if (goldText != null) goldText.text = T_Utility.FormatGold(PlayableFarmState.PlayerData.Gold);
    }
}
