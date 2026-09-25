using System;
using DG.Tweening;
using UnityEngine;

public class AnimatedFruit : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AnimatedFruitConfig config;

    private Sequence _flightSequence;
    private Action _onArrived;

    private AnimatedFruitConfig Config
    {
        get { return config != null ? config : ConfigHolder.Instance.FruitConfig.animatedFruitConfig; }
    }

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnDisable() { CancelMotion(); }

    public void Fly(Vector3 startPosition, Transform targetTransform, Sprite fruitSprite, Action onArrived = null)
    {
        CancelMotion();
        _onArrived = onArrived;
        if (spriteRenderer != null) spriteRenderer.sprite = fruitSprite;

        AnimatedFruitConfig cfg = Config;
        float popRadius = cfg != null ? cfg.popRadius : 0.6f;
        Vector2 random = UnityEngine.Random.insideUnitCircle;
        if (random.sqrMagnitude < 0.01f) random = Vector2.right;
        Vector3 popPosition = startPosition + new Vector3(random.x, 0f, random.y) * popRadius;
        float initialScale = cfg != null ? cfg.popInitialScale : 0f;
        float peakScale = cfg != null ? cfg.popPeakScale : 0.42f;
        float flightStartScale = cfg != null ? cfg.startScale : 0.35f;
        float endScale = cfg != null ? cfg.endScale : 0.25f;

        transform.position = startPosition;
        transform.localScale = Vector3.one * initialScale;
        _flightSequence = DOTween.Sequence();
        _flightSequence.Append(transform.DOMove(popPosition, cfg != null ? cfg.popDuration : 0.25f).SetEase(cfg != null ? cfg.popEase : Ease.OutQuad));
        _flightSequence.Join(transform.DOScale(peakScale, (cfg != null ? cfg.popDuration : 0.25f) * 0.5f).SetLoops(2, LoopType.Yoyo));
        _flightSequence.Append(transform.DOMove(targetTransform != null ? targetTransform.position : popPosition, cfg != null ? cfg.flightDuration : 0.55f).SetEase(cfg != null ? cfg.flightEase : Ease.InQuad));
        _flightSequence.Join(transform.DOScale(endScale, cfg != null ? cfg.flightDuration : 0.55f).SetEase(Ease.InQuad));
        _flightSequence.OnComplete(CompleteFlight);
    }

    private void CompleteFlight()
    {
        Action callback = _onArrived;
        _onArrived = null;
        callback?.Invoke();
    }

    public void CancelMotion()
    {
        if (_flightSequence != null) _flightSequence.Kill();
        _flightSequence = null;
        _onArrived = null;
    }
}
