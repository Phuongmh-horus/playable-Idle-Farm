using System;
using UnityEngine;

[Serializable]
public enum AdjustEnvironment
{
    Sandbox,
    Production
}

[Serializable]
public enum AdjustLogLevel
{
    Verbose = 1,
    Debug,
    Info,
    Warn,
    Error,
    Assert,
    Suppress
}


[CreateAssetMenu(fileName = "AdjustConfigSO", menuName = "ScriptableObjects/AdjustConfigSO", order = 1)]
public class AdjustConfigSO : ScriptableObject
{
    public string AppToken
    {
        get
        {
#if UNITY_ANDROID
            return androidAppToken;
#elif UNITY_IOS
                return iosAppToken;
#else
                return string.Empty;
#endif
        }
    }

    public string GetAppToken(bool isAndroid) => isAndroid ? androidAppToken : iosAppToken;
    public string GetEventPrefix(bool isAndroid) => isAndroid ? androidEventPrefix : iosEventPrefix;
    public string GetIapEventName(bool isAndroid, bool isTestTracking = false)
    {
        if (isTestTracking)
        {
            string testToken = isAndroid ? androidIAPTestToken : iosIAPTestToken;
            if (!string.IsNullOrEmpty(testToken)) return testToken;
        }
        return isAndroid ? androidIapEventName : iosIapEventName;
    }

    public string EventPrefix
    {
        get
        {
#if UNITY_ANDROID
            return androidEventPrefix;
#elif UNITY_IOS
                return iosEventPrefix;
#else
                return string.Empty;
#endif
        }
    }

    public AdjustEnvironment Environment => environment;
    public AdjustLogLevel LogLevel => logLevel;
    public string IapEventName
    {
        get
        {
#if TEST_TRACKING
    #if UNITY_ANDROID
        return androidIAPTestToken;
    #endif
    #if UNITY_IOS
        return iosIAPTestToken;
    #endif
#endif
#if UNITY_ANDROID
            return androidIapEventName;
#elif UNITY_IOS
                return iosIapEventName;
#else
                return string.Empty;
#endif
        }
    }


    [SerializeField]
    private string androidAppToken;

    [SerializeField]
    private string iosAppToken;

    [SerializeField]
    private string androidEventPrefix;

    [SerializeField]
    private string iosEventPrefix;

    [SerializeField]
    private AdjustEnvironment environment;

    [SerializeField]
    private string androidIapEventName;

    [SerializeField]
    private string iosIapEventName;

    [SerializeField] private string androidIAPTestToken;
    [SerializeField] private string iosIAPTestToken;


    [SerializeField]
    private AdjustLogLevel logLevel = AdjustLogLevel.Verbose;
}

