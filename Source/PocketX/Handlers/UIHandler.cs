using System;
using System.Globalization;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;

namespace PocketX.Handlers
{
    class UIHandler
    {
        public void TitleBarVisiblity(bool visible, Windows.UI.Xaml.UIElement control)
        {
            var coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = !visible;
            Windows.UI.Xaml.Window.Current.SetTitleBar(control);
        }

        public void TitleBarButton_TranparentBackground(bool isDark)
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Black;
            titleBar.BackgroundColor = Colors.Transparent;
            if (isDark)
                titleBar.ButtonForegroundColor = titleBar.ForegroundColor = titleBar.ButtonHoverForegroundColor = titleBar.ButtonHoverForegroundColor = Colors.WhiteSmoke;
            else
                titleBar.ButtonForegroundColor = titleBar.ForegroundColor = titleBar.ButtonHoverForegroundColor = titleBar.ButtonHoverForegroundColor = Colors.Black;
        }

        public void ChangeHeaderTheme(string resourceKey, string HexColor)
        {
            var cl = (AcrylicBrush)App.Current.Resources[resourceKey];
            cl.TintColor = cl.FallbackColor = HexColorToSolidColor(HexColor).Color;
        }

        public void ChangeHeaderTheme(string resourceKey, Color color)
        {
            var cl = (AcrylicBrush)App.Current.Resources[resourceKey];
            cl.TintColor = cl.FallbackColor = color;
        }

        internal Color HexToColor(string hexString)
        {
            hexString = hexString.Replace("#", string.Empty);
            byte r = byte.Parse(hexString.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hexString.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hexString.Substring(4, 2), NumberStyles.HexNumber);
            return Windows.UI.Color.FromArgb(byte.Parse("1"), r, g, b);
        }

        internal static SolidColorBrush HexColorToSolidColor(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            if (hex.Length == 4) hex = "ff" + hex;
            if (hex.Length != 8) return new SolidColorBrush(Colors.LimeGreen);
            byte a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
            SolidColorBrush myBrush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            return myBrush;
        }
    }
}
