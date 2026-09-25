using System;
using UnityEngine;

public class HarvestMachine : MonoBehaviour
{
    [Header("Blade Setup")]
    [Tooltip("Vị trí gốc đặt HarvestBlade khi nghỉ (nếu để trống sẽ tự tạo hoặc lấy transform này)")]
    [SerializeField] private Transform harvesterBladeRoot;

    [Tooltip("Instance của HarvestBlade trong scene")]
    [SerializeField] private HarvestBlade harvestBlade;
    [SerializeField] private StaticSelectable selectable;

    public HarvestBlade Blade => harvestBlade != null ? harvestBlade : (harvestBlade = EnsureHarvestBladeInstance());
    public Transform BladeRoot => harvesterBladeRoot != null ? harvesterBladeRoot : transform;
    public FarmPlotSystem PlotSystem => FarmSystem.Instance?.PlotSystem;

    public event Action<Plot, double> OnManualHarvested;

    private void Awake()
    {
        EnsureHarvestBladeInstance();
        if (selectable == null)
        {
            selectable = GetComponent<StaticSelectable>();
        }
        if (selectable != null)
        {
            selectable.OnTapped += HandleMachineTapped;
        }
    }

    private void Start()
    {
        if (harvestBlade != null)
        {
            harvestBlade.OnPlantHarvested += HandlePlantHarvested;
        }
    }

    private void OnDestroy()
    {
        if (harvestBlade != null)
        {
            harvestBlade.OnPlantHarvested -= HandlePlantHarvested;
        }
        if (selectable != null)
        {
            selectable.OnTapped -= HandleMachineTapped;
        }
    }

    private void HandlePlantHarvested(Plot plot, double revenue)
    {
        OnManualHarvested?.Invoke(plot, revenue);
    }

    private void HandleMachineTapped(StaticSelectable tapped)
    {
        // A tap opens the upgrade layer; direct pointer/drag input on Blade
        // remains responsible for the circular cutting motion and harvesting.
        LunaUIManager.Instance?.ShowHarvestUpgrade();
    }

    public HarvestBlade EnsureHarvestBladeInstance()
    {
        if (harvestBlade != null)
        {
            harvestBlade.Initialize(BladeRoot, PlotSystem);
            return harvestBlade;
        }

        // Tìm trong children
        harvestBlade = GetComponentInChildren<HarvestBlade>();

        if (harvestBlade != null)
        {
            harvestBlade.Initialize(BladeRoot, PlotSystem);
        }

        return harvestBlade;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform r = harvesterBladeRoot != null ? harvesterBladeRoot : transform;
        Gizmos.color = Color.yellow;
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(r.position, r.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.5f, 0.5f, 0.1f));
        Gizmos.matrix = prev;
    }
#endif
}
