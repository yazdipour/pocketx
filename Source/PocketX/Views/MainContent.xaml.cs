using Akavache;

using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI.Controls;

using PocketSharp.Models;

using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views.Dialog;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using PocketX.ViewModels;

namespace PocketX.Views
{
    public sealed partial class MainContent : Page
    {
        private readonly MainContentViewModel _vm;
        private static bool IsSmallWidth(double width) => width < 720;

        #region On PageInit
        public MainContent()
        {
            InitializeComponent();
            _vm = new MainContentViewModel {MarkdownHandler = new MarkdownHandler(MarkdownText)};
            _vm.AudioHandler = new AudioHandler(media, _vm.TextProviderForAudioPlayer)
            {
                MediaStartAction = () => { TopBar.MaxWidth = 48; },
                MediaEndAction = () => { TopBar.MaxWidth = 500; }
            };
            HeaderTitle.Text = "PocketX";
            DataTransferManager.GetForCurrentView().DataRequested += _vm.ShareArticle;
            SizeChanged += (s, e) =>
            {
                if (!SplitView.IsPaneOpen && !IsSmallWidth(e.NewSize.Width)) SplitView.IsPaneOpen = true;
                else if (SplitView.IsPaneOpen && IsSmallWidth(e.NewSize.Width)) SplitView.IsPaneOpen = false;
            };
            Loaded += async (s, e) =>
            {
                MarkdownText.Text = await Utils.TextFromAssets(@"Assets\Icons\Home.md");
                if (!Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    ListViewInSplitView.ItemsSource = await _vm.PocketHandler.GetItemsCache();
                else await ParentCommandAsync("Home");
                try { await _vm.PocketHandler.GetTagsAsync(false); }
                catch { }
            };
        }

        #endregion On PageInit

        #region Miscellaneous


        private void Reload_ArticleView(object sender, RoutedEventArgs e) => OpenInArticleView(_vm.PocketHandler.CurrentPocketItem, true);

        private void HandleAppBarStatus()
        {
            var item = _vm.PocketHandler.CurrentPocketItem;
            WebViewBtn.Tag = "webView";
            WebViewBtn.Content = "Open in WebView";
            ArchiveBtn.Content = item.IsArchive ? "" : "";
            ArchiveBtn.Tag = item.IsArchive ? "Unarchive" : "Archive";
            FavBtn.Tag = FavBtn.Content = item.IsFavorite ? "Unfavorite" : "Favorite";
        }

        private void HandleViewVisibilities(string theOne)
        {
            FindName(theOne);
            switch (theOne)
            {
                case "webView":
                    WebView.Visibility = Visibility.Visible;
                    MarkdownGrid.Visibility = Visibility.Collapsed;
                    try { errorView.Visibility = Visibility.Collapsed; }
                    catch { }
                    break;

                case "markdownGrid":
                    FindName("markdownText");
                    MarkdownGrid.Visibility = Visibility.Visible;
                    try { errorView.Visibility = Visibility.Collapsed; }
                    catch { }
                    try { WebView.Visibility = Visibility.Collapsed; }
                    catch { }
                    break;

                case "errorView":
                    errorView.Visibility = Visibility.Visible;
                    MarkdownGrid.Visibility = Visibility.Collapsed;
                    try { WebView.Visibility = Visibility.Collapsed; }
                    catch { }
                    break;
            }
        }

        #endregion Miscellaneous

        #region Controls Events

        private void splitView_PaneOpened(SplitView sender, object args)
        {
            if (BackBtn == null) return;
            BackBtn.Glyph = SplitView.IsPaneOpen ? "" : "";
            if (!SplitView.IsPaneOpen && _vm.PocketHandler.CurrentPocketItem != null)
                TopBar.Visibility = Visibility.Visible;
            else if (SplitView.IsPaneOpen && IsSmallWidth(ActualWidth))
                TopBar.Visibility = Visibility.Collapsed;
        }

        private void listViewInSplitView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (SplitView.IsPaneOpen && IsSmallWidth(ActualWidth)) SplitView.IsPaneOpen = false;
            _vm.PocketHandler.CurrentPocketItem = e.ClickedItem as PocketItem;
            OpenInArticleView(_vm.PocketHandler.CurrentPocketItem);
            FindName("TopBar");
        }

