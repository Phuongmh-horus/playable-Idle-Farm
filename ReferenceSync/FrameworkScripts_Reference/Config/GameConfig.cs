using System.Runtime.InteropServices;
using UnityEngine;

public static class GameConfig
{
    // DeviceId được đọc ở header của MỌI HTTP request, nên phải cache trong RAM.
    // Nếu không, mỗi lần gọi sẽ chạm PlayerPrefs + đọc file.
    private static string _cachedDeviceId;

    public static string DeviceId
    {
        get
        {
            if (string.IsNullOrEmpty(_cachedDeviceId)) _cachedDeviceId = GetDeviceID();
            return _cachedDeviceId;
        }
    }

#if UNITY_IOS
    [DllImport("__Internal")]
    static extern string _Get_Device_id();
#endif
    private static string deviceKey = "device_id";

    /// <summary>
    /// Bản sao deviceId trên file. PlayerPrefs (SharedPreferences trên Android) có thể mất
    /// trắng khi máy tắt đột ngột; mất deviceId đồng nghĩa tra cứu account trên server trả 404
    /// và user bị coi là người mới. Giữ thêm một bản trên đĩa để danh tính không phụ thuộc
    /// vào một nguồn duy nhất.
    /// </summary>
    private static string DeviceIdFilePath => $"{Application.persistentDataPath}/device_id.txt";

    private static string ReadDeviceIdFile()
    {
        try
        {
            if (System.IO.File.Exists(DeviceIdFilePath))
            {
                return System.IO.File.ReadAllText(DeviceIdFilePath).Trim();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ReadDeviceIdFile error: {e}");
        }

        return string.Empty;
    }

    private static void PersistDeviceId(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (PlayerPrefs.GetString(deviceKey, string.Empty) != id)
        {
            PlayerPrefs.SetString(deviceKey, id);
            PlayerPrefs.Save();
        }

        try
        {
            if (ReadDeviceIdFile() != id)
            {
                System.IO.File.WriteAllText(DeviceIdFilePath, id);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PersistDeviceId error: {e}");
        }
    }

    private static string GetDeviceID()
    {
        if (PlayerPrefs.HasKey(deviceKey))
        {
            var saved = PlayerPrefs.GetString(deviceKey);
            PersistDeviceId(saved); // đảm bảo bản trên file luôn tồn tại
#if TEST_TRACKING
return "TEST_TRACKING_"+ saved;
#endif
            return saved;
        }

        // PlayerPrefs mất nhưng file còn -> khôi phục đúng danh tính cũ thay vì tạo mới.
        var fromFile = ReadDeviceIdFile();
        if (!string.IsNullOrEmpty(fromFile))
        {
            PersistDeviceId(fromFile);
#if TEST_TRACKING
return "TEST_TRACKING_"+ fromFile;
#endif
            return fromFile;
        }

        var _id = string.Empty;
#if UNITY_IPHONE && !UNITY_EDITOR
		_id = _Get_Device_id();
#elif UNITY_ANDROID && !UNITY_EDITOR
        _id = SystemInfo.deviceUniqueIdentifier;
#elif UNITY_EDITOR
        _id = SystemInfo.deviceUniqueIdentifier;
#else
        _id = SystemInfo.deviceUniqueIdentifier;
#endif

        _id = (_id ?? string.Empty).Trim(); // ✅ Xử lý dấu cách, ký tự trắng thừa (native call có thể trả null)

        // SystemInfo.deviceUniqueIdentifier có thể trả Unsupported/rỗng trên một số máy;
        // để rỗng thì mọi máy như vậy dùng chung một "account" trên server.
        if (string.IsNullOrEmpty(_id) || _id == SystemInfo.unsupportedIdentifier)
        {
            _id = System.Guid.NewGuid().ToString("N");
        }
#if TEST_TRACKING
        return "TEST_TRACKING_"+ _id;
#endif
        PersistDeviceId(_id);
        return _id;
    }

    /// <summary>
    /// False: có internet, True: không có internet
    /// </summary>
    /// <returns></returns>
    public static bool NoInternet() // đừng check trong update nhé
    {
        //False = có internet , True = không có internet
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
       return Application.internetReachability.Equals(NetworkReachability.NotReachable);
#endif
        return false;
    }
}
