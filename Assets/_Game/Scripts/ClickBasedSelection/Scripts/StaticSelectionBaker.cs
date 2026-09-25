using UnityEngine;

/// <summary>
/// Optional inspector cache for scene-authored tap targets. Dynamic farm plots register
/// themselves on enable, so this only needs entries which are already active at boot.
/// </summary>
public class StaticSelectionBaker : MonoBehaviour
{
    [SerializeField] private StaticSelectable[] initialSelectables = new StaticSelectable[0];

    public void RegisterConfiguredSelectables(JobSelectionManager selectionManager)
    {
        if (selectionManager == null || initialSelectables == null)
            return;

        for (int i = 0; i < initialSelectables.Length; i++)
        {
            if (initialSelectables[i] != null)
                selectionManager.RegisterSelectable(initialSelectables[i]);
        }
    }
}
