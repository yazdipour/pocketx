using PocketSharp.Models;

using PocketX.Handlers;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PocketX.ViewModels;

namespace PocketX.Views
{
    public sealed partial class MainContent : Page
    {
        public static Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification Notifier;
        private readonly MainContentViewModel _vm;
        private static bool IsSmallWidth(double width) => width < 720;
        public MainContent()
        {
            InitializeComponent();
            Notifier = InAppNotifier;
            _vm = new MainContentViewModel();
            var uiUtils = new UiUtils();
            uiUtils.TitleBarVisibility(false, MarkdownCtrl.WindowBorder);
            uiUtils.TitleBarButtonTransparentBackground(_vm.Settings.AppTheme == ElementTheme.Dark);
            DataTransferManager.GetForCurrentView().DataRequested += _vm.ShareArticle;
            Loaded += async (s, e) =>
            {
                if (!Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    foreach (var pocketItem in await _vm.PocketHandler.GetItemsCache())
                        _vm.ArticlesList.Add(pocketItem);
                else _vm.LoadHomeCommand();
                try { await _vm.PocketHandler.GetTagsAsync(false); }
                catch { }

                Logger.Logger.InitOnlineLogger(Keys.AppCenter);
                Logger.Logger.SetDebugMode(App.DEBUGMODE);
                MarkdownCtrl.ToggleArchiveArticleAsync = _vm.ToggleArchiveAsync;
                MarkdownCtrl.DeleteArticleAsync = _vm.DeleteArticleAsync;
                MarkdownCtrl.ToggleFavoriteArticleAsync = _vm.ToggleFavoriteArticleAsync;
            };
        }
        private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => _vm.SearchCommand(sender?.Text);
        private void HeadAppBarClicked(object sender, RoutedEventArgs e) => Pivot.SelectedIndex = ((AppBarButton)sender)?.Tag?.ToString() == "Find" ? 3 : 4;

        private void listViewInSplitView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (SplitView.IsPaneOpen && IsSmallWidth(ActualWidth)) SplitView.IsPaneOpen = false;
            _vm.PocketHandler.CurrentPocketItem = e.ClickedItem as PocketItem;
            MarkdownCtrl.OpenInArticleView();
            LoadingListControl.IsLoading = true;
        }

    }
}