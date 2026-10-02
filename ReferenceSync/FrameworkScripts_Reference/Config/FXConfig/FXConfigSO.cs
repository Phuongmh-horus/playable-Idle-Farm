using Coffee.UIExtensions;
using UnityEngine;

[CreateAssetMenu(fileName = "FXConfigSO", menuName = "ScriptableObjects/FXConfig")]
public sealed class FXConfigSO : ScriptableObject
{
    [Header("Animated Hoe (World FX)")]
    public AnimatedHoe hoePrefab;

    [Header("UI VFX Settings")]
    public UIParticle cashUIParticlePrefab;
    public float defaultUIParticleDuration = 1.0f;
}
