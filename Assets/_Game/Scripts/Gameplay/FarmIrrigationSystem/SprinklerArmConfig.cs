using UnityEngine;

[CreateAssetMenu(fileName = "SprinklerArmConfig", menuName = "Farm/SprinklerArmConfig")]
public class SprinklerArmConfig : ScriptableObject
{
    [Range(0.1f, 45f)]
    public float alignmentTolerance = 3f;

    public float waterAnimationDuration = 0.5f;

    public float rotationOffsetZ = 0f;
}
