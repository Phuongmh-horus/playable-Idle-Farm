using UnityEngine;

[CreateAssetMenu(fileName = "AnimatedFruitConfig", menuName = "Farm/AnimatedFruitConfig")]
public class AnimatedFruitConfig : ScriptableObject
{
    public float popDuration = 0.25f;
    public float popRadius = 0.6f;
    public float popInitialScale;
    public float popPeakScale = 0.42f;
    public float preFlightScaleMultiplier = 1.25f;
    public float flightDuration = 0.55f;
    public float startScale = 0.35f;
    public float flightScaleMultiplier = 0.95f;
    public float endScale = 0.25f;
}
