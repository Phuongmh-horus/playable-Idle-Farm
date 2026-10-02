using Firebase.Analytics;
using Firebase.Crashlytics;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gom log chẩn đoán cho hệ thống save/load.
///
/// Vì sao cần lớp riêng thay vì gọi thẳng Firebase:
/// 1. <c>DataManager.InitData()</c> chạy TRƯỚC <c>FirebaseManager.StartInitFirebaseApp()</c>,
///    nên mọi sự kiện lúc load data đều rơi vào giai đoạn Firebase chưa sẵn sàng và bị bỏ im lặng —
///    đúng những sự kiện quan trọng nhất lại là những sự kiện bị mất. Ở đây ta đệm lại rồi
///    flush sau khi Firebase init xong.
/// 2. Gọi Crashlytics khi chưa init có thể ném exception. Các lời gọi đó nằm trong khối catch
///    của luồng load, nên một exception ở đó sẽ thoát ra ngoài và làm hỏng luôn quá trình khởi động.
///    Mọi thứ trong lớp này đều được bọc try/catch: log không bao giờ được phép làm chết game.
/// </summary>
public static class SaveDiagnostics
{
    /// <summary>Tên event trên Firebase Analytics. Lọc theo tham số "stage" để biết nguyên nhân.</summary>
    public const string EventName = "save_data_diag";

    #region Stage - mỗi giá trị là một nhánh có thật trong luồng save/load

    // --- Load ---
    /// <summary>Đọc file chính thành công. Đây là đường bình thường.</summary>
    public const string StageLoadOk = "load_ok";
    /// <summary>File chính hỏng/mất, cứu được từ .bak. Đếm cái này = số lần cơ chế backup đã cứu user.</summary>
    public const string StageLoadRecoveredBak = "load_recovered_bak";
    /// <summary>Cứu được từ .tmp (app bị kill giữa lúc swap file).</summary>
    public const string StageLoadRecoveredTmp = "load_recovered_tmp";
    /// <summary>Máy đã từng có data nhưng không đọc được bản nào. Đây là nhóm user đang mất data.</summary>
    public const string StageLoadFailed = "load_failed";
    /// <summary>Máy chưa từng có data — user mới thật sự.</summary>
    public const string StageLoadNewUser = "load_new_user";
    /// <summary>Không còn file nào nhưng PlayerPrefs vẫn có dấu account cũ (nghi user dính bug bản trước).</summary>
    public const string StageLoadMissingMarker = "load_missing_marker";

    // --- Save ---
    /// <summary>Chặn ghi payload null (bug ghi đè bằng 1 byte marker null).</summary>
    public const string StageSaveNullPayload = "save_null_payload";
    /// <summary>File.Replace thất bại, phải dùng nhánh Copy dự phòng.</summary>
    public const string StageSaveReplaceFallback = "save_replace_fallback";
    /// <summary>Ghi file thất bại.</summary>
    public const string StageSaveError = "save_error";
    /// <summary>Bỏ qua lệnh ghi vì phiên này load lỗi — tránh ghi đè tiến trình thật.</summary>
    public const string StageSaveBlocked = "save_blocked";

    // --- Recovery ---
    /// <summary>Local hỏng, đã kéo data từ server về thành công.</summary>
    public const string StageRestoredFromServer = "restored_from_server";
    /// <summary>Local hỏng, đang offline nên chưa cứu được — chờ phiên sau.</summary>
    public const string StageRestoreDeferredOffline = "restore_deferred_offline";
    /// <summary>Local hỏng và server xác nhận không có account. Đã cách ly file, chơi lại từ đầu.</summary>
    public const string StageQuarantined = "quarantined";

    #endregion

    private struct PendingEvent
    {
        public string Stage;
        public string Reason;
        public string Detail;
        public string Storage;
    }

    // Số event tối đa giữ lại trước khi Firebase sẵn sàng. Luồng boot chỉ sinh vài event,
    // giới hạn này chỉ để phòng trường hợp có vòng lặp gọi ngoài dự tính.
    private const int MaxPending = 20;
    private static readonly List<PendingEvent> _pending = new();
    private static bool _flushed;

