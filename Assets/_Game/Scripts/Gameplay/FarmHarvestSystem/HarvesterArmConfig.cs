using UnityEngine;

[CreateAssetMenu(fileName = "HarvesterArmConfig", menuName = "Farm/HarvesterArmConfig")]
public class HarvesterArmConfig : ScriptableObject
{
    [Range(0.1f, 45f)]
    public float alignmentTolerance = 3f;

    public float bladeSpinSpeed = 360f;

    public float harvestPunchScale = 1.15f;

    public float punchDuration = 0.2f;

    [Tooltip("Khoảng thời gian nghỉ giữa 2 lần scale blade (giây)")]
    public float punchInterval = 0.5f;

    public float rotationOffsetZ = 0f;
}
