#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using BulletStrike;

#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

using UnityEngine;

public enum NotificationIcon
{
    Bell,
    Clock,
    Event,
    Heart,
    Message,
    Star
}

public class NotificationParams
{
    /// <summary>
    /// Use random id for each new notification.
    /// </summary>
    public int Id;
    public TimeSpan Delay;
    public string Title;
    public string Message;
    public string Ticker;
    public bool Sound = true;
    public bool Vibrate = true;
    public bool Light = true;
    public NotificationIcon SmallIcon;
    public Color SmallIconColor;
    /// <summary>
    /// Use "" for simple notification. Use "app_icon" to use the app icon. Use custom value but first place image to "simple-android-notifications.aar/res/". To modify "aar" file just rename it to "zip" and back.
    /// </summary>
    public string LargeIcon;
}

public class NotificationManager : MonoBehaviour
{
    #region Singleton

    private static NotificationManager _notificationManager = null;

    public static NotificationManager Instance
    {
        get
        {
            if (_notificationManager == null)
                _notificationManager = FindObjectOfType<NotificationManager>();
            return _notificationManager;
        }
    }

    #endregion

    #region Properties

    public const string ChannelId = "game_channel0";


    private const string PermissionKey = "HasRequestedNotificationPermission";
    private const string LoginTimesKey = "LoginTimes";
    private const int MinHoursBetweenLogins = 3;


    [SerializeField] private NotificationScheduleSO _notificationSchedule;

    #endregion

    #region ConfigDrivenNotifications
    private const int CONFIG_NTF_BASE_ID = 20000;

    /// <summary>
    /// Lên lịch thông báo dựa trên dữ liệu từ NotificationScheduleSO.
    /// Kiểm tra trạng thái notification trước khi schedule lại.
    /// </summary>
    private void ScheduleFromConfig()
    {
        if (_notificationSchedule == null || _notificationSchedule.Entries.Count == 0)
        {
            Debug.Log("[NotificationManager] ScheduleFromConfig: _notificationSchedule rỗng hoặc không có entry, bỏ qua.");
            return;
        }

        Debug.Log(
            $"[NotificationManager] ScheduleFromConfig: WeeklyRepeat={_notificationSchedule.WeeklyRepeat}, " +
            $"EntriesCount={_notificationSchedule.Entries.Count}");

        var repeatHour = _notificationSchedule.WeeklyRepeat ? 168 : 0; // 168 giờ = 7 ngày

        foreach (var e in _notificationSchedule.Entries)
        {
            if (e == null)
            {
                Debug.Log("[NotificationManager] ScheduleFromConfig: Bỏ qua entry null.");
                continue;
            }

            if (!e.Enabled)
            {
                Debug.Log(
                    "[NotificationManager] ScheduleFromConfig: Entry bị tắt (Enabled=false), " +
                    $"Day={e.Day}, Hour={e.Hour}, Minute={e.Minute}. Bỏ qua.");
                continue;
            }

            int id = GetConfigEntryId(e);
            Debug.Log(
                "[NotificationManager] ScheduleFromConfig: Chuẩn bị schedule entry, " +
                $"Id={id}, Day={e.Day}, Hour={e.Hour}, Minute={e.Minute}, RepeatHour={repeatHour}");

            CheckScheduledNotificationStatus(id, repeatHour, e);
        }
    }

    /// <summary>
    /// Sinh ID ổn định từ entry cấu hình để tránh trùng lặp giữa các lần run.
    /// </summary>
    private int GetConfigEntryId(NotificationScheduleSO.Entry e)
    {
        // encode: day (0..6), hour (0..23), minute (0..59)
        return CONFIG_NTF_BASE_ID + ((int)e.Day * 10000) + (e.Hour * 100) + e.Minute;
    }

    /// <summary>
    /// Tính thời điểm LOCAL kế tiếp cho một ngày/giờ/phút cụ thể.
    /// </summary>
    private DateTime GetNextLocalDateTime(DayOfWeek targetDay, int hour, int minute)
    {
        DateTime now = DateTime.Now;

        int daysUntil = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;

        DateTime candidate = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);

