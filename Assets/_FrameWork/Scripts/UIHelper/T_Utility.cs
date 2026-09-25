using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public static class T_Utility
{
    private static readonly string[] GoldUnits =
    {
        "", "K", "M", "B", "T", "aa", "ab", "ac", "ad", "ae", "af",
        "ag", "ah", "ai", "aj", "ak", "al", "am", "an", "ao", "ap",
        "aq", "ar", "as", "at", "au", "av", "aw", "ax", "ay", "az",
        "ba", "bb", "bc", "bd", "be", "bf", "bg", "bh", "bi", "bj",
        "bk", "bl", "bm", "bn", "bo", "bp", "bq", "br", "bs", "bt",
        "bu", "bv", "bw", "bx", "by", "bz"
    };

    public static string FormatGold(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return value.ToString();

        var absoluteValue = Math.Abs(value);
        if (absoluteValue < 1000) return Math.Floor(value).ToString("0", EN_Culture);

        int unitIndex = (int)Math.Floor(Math.Log10(absoluteValue) / 3);
        unitIndex = Math.Min(unitIndex, GoldUnits.Length - 1);
        double scaledValue = value / Math.Pow(1000, unitIndex);
        return scaledValue.ToString("0.##", EN_Culture) + " " + GoldUnits[unitIndex];
    }

    public static string FormatGem(double value)
    {
        return FormatGold(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ZeroReplace(this int value, int target)
    {
        return value > 0 ? value : target;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ZeroReplace(this double value, double target)
    {
        return value > 0 ? value : target;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NullReplace(this string str, string replace = "null")
    {
        return string.IsNullOrEmpty(str)
            ? replace
            : str;
    }

    public static void TryCancel(this CancellationTokenSource cancellation)
    {
        if (cancellation == null) return;
        try
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }
        catch (Exception)
        {
        }
    }

    public static CultureInfo EN_Culture = new CultureInfo("en-US");

    #region Button

    public static void RegisterListener(this List<Button> buttons, Action onClick)
    {
        if (buttons == null || buttons.Count == 0) return;
        foreach (var button in buttons)
        {
            button?.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    public static void RemoveListener(this List<Button> buttons, Action onClick)
    {
        if (buttons == null || buttons.Count == 0) return;
        foreach (var button in buttons)
        {
            button?.onClick.RemoveListener(() => onClick?.Invoke());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetActive(this Button button, bool active)
    {
        if (!button) return;
        button.gameObject.SetActive(active);
    }

    #endregion

    #region UnityEvent (Editor Helper)

#if UNITY_EDITOR
    /// <summary>
    /// Thêm persistent listener vào UnityEvent nếu chưa tồn tại (tránh trùng lặp khi chạy OnValidate).
    /// </summary>
    public static void AddPersistentListenerIfNotExists(this UnityEngine.Events.UnityEvent unityEvent, UnityEngine.Events.UnityAction action)
    {
        if (unityEvent == null || action == null) return;

        var target = action.Target as UnityEngine.Object;
        var methodName = action.Method.Name;

        int count = unityEvent.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (unityEvent.GetPersistentTarget(i) == target &&
                unityEvent.GetPersistentMethodName(i) == methodName)
            {
                return;
            }
        }

        UnityEditor.Events.UnityEventTools.AddPersistentListener(unityEvent, action);
        if (target != null)
        {
            UnityEditor.EditorUtility.SetDirty(target);
        }
    }
#endif

    #endregion
}
