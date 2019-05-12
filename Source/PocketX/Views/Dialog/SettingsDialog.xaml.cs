using PocketX.Handlers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PocketX.Models;
using Windows.UI.Popups;
using System;
using Windows.ApplicationModel;

namespace PocketX.Views.Dialog
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        private Settings _settings = SettingsHandler.Settings;
        private readonly string _versionString = $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

        public SettingsDialog()
        {
            this.InitializeComponent();
            comboBox2.ItemsSource = Utils.GetAllFonts();
        }

        public SettingsDialog(int pivotIndex)
        {
            this.InitializeComponent();
            pivot.SelectedIndex = pivotIndex;
            comboBox2.ItemsSource = Utils.GetAllFonts();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            //_settings.ReaderTheme = tg_reader.IsOn ? ElementTheme.Dark : ElementTheme.Light;
            //_settings.ReaderBg = colorPicker.Color.ToString();
            _settings.ReaderFontSize = int.Parse(comboBox.SelectedValue?.ToString());
            //_settings.AppTheme = RequestedTheme = tg_app.IsOn ? ElementTheme.Light : ElementTheme.Dark;
            Hide();
        }

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

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            //tg_app.IsOn = _settings.AppTheme == ElementTheme.Light;
            //tg_reader.IsOn = _settings.ReaderTheme != ElementTheme.Light;
        }

        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            Akavache.BlobCache.LocalMachine.InvalidateAll();
            Akavache.BlobCache.LocalMachine.Vacuum();
        }
    }
}
