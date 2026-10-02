using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Lớp xử lý kiểm tra và cập nhật phiên bản game
/// </summary>
public class CheckGameUpdate : UIEventBase
{
    [SerializeField] private VersionDataSO VersionDataSO;

    private static bool HasShowUpdateLayer = false;
    private bool IsNewVersion = false;
    /// <summary>
    /// Khởi tạo và kiểm tra phiên bản khi đối tượng được tạo
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

    }

    /// <summary>
    /// Kiểm tra phiên bản mới trên cửa hàng ứng dụng tương ứng với nền tảng
    /// </summary>
    private void CheckVersion()
    {
        try
        {
            var updateVersions = ParseUpdateVersions(FirebaseManager.Instance?.GetUpdateVersionValue() ?? "");
            IsNewVersion = CheckUpdate(updateVersions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CheckGameUpdate] Lỗi khi lấy thông tin cập nhật: {ex.Message}\nStackTrace: {ex.StackTrace}");

        }
    }

    private List<VersionDataSO> ParseUpdateVersions(string zipData)
    {
        var list = new List<VersionDataSO>();

        if (string.IsNullOrEmpty(zipData))
            return list;

        string trimmed = zipData
            .Trim()
            .TrimStart('{')
            .TrimEnd('}');

        // Tách theo dấu phẩy
        var pairs = trimmed.Split(',');

        foreach (var pair in pairs)
        {
            // Ví dụ: "0.0.1.3(14)"
            string p = pair.Trim();
            if (string.IsNullOrEmpty(p))
                continue;

            // Tìm dấu "(" và ")"
            int open = p.IndexOf('(');
            int close = p.IndexOf(')');

            if (open < 0 || close < 0 || close <= open)
                continue;

            // Lấy version và code
            string bundleVersion = p.Substring(0, open).Trim();
            string codeStr = p.Substring(open + 1, close - open - 1).Trim();

            if (!int.TryParse(codeStr, out int versionCode))
                continue;

            var data = ScriptableObject.CreateInstance<VersionDataSO>();
            data.BundleVersion = bundleVersion;
            data.BundleVersionCode = versionCode;

            list.Add(data);
        }

        return list;
    }


    /// <summary>
    /// Kiểm tra điều kiện hiển thị popup cập nhật
    /// </summary>
    /// <returns>True nếu cần hiển thị popup cập nhật, ngược lại trả về false</returns>
    public override UniTask<bool> CheckRequire()
    {
        if (GameConfig.NoInternet())
            return UniTask.FromResult(false);
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.GetRemoteConfigValueComplete)
            return UniTask.FromResult(false);
        CheckVersion();
        Debug.Log($"[CheckGameUpdate] Có cập nhật: {IsNewVersion}");
        return UniTask.FromResult(IsNewVersion && !HasShowUpdateLayer);
    }

    /// <summary>
    /// Kiểm tra cập nhật trên Android sử dụng Google Play Core 1.8.5
    /// </summary>
    /// <returns>True nếu có phiên bản mới hơn trên cửa hàng</returns>
    private bool CheckUpdate(List<VersionDataSO> updateVersions)
    {
        try
        {
            Debug.Log($"[CheckGameUpdate] Kiem tra thong tin cap nhat not successful");
            foreach (var updateVersion in updateVersions)
            {
                if (updateVersion.BundleVersion.Equals(VersionDataSO.BundleVersion)
                    && updateVersion.BundleVersionCode == VersionDataSO.BundleVersionCode) return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CheckGameUpdate] General Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Hiển thị popup thông báo cập nhật
    /// </summary>
    public override void Play()
    {
        HasShowUpdateLayer = true;

        LayerManager.Instance.ShowCheckUpdateLayer();
    }

    /// <summary>
    /// Mở cửa hàng ứng dụng tương ứng với nền tảng
    /// </summary>
    private void GotoStore()
    {
        string webUrl = string.Empty;
        string url = string.Empty;

#if UNITY_ANDROID
        url = $"market://details?id={Application.identifier}";
        webUrl = $"https://play.google.com/store/apps/details?id={Application.identifier}";
        // Fallback URL nếu không thể mở Google Play Store

#elif UNITY_IOS
            url = $"itms-apps://itunes.apple.com/app/id{DataManager.IOSAppId}";
            // Fallback URL cho iOS
            webUrl = $"https://apps.apple.com/app/id{DataManager.IOSAppId}";

#endif
        try
        {
            Debug.Log($"[CheckGameUpdate] Đang mở cửa hàng ứng dụng: {url}");
            if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CheckGameUpdate] Lỗi khi mở cửa hàng ứng dụng: {e.Message}");
            try
            {
                if (!string.IsNullOrEmpty(webUrl)) Application.OpenURL(webUrl);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CheckGameUpdate] Lỗi khi mở cửa hàng ứng dụng: {exception.Message}");
            }
        }
    }
}
