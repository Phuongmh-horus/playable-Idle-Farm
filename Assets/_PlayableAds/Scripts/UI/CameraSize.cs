

using DG.Tweening;
using UnityEngine;


public class CameraSize : Singleton<CameraSize>
{
    [Header("Views")]
    [SerializeField] private ViewCamera[] viewCameraHori;
    [SerializeField] private ViewCamera[] viewCameraVerti;

    [Header("Camera")]
    [SerializeField] private Camera cameraU;

    [Header("Reference Aspect")]
    [SerializeField] private float verticalReferenceWidth = 9f;
    [SerializeField] private float verticalReferenceHeight = 16f;

    [SerializeField] private float horizontalReferenceWidth = 3f;
    [SerializeField] private float horizontalReferenceHeight = 4f;

    [Header("Clamp Size")]
    [SerializeField] private bool useClampSize = true;
    [SerializeField] private float minOrthoSize = 15f;
    [SerializeField] private float maxOrthoSize = 35f;

    [Header("Tween")]
    [SerializeField] private float cameraMoveDuration = 0.3f;
    [SerializeField] private float sizeTweenDuration = 0.3f;


    /// <summary>
    ///  currentViewIndex = 0 là viewCamera dùng cho gameMode Grid
    ///  currentViewIndex = 1 là viewCamera dùng cho gameMode Column và CTAConfig.MarioPipe = true 
    /// </summary>
    public int currentViewIndex = 0;

    private Tween moveTween;
    private Tween sizeTween;
    private Tween fovTween;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        moveTween?.Kill();
        sizeTween?.Kill();
        fovTween?.Kill();
    }

    private void Start()
    {
        // OnChangeScreen();   
    }

    private bool IsHorizontalReference()
    {
        float currentAspect = Screen.width / (float)Screen.height;

        float verticalAspect = verticalReferenceWidth / verticalReferenceHeight;
        float horizontalAspect = horizontalReferenceWidth / horizontalReferenceHeight;

        return Mathf.Abs(currentAspect - horizontalAspect)
            < Mathf.Abs(currentAspect - verticalAspect);
    }

    public void OnChangeScreen(int index = 0)
    {
    if (IsHorizontalReference())
        {
            Debug.Log("[CameraSize]Horizontal Reference");
            SetCamera(viewCameraHori, index);
        }
        else
        {
            Debug.Log("[CameraSize]Vertical Reference");
            SetCamera(viewCameraVerti,index);
        }
    }


    [ContextMenu("Set Camera")]
    public void SetCameraVerti()
    {
        SetCamera(viewCameraVerti, currentViewIndex);
    }



    public void SetCamera(ViewCamera[] views, int index)
    {
        if (cameraU == null)
        {
            return;
        }

        if (views == null || views.Length == 0)
        {
            return;
        }
        currentViewIndex = index;
        if (currentViewIndex < 0 || currentViewIndex >= views.Length)
        {
            currentViewIndex = 0;
        }

        moveTween?.Kill();

        moveTween = transform
        .DOMove(views[currentViewIndex].pos, cameraMoveDuration)
        .SetEase(Ease.OutQuad);

        if (cameraU.orthographic)
        {
            OnOrthographicSize(views);
        }
        else
        {
            OnFieldOfView(views);
        }
    }

    private void OnOrthographicSize(ViewCamera[] views)
    {
        float baseSize = views[currentViewIndex].fieldOfView;
        float finalSize = GetAspectCorrectedOrthoSize(baseSize);

        if (useClampSize)
        {
            finalSize = Mathf.Clamp(finalSize, minOrthoSize, maxOrthoSize);
        }

        sizeTween?.Kill();

        sizeTween = DOTween.To(
            () => cameraU.orthographicSize,
            x => cameraU.orthographicSize = x,
            finalSize,
            sizeTweenDuration
            ).SetEase(Ease.OutQuad);
    }

    private float GetAspectCorrectedOrthoSize(float baseSize)
    {
        float currentAspect = Screen.width / (float)Screen.height;

        float referenceAspect;


        // referenceAspect = horizontalReferenceWidth / horizontalReferenceHeight;
        if(IsHorizontalReference())
        {
            referenceAspect = horizontalReferenceWidth / horizontalReferenceHeight;
        }
        else
        {
            referenceAspect = verticalReferenceWidth / verticalReferenceHeight;
        }

        return baseSize * (referenceAspect / currentAspect);
    }

    private void OnFieldOfView(ViewCamera[] views)
    {
    fovTween?.Kill();

    fovTween = DOTween.To(
    () => cameraU.fieldOfView,
    x => cameraU.fieldOfView = x,
    views[currentViewIndex].fieldOfView,
    sizeTweenDuration
    ).SetEase(Ease.OutQuad);
    }

    #if UNITY_EDITOR
    private void Update()
    {
    // Test nhanh trong Editor khi đổi Game View aspect.
    if (Input.GetKeyDown(KeyCode.R))
    {
        OnChangeScreen();
    }
    }
    #endif
    }

[System.Serializable]
public class ViewCamera
{
public Vector3 pos;

// Nếu camera Orthographic thì fieldOfView này đang được dùng như orthographicSize.
public float fieldOfView;
}