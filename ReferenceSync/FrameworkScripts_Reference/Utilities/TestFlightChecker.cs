using UnityEngine;

public static class TestFlightChecker
{
#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern bool _IsTestFlight();
#endif

    public static bool IsTestFlight
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                return _IsTestFlight();
            }
            catch
            {
                return false;
            }
#else
            return false; // Editor hoặc Android
#endif
        }
    }

    // Dùng tiện hơn: true = App Store, false = TestFlight
    public static bool IsAppStore => !IsTestFlight;
}