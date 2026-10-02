using UnityEngine;

public enum RuntimeEase
{
    Unset = 0,
    Linear = 1,
    InSine = 2,
    OutSine = 3,
    InOutSine = 4,
    InQuad = 5,
    OutQuad = 6,
    InOutQuad = 7,
    InCubic = 8,
    OutCubic = 9,
    InOutCubic = 10,
    InQuart = 11,
    OutQuart = 12,
    InOutQuart = 13,
    InQuint = 14,
    OutQuint = 15,
    InOutQuint = 16,
    InExpo = 17,
    OutExpo = 18,
    InOutExpo = 19,
    InCirc = 20,
    OutCirc = 21,
    InOutCirc = 22,
    InElastic = 23,
    OutElastic = 24,
    InOutElastic = 25,
    InBack = 26,
    OutBack = 27,
    InOutBack = 28,
    InBounce = 29,
    OutBounce = 30,
    InOutBounce = 31
}

public static class RuntimeEaseUtility
{
    private const float BackOvershoot = 1.70158f;

    public static float Evaluate(RuntimeEase ease, float value)
    {
        float t = Mathf.Clamp01(value);
        switch (ease)
        {
            case RuntimeEase.InSine: return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            case RuntimeEase.OutSine: return Mathf.Sin(t * Mathf.PI * 0.5f);
            case RuntimeEase.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
            case RuntimeEase.InQuad: return t * t;
            case RuntimeEase.OutQuad: return 1f - (1f - t) * (1f - t);
            case RuntimeEase.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
            case RuntimeEase.InCubic: return t * t * t;
            case RuntimeEase.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
            case RuntimeEase.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
            case RuntimeEase.InQuart: return t * t * t * t;
            case RuntimeEase.OutQuart: return 1f - Mathf.Pow(1f - t, 4f);
            case RuntimeEase.InOutQuart: return t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f;
            case RuntimeEase.InQuint: return t * t * t * t * t;
            case RuntimeEase.OutQuint: return 1f - Mathf.Pow(1f - t, 5f);
            case RuntimeEase.InOutQuint: return t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) * 0.5f;
            case RuntimeEase.InExpo: return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
            case RuntimeEase.OutExpo: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
            case RuntimeEase.InOutExpo:
                if (t <= 0f || t >= 1f) return t;
                return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
            case RuntimeEase.InCirc: return 1f - Mathf.Sqrt(1f - t * t);
            case RuntimeEase.OutCirc: return Mathf.Sqrt(1f - (t - 1f) * (t - 1f));
            case RuntimeEase.InOutCirc:
                return t < 0.5f
                    ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) * 0.5f
                    : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) * 0.5f;
            case RuntimeEase.InElastic:
                if (t <= 0f || t >= 1f) return t;
                return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * (2f * Mathf.PI / 3f));
            case RuntimeEase.OutElastic:
                if (t <= 0f || t >= 1f) return t;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
            case RuntimeEase.InOutElastic:
                if (t <= 0f || t >= 1f) return t;
                return t < 0.5f
                    ? -Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * (2f * Mathf.PI / 4.5f)) * 0.5f
                    : Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * (2f * Mathf.PI / 4.5f)) * 0.5f + 1f;
            case RuntimeEase.InBack: return (BackOvershoot + 1f) * t * t * t - BackOvershoot * t * t;
            case RuntimeEase.OutBack:
                float outBackT = t - 1f;
                return 1f + (BackOvershoot + 1f) * outBackT * outBackT * outBackT + BackOvershoot * outBackT * outBackT;
            case RuntimeEase.InOutBack:
                float scaledOvershoot = BackOvershoot * 1.525f;
                return t < 0.5f
                    ? Mathf.Pow(2f * t, 2f) * ((scaledOvershoot + 1f) * 2f * t - scaledOvershoot) * 0.5f
                    : (Mathf.Pow(2f * t - 2f, 2f) * ((scaledOvershoot + 1f) * (t * 2f - 2f) + scaledOvershoot) + 2f) * 0.5f;
            case RuntimeEase.InBounce: return 1f - OutBounce(1f - t);
            case RuntimeEase.OutBounce: return OutBounce(t);
            case RuntimeEase.InOutBounce: return t < 0.5f ? (1f - OutBounce(1f - 2f * t)) * 0.5f : (1f + OutBounce(2f * t - 1f)) * 0.5f;
            default: return t;
        }
    }

    private static float OutBounce(float t)
    {
        const float n = 7.5625f;
        const float d = 2.75f;
        if (t < 1f / d) return n * t * t;
        if (t < 2f / d) { t -= 1.5f / d; return n * t * t + 0.75f; }
        if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
        t -= 2.625f / d;
        return n * t * t + 0.984375f;
    }
}

public abstract class AnimationStateBase : MonoBehaviour
{
    [SerializeField] protected float duration = 0.7f;
    [SerializeField] protected RuntimeEase ease = RuntimeEase.OutBack;
    public abstract void PlayState();
    public abstract void PlayStateSmooth();
    public abstract void SetupState(int stateIndex);
    public abstract void SetupStateSmooth(int stateIndex);
}
