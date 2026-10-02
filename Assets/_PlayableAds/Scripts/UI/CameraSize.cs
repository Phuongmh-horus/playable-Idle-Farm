using System.Collections;
using UnityEngine;


public class CameraSize : MonoSingleton<CameraSize>
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

    [Header("Animation")]
    [SerializeField] private float cameraMoveDuration = 0.3f;
    [SerializeField] private float sizeTweenDuration = 0.3f;


    /// <summary>
    ///  currentViewIndex = 0 là viewCamera dùng cho gameMode Grid
    ///  currentViewIndex = 1 là viewCamera dùng cho gameMode Column và CTAConfig.MarioPipe = true 
    /// </summary>
    public int currentViewIndex = 0;

    private Coroutine moveRoutine;
    private Coroutine sizeRoutine;
    private Coroutine fovRoutine;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnDisable()
    {
        CancelAnimations();
    }

    protected override void OnDestroy()
    {
        CancelAnimations();
        base.OnDestroy();
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

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveCameraRoutine(views[currentViewIndex].pos));

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

        if (sizeRoutine != null) StopCoroutine(sizeRoutine);
        sizeRoutine = StartCoroutine(OrthographicSizeRoutine(finalSize));
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
        if (fovRoutine != null) StopCoroutine(fovRoutine);
        fovRoutine = StartCoroutine(FieldOfViewRoutine(views[currentViewIndex].fieldOfView));
    }

    private IEnumerator MoveCameraRoutine(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float duration = Mathf.Max(0f, cameraMoveDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = RuntimeEaseUtility.Evaluate(RuntimeEase.OutQuad, elapsed / duration);
            transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
            yield return null;
        }
        transform.position = targetPosition;
        moveRoutine = null;
    }

    private IEnumerator OrthographicSizeRoutine(float targetSize)
    {
        float startSize = cameraU.orthographicSize;
        float duration = Mathf.Max(0f, sizeTweenDuration);
        float elapsed = 0f;
        while (elapsed < duration && cameraU != null)
        {
            elapsed += Time.deltaTime;
            float progress = RuntimeEaseUtility.Evaluate(RuntimeEase.OutQuad, elapsed / duration);
            cameraU.orthographicSize = Mathf.LerpUnclamped(startSize, targetSize, progress);
            yield return null;
        }
        if (cameraU != null) cameraU.orthographicSize = targetSize;
        sizeRoutine = null;
    }

    private IEnumerator FieldOfViewRoutine(float targetFov)
    {
        float startFov = cameraU.fieldOfView;
        float duration = Mathf.Max(0f, sizeTweenDuration);
        float elapsed = 0f;
        while (elapsed < duration && cameraU != null)
        {
            elapsed += Time.deltaTime;
            float progress = RuntimeEaseUtility.Evaluate(RuntimeEase.OutQuad, elapsed / duration);
            cameraU.fieldOfView = Mathf.LerpUnclamped(startFov, targetFov, progress);
            yield return null;
        }
        if (cameraU != null) cameraU.fieldOfView = targetFov;
        fovRoutine = null;
    }

    private void CancelAnimations()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        if (sizeRoutine != null) StopCoroutine(sizeRoutine);
        if (fovRoutine != null) StopCoroutine(fovRoutine);
        moveRoutine = null;
        sizeRoutine = null;
        fovRoutine = null;
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
