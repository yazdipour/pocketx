using static System.Diagnostics.Debug;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace Logger
{
    public static class Logger
    {
        private static bool _debugEnabled = false;
        private static bool _appCenterEnabled = false;

        public static void InitOnlineLogger(string token)
        {
            AppCenter.Start(token, typeof(Analytics), typeof(Crashes));
            AppCenter.LogLevel = LogLevel.Error;
            _appCenterEnabled = true;
        }

        public static void SetDebugMode(bool debug) => _debugEnabled = debug;

        public static void L(string message)
        {
            const string TAG = "[LOGGER] ";
            if (_debugEnabled) WriteLine(TAG + message);
            if (_appCenterEnabled) Analytics.TrackEvent(TAG + message);
        }
        public static void E(System.Exception e)
        {
            if (_debugEnabled) WriteLine("[LOGGER-ERR] " + e.Message);
            if (_appCenterEnabled) Crashes.TrackError(e);
        }
    }
}