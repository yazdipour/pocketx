using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace PocketX.Handlers
{
    internal class UiUtils
    {
        public void TitleBarVisibility(bool visible, Windows.UI.Xaml.UIElement control)
        {
            var coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = !visible;
            Windows.UI.Xaml.Window.Current.SetTitleBar(control);
        }

        public void TitleBarButtonTransparentBackground(bool isDark)
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
            var cl = (AcrylicBrush)Application.Current.Resources[resourceKey];
            cl.TintColor = cl.FallbackColor = HexColorToSolidColor(HexColor).Color;
        }

        public void ChangeHeaderTheme(string resourceKey, Color color)
        {
            var cl = (AcrylicBrush)Application.Current.Resources[resourceKey];
            cl.TintColor = cl.FallbackColor = color;
        }

        internal Color HexToColor(string hexString)
        {
            hexString = hexString.Replace("#", string.Empty);
            var r = byte.Parse(hexString.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(hexString.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(hexString.Substring(4, 2), NumberStyles.HexNumber);
            return Color.FromArgb(byte.Parse("1"), r, g, b);
        }

        internal static SolidColorBrush HexColorToSolidColor(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            if (hex.Length == 4) hex = "ff" + hex;
            if (hex.Length != 8) return new SolidColorBrush(Colors.LimeGreen);
            var a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            var r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            var g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            var b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        public async Task PinAppWindow(int width, int height)
        {
            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
                {
                    var compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                    compactOptions.CustomSize = new Windows.Foundation.Size(width, height);
                    await ApplicationView.GetForCurrentView()
                        .TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
                }
                else await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
            }
            else await ShowDialogAsync("You System does not support Compact Mode");
        }

        public static async Task ShowDialogAsync(string errorMessage) => await new Windows.UI.Popups.MessageDialog(errorMessage).ShowAsync();
    }
}
