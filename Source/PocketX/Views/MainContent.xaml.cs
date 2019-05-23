using System;
using System.Collections.Generic;
using System.Linq;
using PocketSharp.Models;
using PocketX.Handlers;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using PocketX.ViewModels;
using PocketX.Views.Dialog;

namespace PocketX.Views
{
    public sealed partial class MainContent : Page
    {
        private readonly MainContentViewModel _vm;
        private static bool IsSmallWidth(double width) => width < 720;
        public MainContent()
        {
            InitializeComponent();
            _vm = new MainContentViewModel();
            var uiUtils = new UiUtils();
            uiUtils.TitleBarVisibility(false, MarkdownCtrl.WindowBorder);
            uiUtils.TitleBarButtonTransparentBackground(_vm.Settings.AppTheme == ElementTheme.Dark);
            DataTransferManager.GetForCurrentView().DataRequested += _vm.ShareArticle;
            Loaded += async (s, e) =>
            {
                // TODO move this stuff to repository
                if (!Utils.HasInternet)
                    foreach (var pocketItem in await _vm.PocketHandler.GetItemsCache())
                        _vm.ArticlesList.Add(pocketItem);
                Logger.Logger.InitOnlineLogger(Keys.AppCenter);
                Logger.Logger.SetDebugMode(App.DEBUGMODE);
                NotificationHandler.InAppNotificationControl = InAppNotifier;
                await _vm.PocketHandler.FetchTagsAsync();
            };
            MarkdownCtrl.DeleteArticleAsync = _vm.DeleteArticleAsync;
            MarkdownCtrl.ToggleArchiveArticleAsync = _vm.ToggleArchiveArticleAsync;
            MarkdownCtrl.ToggleFavoriteArticleAsync = _vm.ToggleFavoriteArticleAsync;
        }
        private async void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => await _vm.SearchCommand(sender?.Text);
        private void HeadAppBarClicked(object sender, RoutedEventArgs e) => PivotList.SelectedIndex = ((AppBarButton)sender)?.Tag?.ToString() == "Find" ? 3 : 4;
        private void ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!(e?.ClickedItem is PocketItem item)) return;
            if (SplitView.IsPaneOpen && IsSmallWidth(ActualWidth)) SplitView.IsPaneOpen = false;
            _vm.PocketHandler.CurrentPocketItem = item;
            MarkdownCtrl.OpenInArticleView();
        }
        private async void SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args) => await _vm.SwipeItem_Invoked(sender, args);
        private void ItemRightTapped(object sender, RightTappedRoutedEventArgs e) => _vm.ItemRightTapped(sender, e);
        private async void Tag_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(((TextBlock)sender).DataContext is string tag)) return;
            PivotList.SelectedIndex = 3;
            tag = '#' + tag;
            SearchBox.Text = tag;
            await _vm.SearchCommand(tag);
        }
        private async void SymbolIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(((SymbolIcon)sender).DataContext is string tag)) return;
            _vm.PocketHandler.Tags.Remove(tag);
            await _vm.PocketHandler.Client.DeleteTag(tag);
        }

        private void PivotList_SelectionChanged(object sender, SelectionChangedEventArgs e) => _vm.ListIsLoading = false;

        private async void TopAppBarClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog();
            await dialog.ShowAsync();
            if (dialog.Tag?.ToString() == Keys.Logout)
            {
                PocketHandler.GetInstance().Logout();
                Frame?.Navigate(typeof(Views.LoginPage));
                Frame?.BackStack.Clear();
                return;
            }
            Bindings.Update();
        }
        public static string ConvertTagsToString(IEnumerable<PocketTag> tags)
            => tags == null ? "" : "#" + string.Join(" #", tags.Select(_ => _.Name).ToArray());
    }
}