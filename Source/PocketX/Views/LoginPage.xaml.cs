using System;
using PocketX.Handlers;
using Windows.Security.Authentication.Web;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PocketX.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage() => InitializeComponent();

        private async void Login_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = await PocketHandler.GetInstance().LoginUriAsync();
                var auth = await WebAuthenticationBroker.
                    AuthenticateAsync(WebAuthenticationOptions.None, uri, new Uri(App.Protocol));
                if (auth.ResponseStatus != WebAuthenticationStatus.Success) return;
                if (await PocketHandler.GetInstance().LoginAsync()) Frame.Navigate(typeof(MainContent));
                else throw new Exception();
            }
            catch
            {
                var dialog = new MessageDialog("Error.");
                dialog.Commands.Add(new UICommand("Close"));
                await dialog.ShowAsync();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog("If you login to your Pocket with Google Account, \nYou may " +
                                           "trouble login in due to problems Google has with Windows Auth " +
                                           "panel (IE Engine).\n\nWe encourage you to go to (https://getpocket.com/changeuser) " +
                                           "and choose a username for your account and then login with your " +
                                           "UserName and Password.");
            dialog.Commands.Add(new UICommand("Ok!"));
            await dialog.ShowAsync();
        }
    }
}
