using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

using static System.Console;

namespace Logger
{
    public static class Logger
    {
        private static bool DebugEnabled = false;
        private static bool AppCenterEnabled = false;

        public static void InitOnlineLogger(string token)
        {
            AppCenter.Start(token, typeof(Analytics), typeof(Crashes));
            AppCenter.LogLevel = LogLevel.Error;
            AppCenterEnabled = true;
        }

        public static void SetDebugMode(bool debug) => DebugEnabled = debug;

        public static void L(string message)
        {
            var TAG = "[LOGGER] ";
            if (DebugEnabled) WriteLine(TAG + message);
            if (AppCenterEnabled) Analytics.TrackEvent(TAG + message);
        }
        public static void E(System.Exception e)
        {
            if (DebugEnabled) WriteLine("[LOGGER-ERR] " + e.Message);
            if (AppCenterEnabled) Crashes.TrackError(e);
        }
    }
}