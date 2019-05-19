namespace PocketX.Handlers
{
    public class NotificationHandler
    {
        public static Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification InAppNotificationControl;
        public static void InAppNotification(string text, int duration) => InAppNotificationControl?.Show(text, duration);
    }
}
