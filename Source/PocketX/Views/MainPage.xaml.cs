using PocketX.Handlers;
using Windows.UI.Xaml.Controls;
using PocketX.ViewModels;

namespace PocketX.Views
{
    public sealed partial class MainPage : Page
    {
        public static Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification Notifier;
        private readonly MainPageViewModel _vm;

        public MainPage()
        {
            InitializeComponent();
            Notifier = InAppNotifier;
            InsideFrame.Navigate(typeof(MainContent));
            if (InsideFrame.Content is MainContent main)
                DataContext = _vm = new MainPageViewModel(main.ParentCommandAsync);
            Loaded += (s, e) =>
            {
                var uiUtils = new UiUtils();
                uiUtils.TitleBarVisibility(false, WindowBorder);
                uiUtils.TitleBarButtonTransparentBackground(_vm.Settings.AppTheme == Windows.UI.Xaml.ElementTheme.Dark);

                Logger.Logger.InitOnlineLogger(Keys.AppCenter);
                Logger.Logger.SetDebugMode(App.DEBUGMODE);
            };
        }

        private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
            => await _vm.Navigate(((NavigationViewItem)args.SelectedItem)?.Content?.ToString());

        private void PaneFooter_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is NavigationViewItem item && item.Content != null)
            {
                if (item.Content.ToString().Contains("Tags"))
                    _vm.TagsBtnClicked(NavView);
                else
                {
                    _vm.SettingsBtnClicked(Frame);
                    ((MainContent)InsideFrame.Content)?.BindingsUpdate();
                    //Bindings?.Update();
                }
            }
        }
    }
}