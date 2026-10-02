#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BulletStrike
{
    /// <summary>
    /// ScriptableObject cấu hình lịch thông báo. Cho phép định nghĩa nhiều mục thông báo
    /// theo ngày trong tuần, giờ/phút, và key I2 cho title/description. Có thể cấu hình
    /// lặp hàng tuần hoặc one-shot.
    /// </summary>
    [CreateAssetMenu(fileName = "NotificationScheduleSO", menuName = "Config/Notification Schedule SO")]
    public class NotificationScheduleSO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private DayOfWeek _day = DayOfWeek.Monday;
            [SerializeField] private int _hour = 8;      // 0..23
            [SerializeField] private int _minute = 0;    // 0..59
            [SerializeField] private bool _isMorning = true; // Thông tin bổ trợ (sáng/tối) nếu cần hiển thị/debug
            [SerializeField] private string _titleI2Key = string.Empty;
            [SerializeField] private string _descriptionI2Key = string.Empty;
            [SerializeField] private bool _enabled = true;

            /// <summary>
            /// Ngày trong tuần của thông báo.
            /// </summary>
            public DayOfWeek Day => _day;
            /// <summary>
            /// Giờ (0..23) của thông báo.
            /// </summary>
            public int Hour => _hour;
            /// <summary>
            /// Phút (0..59) của thông báo.
            /// </summary>
            public int Minute => _minute;
            /// <summary>
            /// Cờ sáng/tối (tùy chọn) để tham chiếu.
            /// </summary>
            public bool IsMorning => _isMorning;
            /// <summary>
            /// Key I2 cho Title.
            /// </summary>
            public string TitleI2Key => _titleI2Key;
            /// <summary>
            /// Key I2 cho Description/Body.
            /// </summary>
            public string DescriptionI2Key => _descriptionI2Key;
            /// <summary>
            /// Cho phép bật/tắt mục lịch.
            /// </summary>
            public bool Enabled => _enabled;

            /// <summary>
            /// Factory tạo Entry với dữ liệu đầy đủ.
            /// </summary>
            public static Entry Create(
                DayOfWeek day, int hour, int minute, bool isMorning,
                string titleI2Key, string descriptionI2Key, bool enabled)
            {
                var e = new Entry();
                e._day = day;
                e._hour = hour;
                e._minute = minute;
                e._isMorning = isMorning;
                e._titleI2Key = titleI2Key ?? string.Empty;
                e._descriptionI2Key = descriptionI2Key ?? string.Empty;
                e._enabled = enabled;
                return e;
            }
        }

        [Header("Lặp lại")]
        [SerializeField] private bool _weeklyRepeat = true;

        [Header("Danh sách mục lịch")]
        [SerializeField] private List<Entry> _entries = new List<Entry>();

        /// <summary>
        /// Có lặp theo tuần hay không.
        /// </summary>
        public bool WeeklyRepeat => _weeklyRepeat;

        /// <summary>
        /// Danh sách mục lịch (read-only).
        /// </summary>
        public IReadOnlyList<Entry> Entries => _entries;

        #region API
        /// <summary>
        /// Xóa toàn bộ entries.
        /// </summary>
        public void ClearEntries()
        {
            _entries.Clear();
        }

        /// <summary>
        /// Thêm một entry.
        /// </summary>
        public void AddEntry(Entry entry)
        {
            if (entry == null)
            {
                return;
            }
            _entries.Add(entry);
        }

        /// <summary>
        /// Đặt chế độ lặp tuần.
        /// </summary>
        public void SetWeeklyRepeat(bool value)
        {
            _weeklyRepeat = value;
        }
        #endregion
    }
}
