using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HarvesterArm : MonoBehaviour
{
    [SerializeField] private HarvesterArmConfig config;
    [SerializeField] private Transform armPivot;
    [SerializeField] private GameObject[] harvests;
    [SerializeField] private List<Transform> blades = new List<Transform>();

    private Vector3[] _initialBladeScales;
    private Sequence _punchSequence;
    public HarvesterArmConfig Config { get { return config; } }
    public Transform Pivot { get { return armPivot != null ? armPivot : transform; } }
    public int CurrentBladeCount { get; private set; } = 1;

    private void Awake()
    {
        if (armPivot == null) armPivot = transform;
        Pivot.localRotation = Quaternion.Euler(0f, 0f, config != null ? config.rotationOffsetZ : 0f);
        CacheInitialScales();
    }

    private void OnEnable() { StartScaleAnimation(); }
    private void OnDisable() { StopScaleAnimation(); }
    private void OnDestroy() { StopScaleAnimation(); }

    private void CacheInitialScales()
    {
        _initialBladeScales = new Vector3[blades.Count];
        for (int i = 0; i < blades.Count; i++) _initialBladeScales[i] = blades[i] != null ? blades[i].localScale : Vector3.one;
    }

    private void StartScaleAnimation()
    {
        StopScaleAnimation();
        if (blades.Count == 0) return;
        float duration = config != null ? Mathf.Max(0.01f, config.punchDuration * 0.5f) : 0.1f;
        float scale = config != null ? config.harvestPunchScale : 1.15f;
        float interval = config != null ? Mathf.Max(0f, config.punchInterval) : 0.5f;
        _punchSequence = DOTween.Sequence();
        _punchSequence.Append(DOTween.To(() => 1f, ApplyBladeScale, scale, duration).SetEase(Ease.OutQuad));
        _punchSequence.Append(DOTween.To(() => scale, ApplyBladeScale, 1f, duration).SetEase(Ease.InQuad));
        _punchSequence.AppendInterval(interval);
        _punchSequence.SetLoops(-1).SetLink(gameObject);
    }

    private void StopScaleAnimation()
    {
        if (_punchSequence != null) _punchSequence.Kill();
        _punchSequence = null;
        ApplyBladeScale(1f);
    }

    private void ApplyBladeScale(float multiplier)
    {
        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null) blades[i].localScale = _initialBladeScales[i] * multiplier;
        }
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