        private void gridView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as PocketItem;
            var flyout = new MenuFlyout();
            var style = new Style { TargetType = typeof(MenuFlyoutPresenter) };
            style.Setters.Add(new Setter(RequestedThemeProperty, _vm.Settings.AppTheme));
            flyout.MenuFlyoutPresenterStyle = style;
            var el = new MenuFlyoutItem { Text = "Copy Link", Icon = new SymbolIcon(Symbol.Copy) };
            el.Click += (sen, ee) =>
            {
                Utils.CopyToClipboard(item?.Uri?.AbsoluteUri);
                MainPage.Notifier.Show("Copied", 2000);
            };
            flyout?.Items?.Add(el);
            el = new MenuFlyoutItem { Text = "Open in browser", Icon = new SymbolIcon(Symbol.World) };
            el.Click += async (sen, ee) => await Launcher.LaunchUriAsync(item?.Uri);
            flyout.Items.Add(el);
            el = new MenuFlyoutItem { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) };
            el.Click += async (sen, ee) => await DeleteArticleAsync(item);
            flyout.Items.Add(el);
            if (!(SplitView.Tag ?? "").ToString().Contains("Fav"))
            {
                el = new MenuFlyoutItem
                {
                    Text = item.IsArchive ? "Add" : "Archive",
                    Icon = new SymbolIcon(item.IsArchive ? Symbol.Add : Symbol.Accept)
                };
                el.Click += async (sen, ee) => { await ArchiveArticleAsync(item, !item.IsArchive); };
                flyout.Items.Insert(0, el);
            }
            if (sender is GridView senderElement) flyout.ShowAt(senderElement, e.GetPosition(senderElement));
        }

        private async void SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            var item = args.SwipeControl?.DataContext as PocketItem;
            if (string.Equals(sender.Text, "Add", StringComparison.OrdinalIgnoreCase))
                await ArchiveArticleAsync(item, false);
            else if (string.Equals(sender.Text, "Archive", StringComparison.OrdinalIgnoreCase))
                await ArchiveArticleAsync(item, true);
            else if (sender.Text == "Delete") await DeleteArticleAsync(item);
        }

        #endregion Controls Events

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
            => await ParentCommandAsync(args?.QueryText);

        public async Task ParentCommandAsync(string tag) => await ParentCommandAsync(tag, 40, 0);
        public async Task ParentCommandAsync(string tag, int count, int offset)
        {
            if ((tag ?? "").Length < 2) return;
            LoadingControl.IsLoading = true;
            if (!SplitView.IsPaneOpen) SplitView.IsPaneOpen = true;
            SplitView.Tag = tag;

            //leftSwipeItems.Clear();
            try
            {
                switch (tag)
                {
                    case "Home":
                        //leftSwipeItems.Add(leftSwipeArchive);
                        if (count == 0)
                        {
                            await _vm.ArticlesList.RefreshAsync();
                            break;
                        }
                        Logger.Logger.L($"Tap {tag}");
                        ListViewInSplitView.ItemsSource = _vm.ArticlesList;
                        //BindingsUpdate();
                        break;

                    case "Favorites":
                        Logger.Logger.L($"Tap {tag}");
                        ListViewInSplitView.ItemsSource = await _vm.PocketHandler.GetListAsync(
                            state: State.all,
                            favorite: true,
                            tag: null,
                            search: null,
                            count: count,
                            offset: offset);
                        break;

                    case "Archives":
                        Logger.Logger.L($"Tap {tag}");
                        ListViewInSplitView.ItemsSource = await _vm.PocketHandler.GetListAsync(
                            state: State.archive,
                            favorite: null,
                            tag: null,
                            search: null,
                            count: count,
                            offset: offset);
                        leftSwipeItems.Add(leftSwipeAdd);
                        break;

                    default:
                        IEnumerable<PocketItem> searchList = null;
                        if ('#'.Equals(tag[0]))
                        {
                            Logger.Logger.L($"Tag {tag}");
                            searchList = await _vm.PocketHandler.GetListAsync(
                                state: State.all,
                                favorite: null,
                                tag: tag.Substring(1),
                                search: null,
                                count: count,
                                offset: offset);
                        }
                        else
                        {
                            Logger.Logger.L("Search");
                            searchList = await _vm.PocketHandler.GetListAsync(
                                state: State.all,
                                favorite: null,
                                tag: null,
                                search: tag,
                                count: count,
                                offset: offset);
                        }
                        ListViewInSplitView.ItemsSource = searchList;
                        leftSwipeItems.Add(leftSwipeArchive);
                        break;
                }
            }
            catch { }
            LoadingControl.IsLoading = false;
        }

        private async void AppBar_Click(object sender, RoutedEventArgs e)
        {
            var tag = sender is Control c ? c?.Tag?.ToString()?.ToLower() : sender is string s ? s : "";
            if (tag == "unfavorite")
            {
                FavBtn.Tag = FavBtn.Content = "Favorite";
                await _vm.PocketHandler.Client.Unfavorite(_vm.PocketHandler.CurrentPocketItem);
                MainPage.Notifier.Show("Remove from Favorite", 2000);
            }
            else if (tag == "favorite")
            {
                FavBtn.Tag = FavBtn.Content = "Unfavorite";
                await _vm.PocketHandler.Client.Favorite(_vm.PocketHandler.CurrentPocketItem);
                MainPage.Notifier.Show("Saved as Favorite", 2000);
            }
            else if (tag == "share")
            {
                DataTransferManager.ShowShareUI();
            }
            else if (tag == "unarchive")
            {
                ArchiveBtn.Tag = "Archive";
                ArchiveBtn.Content = "";
                await ArchiveArticleAsync(_vm.PocketHandler.CurrentPocketItem, false, true);
            }
            else if (tag == "archive")
            {
                ArchiveBtn.Tag = "Unarchive";
                ArchiveBtn.Content = "";
                await ArchiveArticleAsync(_vm.PocketHandler.CurrentPocketItem, true, true);
            }
            else if (tag == "copy")
            {
                Utils.CopyToClipboard(_vm.PocketHandler.CurrentPocketItem?.Uri?.AbsoluteUri);
                MainPage.Notifier.Show("Copied", 2000);
            }
            else if (tag == "webview")
            {
                WebViewBtn.Tag = "articleView";
                WebViewBtn.Content = "Open in ArticleView";
                HandleViewVisibilities(tag);
                WebView.Navigate(_vm.PocketHandler.CurrentPocketItem?.Uri);
            }
            else if (tag == "articleview")
            {
                WebViewBtn.Tag = "webView";
                WebViewBtn.Content = "Open in WebView";
                HandleViewVisibilities(tag);
                OpenInArticleView(_vm.PocketHandler.CurrentPocketItem, true);
            }
            else if (tag == "delete") await DeleteArticleAsync(_vm.PocketHandler.CurrentPocketItem);
            else if (tag == "back") SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
        }

        private async Task ArchiveArticleAsync(PocketItem item, bool archiveIt, bool notify = true)
        {
            if (notify) MainPage.Notifier.Show(archiveIt ? "Archived" : "Added", 2000);
            if (archiveIt)
            {
                await _vm.PocketHandler.Client.Archive(item);
                _vm.ArticlesList.Remove(item);
            }
            else
            {
                await _vm.PocketHandler.Client.Unarchive(item);
                _vm.ArticlesList.Insert(0, item);
            }
        }

        private async Task DeleteArticleAsync(PocketItem pocketItem, bool notify = true)
        {
            try
            {
                if (pocketItem == null) return;
                await _vm.PocketHandler.Delete(pocketItem);
                _vm.ArticlesList.Remove(pocketItem);
                if (notify) MainPage.Notifier?.Show("Deleted", 2000);
            }
            catch { }
        }

        private async void OpenInArticleView(PocketItem item, bool force = false)
        {
            if (item == null) return;
            MarkdownLoading.IsLoading = true;
            TopBar.Visibility = Visibility.Collapsed;
            try
            {
                MarkdownText.Text = "";
                HandleViewVisibilities("markdownGrid");
                var content = await _vm.PocketHandler.Read(item?.Uri, force);
                if (item.ID != _vm.PocketHandler.CurrentPocketItem.ID) return;
                MarkdownText.Text = content;
                MarkdownText.UriPrefix = item?.Uri?.AbsoluteUri;
                HandleAppBarStatus();
                //BindingsUpdate();
                TopBar.Visibility = Visibility.Visible;
            }
            catch
            {
                if (item.ID == _vm.PocketHandler.CurrentPocketItem.ID) HandleViewVisibilities("errorView");
            }
            MarkdownLoading.IsLoading = false;
        }
    }
}