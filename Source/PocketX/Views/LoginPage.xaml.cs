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
        private readonly PocketHandler _pocketHandler = new PocketHandler();
        public LoginPage() => InitializeComponent();

        private async void Login_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = await _pocketHandler.LoginUriAsync();
                var auth = await WebAuthenticationBroker.
                    AuthenticateAsync(WebAuthenticationOptions.None, uri, new Uri(App.Protocol));
                if (auth.ResponseStatus != WebAuthenticationStatus.Success) return;
                if (await _pocketHandler.LoginAsync()) Frame.Navigate(typeof(MainPage));
                else throw new Exception();
            }
            catch
            {
                var dialog = new MessageDialog("Error.");
                dialog.Commands.Add(new UICommand("Close"));
                await dialog.ShowAsync();
            }
        }
    }
}
