using System;
using System.Threading.Tasks;
using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views.Dialog;

namespace PocketX.ViewModels
{
    internal class MainPageViewModel
    {
        public MainPageViewModel(Func<string, Task> navFunc) => NavigationFunction = navFunc;
        private Func<string, Task> NavigationFunction { get; }
        internal Settings Settings => SettingsHandler.Settings;
        internal async void PinBtnClicked() => await new UiUtils().PinAppWindow(520, 400);
        internal async Task Navigate(string pageName) => await NavigationFunction(pageName);
        internal async void TagsBtnClicked(Windows.UI.Xaml.Controls.NavigationView navView)
        {
            var dialog = new TagsDialog();
            await dialog.ShowAsync();
            if (dialog.Tag == null) return;
            await NavigationFunction(dialog.Tag.ToString());
            if (navView != null) navView.SelectedItem = -1;
        }
        public async void SettingsBtnClicked(Windows.UI.Xaml.Controls.Frame frame)
        {
            var dialog = new SettingsDialog(0);
            await dialog.ShowAsync();
            if (dialog.Tag?.ToString() == Keys.Logout)
            {
                PocketHandler.GetInstance().Logout();
                frame?.Navigate(typeof(Views.LoginPage));
                frame?.BackStack.Clear();
                return;
            }
            SettingsHandler.Save();
        }
    }
}
