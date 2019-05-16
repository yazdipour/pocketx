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
    }
}
