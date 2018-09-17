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
        PocketHandler pocketHandler = new PocketHandler();
        public LoginPage()
        {
            this.InitializeComponent();
        }

        private async void Login_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = await pocketHandler.LoginUriAsync();
                WebAuthenticationResult auth = await WebAuthenticationBroker.
                    AuthenticateAsync(WebAuthenticationOptions.None, uri, new Uri(App.Protocol));
                if (auth.ResponseStatus == WebAuthenticationStatus.Success)
                {
                    if (await pocketHandler.LoginAsync()) Frame.Navigate(typeof(MainPage));
                    else throw new Exception();
                }
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
