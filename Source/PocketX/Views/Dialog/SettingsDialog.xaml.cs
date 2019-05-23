using PocketX.Handlers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PocketX.Models;
using Windows.UI.Popups;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using PocketSharp.Models;

namespace PocketX.Views.Dialog
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsDialog() => InitializeComponent();
        private Settings Settings => SettingsHandler.Settings;
        private readonly string _versionString = $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";
        private List<string> FontsList => Utils.GetAllFonts();
        private readonly Settings[] _themes = {
            new Settings { AppTheme = ElementTheme.Light,ReaderBg = "#FEFEFE",ReaderTheme = ElementTheme.Light,Thumbnail = "/Assets/ReadTheme/theme1.png"},
            new Settings { AppTheme = ElementTheme.Dark,ReaderBg = "#454545",ReaderTheme = ElementTheme.Dark,Thumbnail = "/Assets/ReadTheme/theme4.png"},
            new Settings { AppTheme = ElementTheme.Dark,ReaderBg = "#111111",ReaderTheme = ElementTheme.Dark,Thumbnail = "/Assets/ReadTheme/theme5.png"},
        };
        private PocketUser User => PocketHandler.GetInstance().User;
        private void Close_Click(object sender, RoutedEventArgs e) => Hide();
        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog("Are you sure you want to logout from your account!?");
            dialog.Commands.Add(new UICommand("Yes", (command) =>
            {
                Tag = Keys.Logout;
                Hide();
            }));
            dialog.Commands.Add(new UICommand("NO!"));
            await dialog.ShowAsync();
        }
        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            Akavache.BlobCache.LocalMachine.InvalidateAll();
            Akavache.BlobCache.LocalMachine.Vacuum();
        }
        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is Settings theme)) return;
            Settings.AppTheme = theme.AppTheme;
            Settings.ReaderBg = theme.ReaderBg;
            Settings.ReaderTheme = theme.ReaderTheme;
        }
        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            Settings.ReaderFontSize = int.Parse(comboBox.SelectedValue?.ToString());
            SettingsHandler.Save();
        }
        private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var userStatistics = await PocketHandler.GetInstance().UserStatistics();
            if (userStatistics == null) return;
            RadialProgressBarControl.Maximum = userStatistics.CountAll;
            RadialProgressBarControl.Value = userStatistics.CountRead;
            StatisticsCtrl.Header = $"{userStatistics.CountAll} Articles";
            StatisticsCtrl.Text = $"Unread {userStatistics.CountUnread} Articles";
        }
    }
}
