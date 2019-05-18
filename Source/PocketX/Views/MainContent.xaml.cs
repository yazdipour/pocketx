﻿using System;
using PocketSharp.Models;

using PocketX.Handlers;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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
                await _vm.PocketHandler.FetchTagsAsync();
                Logger.Logger.InitOnlineLogger(Keys.AppCenter);
                Logger.Logger.SetDebugMode(App.DEBUGMODE);
            };
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
        private async void TagItemClick(object sender, ItemClickEventArgs e)
        {
            if (!(e?.ClickedItem is string tag)) return;
            PivotList.SelectedIndex = 3;
            tag = '#' + tag;
            SearchBox.Text = tag;
            await _vm.SearchCommand(tag);
        }
        private void PivotList_SelectionChanged(object sender, SelectionChangedEventArgs e) => _vm.ListIsLoading = false;
    }
}