    /// <summary>
    /// Ghi nhận một sự kiện save/load. An toàn khi gọi ở bất kỳ thời điểm nào,
    /// kể cả trước khi Firebase init.
    /// </summary>
    /// <param name="stage">Một trong các hằng Stage* ở trên.</param>
    /// <param name="reason">Phân loại ngắn, thường là tên kiểu exception.</param>
    /// <param name="detail">Chi tiết thêm (tên file, message đã cắt ngắn).</param>
    /// <param name="storage">Ảnh chụp trạng thái file, xem <c>LocalDatabaseManager.GetStorageSnapshot</c>.</param>
    public static void Report(string stage, string reason = null, string detail = null, string storage = null)
    {
        try
        {
            var evt = new PendingEvent
            {
                Stage = Trim(stage, 40),
                Reason = Trim(reason, 100),
                Detail = Trim(detail, 100),
                Storage = Trim(storage, 100),
            };

            Debug.Log($"[SaveDiag] {evt.Stage} reason={evt.Reason} detail={evt.Detail} storage={evt.Storage}");

            // Crashlytics: log dạng breadcrumb + custom key để mở crash report ra là thấy ngay
            // trạng thái save của user đó.
            SafeCrashlyticsLog($"[SaveDiag] {evt.Stage} | {evt.Reason} | {evt.Detail} | {evt.Storage}");
            SafeCrashlyticsKey("save_last_stage", evt.Stage);
            if (!string.IsNullOrEmpty(evt.Storage)) SafeCrashlyticsKey("save_storage", evt.Storage);

            if (_flushed)
            {
                Send(evt);
                return;
            }

            if (_pending.Count < MaxPending) _pending.Add(evt);
        }
        catch (Exception e)
        {
            // Không bao giờ để việc ghi log làm hỏng luồng save/load.
            Debug.LogError($"[SaveDiag] Report error: {e.Message}");
        }
    }

    /// <summary>Tiện ích cho nhánh catch: tự lấy tên kiểu exception làm reason.</summary>
    public static void ReportException(string stage, Exception ex, string detail = null, string storage = null)
    {
        Report(stage, ex?.GetType().Name, detail ?? ex?.Message, storage);
        SafeCrashlyticsException(ex);
    }

    /// <summary>
    /// Gửi hết các event đã đệm. Gọi sau khi Firebase init xong
    /// (xem <c>FirebaseManager.OnInitialized</c>).
    /// </summary>
    public static void Flush()
    {
        try
        {
            _flushed = true;
            for (int i = 0; i < _pending.Count; i++) Send(_pending[i]);

            // Các event đệm được ghi khi Crashlytics chưa sẵn sàng nên breadcrumb đã bị bỏ qua.
            // Đặt lại custom key theo event cuối để crash report sau này còn thấy trạng thái save.
            if (_pending.Count > 0)
            {
                var last = _pending[_pending.Count - 1];
                SafeCrashlyticsKey("save_last_stage", last.Stage);
                SafeCrashlyticsKey("save_storage", last.Storage);
            }

            _pending.Clear();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveDiag] Flush error: {e.Message}");
        }
    }

    private static void Send(PendingEvent evt)
    {
        try
        {
            FirebaseManager.FirebaseLogEvent(EventName,
                new Parameter("stage", evt.Stage ?? string.Empty),
                new Parameter("reason", evt.Reason ?? string.Empty),
                new Parameter("detail", evt.Detail ?? string.Empty),
                new Parameter("storage", evt.Storage ?? string.Empty),
                new Parameter("app_version", AppVersion));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveDiag] Send error: {e.Message}");
        }
    }

    // Application.version là API chỉ gọi được từ main thread. Report có thể được gọi sau
    // một await I/O nên không chắc còn ở main thread — cache lại từ lần gọi đầu (lúc boot).
    private static string _appVersion;

    private static string AppVersion
    {
        get
        {
            if (!string.IsNullOrEmpty(_appVersion)) return _appVersion;
            try
            {
                _appVersion = Application.version;
            }
            catch
            {
                _appVersion = "unknown";
            }

            return _appVersion;
        }
    }

    private static string Trim(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        value = value.Replace('\n', ' ').Replace('\r', ' ');
        return value.Length <= max ? value : value.Substring(0, max);
    }

    #region Firebase wrappers - tất cả đều nuốt lỗi

    private static void SafeCrashlyticsLog(string message)
    {
        try
        {
            if (!FirebaseManager.IsInitialized) return;
            Crashlytics.Log(message);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveDiag] Crashlytics.Log unavailable: {e.Message}");
        }
    }

    private static void SafeCrashlyticsKey(string key, string value)
    {
        try
        {
            if (!FirebaseManager.IsInitialized) return;
            Crashlytics.SetCustomKey(key, value ?? string.Empty);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveDiag] Crashlytics.SetCustomKey unavailable: {e.Message}");
        }
    }

    private static void SafeCrashlyticsException(Exception ex)
    {
        try
        {
            if (ex == null) return;
            if (!FirebaseManager.IsInitialized) return;
            Crashlytics.LogException(ex);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveDiag] Crashlytics.LogException unavailable: {e.Message}");
        }
    }

    #endregion
}
