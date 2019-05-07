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
        private Settings settings = SettingsHandler.Settings;
        private PocketHandler pocketHandler = new PocketHandler();
        private string versionString = $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

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
            settings.reader_theme = tg_reader.IsOn ? ElementTheme.Dark : ElementTheme.Light;
            settings.reader_bg = colorPicker.Color.ToString();
            settings.reader_font_size = System.Int32.Parse(comboBox.SelectedValue?.ToString());
            settings.app_theme = RequestedTheme = tg_app.IsOn ? ElementTheme.Light : ElementTheme.Dark;
            settings.listview_scrollbar = tg_scrollbar.IsOn ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden;
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

        private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            tg_app.IsOn = settings.app_theme == ElementTheme.Light;
            tg_reader.IsOn = settings.reader_theme != ElementTheme.Light;
            tg_scrollbar.IsOn = settings.listview_scrollbar == ScrollBarVisibility.Auto;
            MarkdownText.Text = (await Utils.TextFromAssets(@"Assets\Icons\ChangeLog.md")).Replace("[VERSION]", versionString);
        }

        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            Akavache.BlobCache.LocalMachine.InvalidateAll();
            Akavache.BlobCache.LocalMachine.Vacuum();
        }
    }
}
