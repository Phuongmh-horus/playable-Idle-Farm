using UnityEngine;

[CreateAssetMenu(fileName = "DatabucketsTrackingConfigSO", menuName = "ScriptableObjects/DatabucketsTrackingConfigSO", order = 1)]
public class DatabucketsTrackingConfig : ScriptableObject
{
    public string ApiEndpoint
    {
        get
        {
#if UNITY_ANDROID
            return androidApiEndpoint;
#elif UNITY_IOS
            return iosApiEndpoint;
#else
            return string.Empty;
#endif
        }
    }

    public string ApiKey
    {
        get
        {
#if DATABUCKETS_TEST
            return testApiKey;
#else
#if UNITY_ANDROID
            return androidApiKey;
#elif UNITY_IOS
            return iosApiKey;
#else
            return string.Empty;
#endif
#endif
        }
    }

    [SerializeField]
    private string androidApiEndpoint;

    [SerializeField]
    private string iosApiEndpoint;

    [SerializeField]
    private string androidApiKey;

    [SerializeField]
    private string iosApiKey;

    [SerializeField]
    private string testApiKey;
}
