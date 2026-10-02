#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BulletStrike.EditorTools
{
    /// <summary>
    /// Tạo asset NotificationScheduleSO với lịch 8h sáng/8h tối cho Th2→CN,
    /// key I2 theo quy ước: local_ntf_{t2..t7|tcn}_{8am|8pm}_{tit|des}
    /// </summary>
    public static class NotificationScheduleSOGenerator
    {
        private const string DefaultAssetPath = "Assets/_Game/_Config/NotificationScheduleSO.asset";

        [MenuItem("Tools/Notifications/Generate Weekly 8AM & 8PM Schedule Asset")]
        public static void Generate()
        {
            // Tạo thư mục nếu chưa có
            var dir = Path.GetDirectoryName(DefaultAssetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var so = ScriptableObject.CreateInstance<NotificationScheduleSO>();
            so.SetWeeklyRepeat(true);
            so.ClearEntries();

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                AddDay(so, day, 8, 0, true);
                AddDay(so, day, 20, 0, false);
            }

            // Lưu asset
            AssetDatabase.CreateAsset(so, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = so;

            // Thử auto-gán vào NotificationManager trong scene (nếu có)
            TryAssignToNotificationManager(so);

            Debug.Log($"Đã tạo asset lịch thông báo: {DefaultAssetPath}");
        }

        private static void AddDay(NotificationScheduleSO so, DayOfWeek day, int hour, int minute, bool isMorning)
        {
            string code = GetDayCode(day);
            string timeCode = isMorning ? "8am" : "8pm";
            string titleKey = $"local_ntf_{code}_{timeCode}_tit";
            string desKey = $"local_ntf_{code}_{timeCode}_des";

            var entry = NotificationScheduleSO.Entry.Create(
                day: day,
                hour: hour,
                minute: minute,
                isMorning: isMorning,
                titleI2Key: titleKey,
                descriptionI2Key: desKey,
                enabled: true);

            so.AddEntry(entry);
        }

        private static string GetDayCode(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return "t2";
                case DayOfWeek.Tuesday: return "t3";
                case DayOfWeek.Wednesday: return "t4";
                case DayOfWeek.Thursday: return "t5";
                case DayOfWeek.Friday: return "t6";
                case DayOfWeek.Saturday: return "t7";
                case DayOfWeek.Sunday: return "tcn";
                default: return "t2";
            }
        }

        private static void TryAssignToNotificationManager(NotificationScheduleSO so)
        {
            var manager = UnityEngine.Object.FindObjectOfType<NotificationManager>();
            if (manager == null)
            {
                return;
            }

            var soManager = new SerializedObject(manager);
            var prop = soManager.FindProperty("_notificationSchedule");
            if (prop != null)
            {
                soManager.Update();
                prop.objectReferenceValue = so;
                soManager.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager);
                Debug.Log("Đã gán NotificationScheduleSO vào NotificationManager trong scene hiện tại.");
            }
        }
    }
}
#endif