        if (daysUntil == 0)
        {
            // Hôm nay: nếu chưa đến giờ → dùng hôm nay; nếu đã qua → cộng 7 ngày
            if (now <= candidate)
            {
                return candidate;
            }

            return candidate.AddDays(7);
        }

        return candidate.AddDays(daysUntil);
    }

    /// <summary>
    /// Tạo ID ổn định cho thông báo lặp theo tuần dựa trên DayOfWeek và giờ.
    /// </summary>

#if UNITY_IOS
    /// <summary>
    /// Map DayOfWeek (.NET) sang Weekday của iOS (1=CN, 2=Th2, ..., 7=Th7).
    /// </summary>
    private int GetIosWeekday(DayOfWeek day)
    {
        return day == DayOfWeek.Sunday ? 1 : ((int)day + 1);
    }
#endif

    #endregion

    #region Implement

    private void Awake()
    {
#if UNITY_ANDROID
        // Nếu chưa từng xin và quyền chưa được cấp → xin quyền
        if (GetApiLevel() >= 33 && PlayerPrefs.GetInt(PermissionKey, 0) == 0)
        {
            StartCoroutine(RequestNotificationPermission());

            PlayerPrefs.SetInt(PermissionKey, 1);
            PlayerPrefs.Save();
        }

        var group = new AndroidNotificationChannelGroup()
        {
            Id = "Main",
            Name = "Main notifications",
        };
        AndroidNotificationCenter.RegisterNotificationChannelGroup(group);

        var channel = new AndroidNotificationChannel()
        {
            Id = ChannelId,
            Name = "Default Game Channel",
            Importance = Importance.High,
            Description = "Generic notifications",
            Group = "Main",  // must be same as Id of previously registered group
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        Debug.Log($"[NotificationManager] Initialized notification channel {channel.Id}");
#elif UNITY_IOS
        var allStatus = PlayerPrefs.GetInt(PermissionKey, 0);
        if (allStatus == 0)
        {
            StartCoroutine(RequestNotificationPermission());
        }
        else if (allStatus == 1)
        {
            iOSNotificationCenter.ApplicationBadge = 0;
        }
#endif
        RecordLogin();
    }

#if UNITY_ANDROID
    private IEnumerator RequestNotificationPermission()
    {
        // Request POST_NOTIFICATIONS permission for Android 13+ (API 33+)
        Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        yield return null;
    }
#elif UNITY_IOS

    IEnumerator RequestNotificationPermission()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            }

            if (req.Granted)
            {
                PlayerPrefs.SetInt(PermissionKey, 1);
                PlayerPrefs.Save();
            }
            else
            {
                PlayerPrefs.SetInt(PermissionKey, 2);
                PlayerPrefs.Save();
            }
        }
    }


#endif


    private int GetApiLevel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
#else
        return 0;
