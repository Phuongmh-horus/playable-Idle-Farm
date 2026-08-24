
using UnityEngine;
using DG.Tweening;
public class HandEndCard : MonoBehaviour
{
    [SerializeField] private bool isWinningEndCard = true;

    [Header("Win End Card")]
    [SerializeField] private RectTransform transform1ToMove;

    [SerializeField] private RectTransform transform2ToMove;

    [SerializeField] private float moveDuration = 1f;

    [SerializeField] private Vector2 offset = new Vector2(0f, 0f);

    // [SerializeField] private float waitTime = 0.5f;


    [Header("Lose End Card")]
    [SerializeField] private RectTransform transformToMove;
    // [SerializeField] private float PulseScale = 1.1f;


    private Sequence _moveSequence;
    private Sequence _pulseSequence;

    private Vector3 GetWorldOffset()
    {
        // Multiply the offset by lossyScale to make it resolution-independent
        return new Vector3(offset.x * transform.lossyScale.x, offset.y * transform.lossyScale.y, 0f);
    }

    protected void Start()
    {
        
    }
    void OnEnable()
    {
        if (isWinningEndCard)
        {        
            Vector3 worldOffset = GetWorldOffset();
            Vector3 transform1ToMovePosition = transform1ToMove.position + worldOffset;
            Vector3 transform2ToMovePosition = transform2ToMove.position + worldOffset;
            transform.position = transform1ToMovePosition;

            _moveSequence = DOTween.Sequence()
                .Append(transform.DOMove(transform2ToMovePosition, moveDuration).SetEase(Ease.InOutSine))
                .AppendCallback(() => AnimationClickButton(transform2ToMove))
                .AppendInterval(pulseHandDuration)
                .Append(transform.DOMove(transform1ToMovePosition, moveDuration).SetEase(Ease.InOutSine))
                .AppendCallback(() => AnimationClickButton(transform1ToMove))
                .AppendInterval(pulseHandDuration)
                .SetLoops(-1, LoopType.Restart);
        }
        else
        {
            transform.position = transformToMove.position + GetWorldOffset();
            _moveSequence = DOTween.Sequence()
                .AppendCallback(() => AnimationClickButton(transformToMove))
                .AppendInterval(pulseHandDuration)
                .SetLoops(-1, LoopType.Restart);
        }
    }
    
    void OnDisable()
    {
        if (_moveSequence != null) _moveSequence.Kill();
        if (_pulseSequence != null) _pulseSequence.Kill();
    }



    [Header("Click Button Animation")]
    [SerializeField] private float rotateHand = 15f;
    [SerializeField] private float pulseHandScale = 1.2f;
    [SerializeField] private float startHandScale = 1f;

    [SerializeField] private float pulseHandDuration = 1f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float StartScale = 0.9f;


    
    public void AnimationClickButton(Transform buttonTransform)
    {
        if (_pulseSequence != null) _pulseSequence.Kill();
        transform.position = buttonTransform.position + GetWorldOffset();
        buttonTransform.localScale = Vector3.one * normalScale;
        _pulseSequence = DOTween.Sequence()
            .Append(transform.DORotate(new Vector3(0f, 0f, rotateHand), pulseHandDuration / 2f).SetEase(Ease.InOutSine))
            .Join(transform.DOScale(pulseHandScale, pulseHandDuration / 2f).SetEase(Ease.InOutSine))
            .Join(buttonTransform.DOScale(StartScale, pulseHandDuration / 2f).SetEase(Ease.InOutSine))
            // .Append(buttonTransform.DORotate(new Vector3(0f, 0f, -rotateHand), pulseHandDuration).SetEase(Ease.InOutSine))
            .Append(transform.DORotate(new Vector3(0f, 0f, 0f), pulseHandDuration / 2f).SetEase(Ease.InOutSine))
            .Join(transform.DOScale(startHandScale, pulseHandDuration / 2f).SetEase(Ease.InOutSine))
            .Join(buttonTransform.DOScale(normalScale, pulseHandDuration / 2f).SetEase(Ease.InOutSine));
    }



}
