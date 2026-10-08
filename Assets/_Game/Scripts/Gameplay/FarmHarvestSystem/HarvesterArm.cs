using System.Collections.Generic;
using UnityEngine;

public class HarvesterArm : MonoBehaviour
{
    [SerializeField] private HarvesterArmConfig config;
    [SerializeField] private Transform armPivot;
    [SerializeField] private GameObject[] harvests;
    [SerializeField] private List<Transform> blades = new List<Transform>();

    private Vector3[] _initialBladeScales;
    private float _pulseElapsed;
    public HarvesterArmConfig Config { get { return config; } }
    public Transform Pivot { get { return armPivot != null ? armPivot : transform; } }
    public int CurrentBladeCount { get; private set; } = 1;

    private void Awake()
    {
        if (armPivot == null) armPivot = transform;
        Pivot.localRotation = Quaternion.Euler(0f, 0f, config != null ? config.rotationOffsetZ : 0f);
        CacheInitialScales();
    }

    private void OnEnable() { _pulseElapsed = 0f; }
    private void OnDisable() { ResetBladeScale(); }

    private void Update()
    {
        // Keep the blade animation self-contained; the owning system rotates
        // the arm so sprinkler and harvester share the same Luna-safe motion path.
        AnimateBladePulse(Time.deltaTime);
    }

    private void CacheInitialScales()
    {
        _initialBladeScales = new Vector3[blades.Count];
        for (int i = 0; i < blades.Count; i++) _initialBladeScales[i] = blades[i] != null ? blades[i].localScale : Vector3.one;
    }

    private void AnimateBladePulse(float deltaTime)
    {
        if (blades.Count == 0) return;

        float duration = config != null ? Mathf.Max(0.02f, config.punchDuration) : 0.2f;
        float peakScale = config != null ? config.harvestPunchScale : 1.15f;
        float interval = config != null ? Mathf.Max(0f, config.punchInterval) : 0.5f;
        float cycleDuration = duration + interval;
        _pulseElapsed = Mathf.Repeat(_pulseElapsed + deltaTime, cycleDuration);
        float multiplier = 1f;
        if (_pulseElapsed < duration)
        {
            float progress = _pulseElapsed / duration;
            multiplier = Mathf.Lerp(1f, peakScale, Mathf.Sin(progress * Mathf.PI));
        }
        ApplyBladeScale(multiplier);
    }

    private void ApplyBladeScale(float multiplier)
    {
        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null) blades[i].localScale = _initialBladeScales[i] * multiplier;
        }
    }

    private void ResetBladeScale()
    {
        _pulseElapsed = 0f;
        ApplyBladeScale(1f);
    }

    public void RotateArm(float speed, float deltaTime) { Pivot.Rotate(0f, 0f, -speed * deltaTime, Space.Self); }

    public void SpinBlades(float deltaTime)
    {
        float speed = config != null ? config.bladeSpinSpeed : 360f;
        for (int i = 0; i < blades.Count; i++) if (blades[i] != null && blades[i].gameObject.activeInHierarchy) blades[i].Rotate(0f, 0f, -speed * deltaTime, Space.Self);
    }

    public void SetBladeCount(int bladeCount)
    {
        CurrentBladeCount = Mathf.Max(1, bladeCount);
        if (harvests == null) return;
        for (int i = 0; i < harvests.Length; i++) if (harvests[i] != null) harvests[i].SetActive(i < CurrentBladeCount);
    }

    public Vector3 GetForwardDirection(float additionalAngleOffset = 0f)
    {
        return (Quaternion.Euler(0f, 0f, additionalAngleOffset) * Pivot.right).normalized;
    }
}