#endif
    }


    private string GetI2(string content)
    {
        try
        {
            return TextUtility.GetI2(content);
        }
        catch
        {
            return content;
        }
    }

    /// <summary>
    /// Ghi nhận lần login hiện tại. Chỉ reschedule notification nếu khoảng cách từ lần login gần nhất >= MinHoursBetweenLogins.
    /// </summary>
    private void RecordLogin()
    {
        List<DateTime> loginTimes = GetLoginTimes();
        DateTime now = DateTime.Now;

        Debug.Log(
            $"[NotificationManager] RecordLogin: Now={now:o}, LoginCountTrước={loginTimes.Count}, " +
            $"LastLogin={(loginTimes.Count > 0 ? loginTimes[loginTimes.Count - 1].ToString("o") : "None")}");

        // Kiểm tra xem có thể log login mới không
        if (loginTimes.Count > 0 && (now - loginTimes[loginTimes.Count - 1]).TotalHours < MinHoursBetweenLogins)
        {
            Debug.Log(
                $"[NotificationManager] RecordLogin: Login quá sớm sau lần login gần nhất (" +
                $"< {MinHoursBetweenLogins} giờ). Bỏ qua reschedule notification.");
            return;
        }

        // Thêm thời gian login mới
        loginTimes.Add(now);
        SaveLoginTimes(loginTimes);

        Debug.Log("[NotificationManager] RecordLogin: Đủ điều kiện, tiến hành ScheduleFromConfig.");

        // Chỉ reschedule notification nếu đủ điều kiện throttling
        ScheduleFromConfig();
    }

    /// <summary>
    /// Lấy danh sách các lần login từ PlayerPrefs.
    /// </summary>
    private List<DateTime> GetLoginTimes()
    {
        List<DateTime> loginTimes = new List<DateTime>();
        string loginTimesStr = PlayerPrefs.GetString(LoginTimesKey, string.Empty);

        if (!string.IsNullOrEmpty(loginTimesStr))
        {
            string[] loginTimesArray = loginTimesStr.Split(',');
            foreach (string timeStr in loginTimesArray)
            {
                if (DateTime.TryParse(timeStr, out DateTime time))
                {
                    loginTimes.Add(time);
                }
            }
        }

        return loginTimes;
    }

    /// <summary>
    /// Lưu danh sách lần login vào PlayerPrefs (dùng format ISO 8601). Chỉ giữ lại login gần nhất để từnh.
    /// </summary>
    private void SaveLoginTimes(List<DateTime> loginTimes)
    {
        // Chỉ giữ lại 1 phần tặ gần nhất
        if (loginTimes.Count > 1)
        {
            loginTimes = new List<DateTime> { loginTimes[loginTimes.Count - 1] };
        }

        List<string> loginTimesStrList = new List<string>();
        foreach (DateTime time in loginTimes)
        {
            loginTimesStrList.Add(time.ToString("o"));
        }

        string loginTimesStr = string.Join(",", loginTimesStrList);
        PlayerPrefs.SetString(LoginTimesKey, loginTimesStr);
        PlayerPrefs.Save();

        Debug.Log($"[NotificationManager] SaveLoginTimes: Count={loginTimes.Count}, Value='{loginTimesStr}'.");
    }

    /// <summary>
    /// Kiểm tra trạng thái của scheduled notification theo entry cấu hình.
    /// - Scheduled: cập nhật thời gian/gói thông báo.
    /// - Delivered: hủy cũ và gửi lại.
    /// - Unknown: gửi mới.
    /// </summary>
    private void CheckScheduledNotificationStatus(int ntfId, int repeatHour, NotificationScheduleSO.Entry entry)
    {
        if (ntfId == 0 || entry == null)
        {
            Debug.Log(
                $"[NotificationManager] CheckScheduledNotificationStatus: ntfId hoặc entry không hợp lệ. " +
                $"ntfId={ntfId}, entryNull={entry == null}");
            return;
        }

        var nextTime = GetNextLocalDateTime(entry.Day, entry.Hour, entry.Minute);
        var delay = nextTime - DateTime.Now;

        Debug.Log(
            "[NotificationManager] CheckScheduledNotificationStatus: " +
            $"Id={ntfId}, Day={entry.Day}, Hour={entry.Hour}, Minute={entry.Minute}, " +
            $"NextTime={nextTime:o}, Delay={delay}, RepeatHour={repeatHour}");

        var notificationParams = new NotificationParams
        {
            Id = ntfId,
            Delay = delay,
            Title = GetI2(entry.TitleI2Key),
            Message = GetI2(entry.DescriptionI2Key),
        };

#if UNITY_ANDROID
        var notificationStatus = AndroidNotificationCenter.CheckScheduledNotificationStatus(ntfId);
        AndroidNotification gameNotification = GetAndroidNotification(notificationParams, repeatHour, true);

        Debug.Log(
            $"[NotificationManager] CheckScheduledNotificationStatus(Android): Id={ntfId}, " +
            $"Status={notificationStatus}");

        if (notificationStatus == NotificationStatus.Scheduled)
        {
            Debug.Log(
                $"[NotificationManager] Android: UpdateScheduledNotification cho Id={ntfId}, " +
                $"FireTime={gameNotification.FireTime}");
            AndroidNotificationCenter.UpdateScheduledNotification(ntfId, gameNotification, ChannelId);
        }
        else if (notificationStatus == NotificationStatus.Delivered)
        {
            Debug.Log(
                $"[NotificationManager] Android: Notification Id={ntfId} đã Delivered, Cancel và SendCustom lại.");
            Cancel(ntfId);
            SendCustom(notificationParams, repeatHour, true);
        }
        else if (notificationStatus == NotificationStatus.Unknown)
        {
            Debug.Log(
                $"[NotificationManager] Android: Notification Id={ntfId} ở trạng thái Unknown, SendCustom mới.");
            SendCustom(notificationParams, repeatHour, true);
        }
#elif UNITY_IOS
        iOSNotification newNotification = GetIOSNotification(notificationParams, repeatHour);
        var scheduledNotifications = iOSNotificationCenter.GetScheduledNotifications();

        bool found = false;
        foreach (var scheduled in scheduledNotifications)
        {
            if (scheduled.Identifier == ntfId.ToString())
            {
                Debug.Log(
                    $"[NotificationManager] iOS: Tìm thấy notification Id={ntfId}, tiến hành update.");
                found = true;
                iOSNotificationCenter.RemoveScheduledNotification(ntfId.ToString());
                iOSNotificationCenter.ScheduleNotification(newNotification);
                break;
            }
        }

        if (!found)
        {
            Debug.Log(
                $"[NotificationManager] iOS: Không tìm thấy notification Id={ntfId}, schedule mới.");
            iOSNotificationCenter.ScheduleNotification(newNotification);
        }
#endif
    }






    private void Cancel(int id)
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelNotification(id);
#endif

