using System;
using System.Linq;
using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views.Dialog;

using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using PocketX.ViewModels;

namespace PocketX.Views
{
    public sealed partial class MainPage : Page
    {
        public static Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification Notifier { get; set; }
        private readonly MainPageViewModel _vm = new MainPageViewModel();
        internal readonly Settings _settings = SettingsHandler.Settings;

        public MainPage()
        {
            InitializeComponent();
            //DataContext = _vm;
            Notifier = _notifer;
            Loaded += (s, e) =>
            {
                Logger.Logger.InitOnlineLogger(Keys.AppCenter);
                Logger.Logger.SetDebugMode(App.DEBUGMODE);
                var uiUtils = new UiUtils();
                uiUtils.TitleBarVisibility(false, WindowBorder);
                uiUtils.TitleBarButtonTransparentBackground(_settings.AppTheme == Windows.UI.Xaml.ElementTheme.Dark);
                insideFrame.Navigate(typeof(MainContent));
            };
        }

        private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (insideFrame.Content is MainContent mainContent &&
                args.SelectedItem is NavigationViewItem i)
                await mainContent.ParentCommandAsync(i.Content?.ToString());
        }

        private async void PaneFooter_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var tag = ((NavigationViewItem)sender).Tag?.ToString();
            var m = (MainContent)insideFrame.Content;
            switch (tag)
            {
                case "Tags":
                {
                    var dialog = new TagsDialog();
                    await dialog.ShowAsync();
                    NavView.SelectedItem = -1;
                    //NavigationViewExtensions.SetSelectedIndex(navView, -1);
                    if (dialog.Tag != null) await m.ParentCommandAsync(dialog.Tag.ToString());
                    break;
                }

                case "Settings":
                {
                    var dialog = new SettingsDialog(0);
                    await dialog.ShowAsync();
                    if (dialog.Tag?.ToString() == Keys.Logout)
                    {
                        PocketHandler.GetInstance().Logout(Frame);
                        return;
                    }
                    m?.BindingsUpdate();
                    Bindings.Update();
                    SettingsHandler.Save();
                    break;
                }
            }
        }

        private async void Pin_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
            => await new UiUtils().PinAppWindow(520, 400);
    }
}