#if UNITY_IOS
        iOSNotificationCenter.RemoveScheduledNotification(id.ToString());
#endif
    }

    private void SendCustom(NotificationParams notificationParams, int repeatHour = 0, bool isTimeLocal = false)
    {
        //Assets.SimpleAndroidNotifications.NotificationManager.SendCustom(notificationParams);
#if UNITY_ANDROID
        AndroidNotification gameNotification = GetAndroidNotification(notificationParams, repeatHour, isTimeLocal);
        AndroidNotificationCenter.SendNotificationWithExplicitID(gameNotification, ChannelId, notificationParams.Id);
#endif

#if UNITY_IOS
        iOSNotification gameNotificationIos = GetIOSNotification(notificationParams, repeatHour);
        iOSNotificationCenter.ScheduleNotification(gameNotificationIos);
#endif
    }

#if UNITY_ANDROID
    private AndroidNotification GetAndroidNotification(NotificationParams notificationParams, int repeatHour, bool isTimeLocal = false)
    {
        AndroidNotification notification = new AndroidNotification();
        notification.Title = notificationParams.Title;
        notification.Text = notificationParams.Message;
        // Android 12+ (API 31+) requires a valid SMALL icon (silhouette). 
        // We use "icon_small" which must be registered in Mobile Notifications settings as a "Small" type icon.
        notification.SmallIcon = "icon_small";
        
        // Optional: Set LargeIcon if you want the colorful icon to show up too
        notification.LargeIcon = "icon_large";
        if (isTimeLocal)
        {
            notification.FireTime = DateTime.Now.Add(notificationParams.Delay);
        }
        else
        {
            notification.FireTime = DateTime.UtcNow.Add(notificationParams.Delay);
        }

        if (repeatHour > 0)
        {
            notification.RepeatInterval = new TimeSpan(repeatHour, 0, 0);
        }

        Debug.Log(
            $"[NotificationManager] GetAndroidNotification: Id={notificationParams.Id}, Title='{notificationParams.Title}', " +
            $"Delay={notificationParams.Delay}, FireTime={notification.FireTime}, RepeatHour={repeatHour}, " +
            $"IsTimeLocal={isTimeLocal}");

        return notification;
    }
#endif

#if UNITY_IOS
    private iOSNotification GetIOSNotification(NotificationParams notificationParams, int repeatHour)
    {
        var notification = new iOSNotification
        {
            Identifier = notificationParams.Id.ToString(),
            Title = notificationParams.Title,
            Body = notificationParams.Message,
            Badge = 1,
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
        };

        var timeTrigger = new iOSNotificationTimeIntervalTrigger
        {
            TimeInterval = notificationParams.Delay,
            Repeats = false
        };

        notification.Trigger = timeTrigger;

        return notification;
    }
#endif

    #endregion
}
