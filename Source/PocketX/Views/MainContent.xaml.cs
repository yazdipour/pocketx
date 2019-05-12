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

namespace PocketX.Views
{
    public sealed partial class MainContent : Page
    {
        private readonly PocketHandler _pocketHandler = new PocketHandler();
        private readonly AudioHandler _audioHandler = new AudioHandler();
        private readonly IncrementalLoadingCollection<PocketHandler, PocketItem> _myList = new IncrementalLoadingCollection<PocketHandler, PocketItem>();
        private readonly Settings _settings = SettingsHandler.Settings;

        private static bool IsSmallWidth(double width) => width < 720;

        #region On PageInit

        public MainContent()
        {
            InitializeComponent();
            //pocketHandler.Client = pocketHandler.LoadCacheClient();
            header_title.Text = "PocketX Tips";
            DataTransferManager.GetForCurrentView().DataRequested += (sender, args) =>
            {
                var request = args.Request;
                request.Data.SetText(_pocketHandler?.CurrentPocketItem?.Uri?.ToString() ?? "");
                request.Data.Properties.Title = "Shared by PocketX";
            };
            SizeChanged += (s, e) =>
            {
                if (!splitView.IsPaneOpen && !IsSmallWidth(e.NewSize.Width))
                    splitView.IsPaneOpen = true;
                else if (splitView.IsPaneOpen && IsSmallWidth(e.NewSize.Width)) splitView.IsPaneOpen = false;
            };
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            markdownText.Text = await Utils.TextFromAssets(@"Assets\Icons\Home.md");
            if (!Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                ListViewInSplitView.ItemsSource = await _pocketHandler.GetItemsCache();
            else await ParentCommandAsync("Home");
            try { await _pocketHandler.GetTagsAsync(false); }
            catch { }
        }

        #endregion On PageInit

        #region Miscellaneous

        private async void Add_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                var dialog = new AddDialog(_settings.AppTheme);
                await dialog.ShowAsync();
                if (dialog.PocketItem == null) return;
                _myList.Insert(0, dialog.PocketItem);
                await _pocketHandler.SetItemCache(0, dialog.PocketItem);
            }
            else await new Windows.UI.Popups.MessageDialog("You need to connect to the internet first").ShowAsync();
        }

        private void Reload_ArticleView(object sender, RoutedEventArgs e) => OpenInArticleView(_pocketHandler.CurrentPocketItem, true);

        private void HandleAppBarStatus()
        {
            var item = _pocketHandler.CurrentPocketItem;
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
                    webView.Visibility = Visibility.Visible;
                    markdownGrid.Visibility = Visibility.Collapsed;
                    try { errorView.Visibility = Visibility.Collapsed; }
                    catch { }
                    break;

                case "markdownGrid":
                    FindName("markdownText");
                    markdownGrid.Visibility = Visibility.Visible;
                    try { errorView.Visibility = Visibility.Collapsed; }
                    catch { }
                    try { webView.Visibility = Visibility.Collapsed; }
                    catch { }
                    break;

                case "errorView":
                    errorView.Visibility = Visibility.Visible;
                    markdownGrid.Visibility = Visibility.Collapsed;
                    try { webView.Visibility = Visibility.Collapsed; }
                    catch { }
                    break;
            }
        }

        internal void BindingsUpdate() => Bindings.Update();

        #endregion Miscellaneous

        #region MediaElement Events

        private async void Text2Speech_Click(object sender, RoutedEventArgs e)
        {
            if (_audioHandler.Media == null) _audioHandler.Media = media;
            if (_audioHandler.Media.CurrentState == MediaElementState.Playing)
            {
                _audioHandler.Media.Stop();
                Media_MediaEnded(null, null);
            }
            else
            {
                TopBar.MaxWidth = 48;
                var text = await BlobCache.LocalMachine.GetObject<string>('_' + _pocketHandler.CurrentPocketItem?.Uri?.AbsoluteUri)
                    .Catch(Observable.Return(markdownText?.Text));
                if (!string.IsNullOrEmpty(text)) await _audioHandler.Start(text);
                else await new Windows.UI.Popups.MessageDialog("No Content to Read").ShowAsync();
            }
        }

        private async void Media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
           => await new Windows.UI.Popups.MessageDialog(e.ErrorMessage).ShowAsync();

        private void Media_MediaOpened(object sender, RoutedEventArgs e)
        {
            Text2SpeechButton.Content = "";
            media.AreTransportControlsEnabled = true;
        }

        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            Text2SpeechButton.Content = "";
            media.AreTransportControlsEnabled = false;
            TopBar.MaxWidth = 500;
        }

        #endregion MediaElement Events

        #region Controls Events

        private void splitView_PaneOpened(SplitView sender, object args)
        {
            if (BackBtn == null) return;
            BackBtn.Glyph = splitView.IsPaneOpen ? "" : "";
            if (!splitView.IsPaneOpen && _pocketHandler.CurrentPocketItem != null)
                TopBar.Visibility = Visibility.Visible;
            else if (splitView.IsPaneOpen && IsSmallWidth(ActualWidth))
                TopBar.Visibility = Visibility.Collapsed;
        }

        private void listViewInSplitView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (splitView.IsPaneOpen && IsSmallWidth(ActualWidth)) splitView.IsPaneOpen = false;
            _pocketHandler.CurrentPocketItem = e.ClickedItem as PocketItem;
            OpenInArticleView(_pocketHandler.CurrentPocketItem);
            FindName("TopBar");
        }

        private void gridView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as PocketItem;
            var flyout = new MenuFlyout();
            var style = new Style { TargetType = typeof(MenuFlyoutPresenter) };
            style.Setters.Add(new Setter(RequestedThemeProperty, _settings.AppTheme));
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
            if (!(splitView.Tag ?? "").ToString().Contains("Fav"))
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

        #region Markdown

        private async void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out var link))
                await Launcher.LaunchUriAsync(link);
        }

        private async void MarkdownText_ImageClicked(object sender, LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out var link))
                await new ImageDialog(link).ShowAsync();
        }

        #endregion Markdown

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
            => await ParentCommandAsync(args?.QueryText);

        public async Task ParentCommandAsync(string tag, int count = 40, int offset = 0)
        {
            if ((tag??"").Length < 2) return;
            LoadingControl.IsLoading = true;
            if (!splitView.IsPaneOpen) splitView.IsPaneOpen = true;
            splitView.Tag = tag;

            //leftSwipeItems.Clear();
            try
            {
                switch (tag)
                {
                    case "Home":
                        leftSwipeItems.Add(leftSwipeArchive);
                        if (count == 0)
                        {
                            await _myList.RefreshAsync();
                            break;
                        }
                        Logger.Logger.L($"Tap {tag}");
                        ListViewInSplitView.ItemsSource = _myList;
                        BindingsUpdate();
                        break;

                    case "Favorites":
                        Logger.Logger.L($"Tap {tag}");
                        ListViewInSplitView.ItemsSource = await _pocketHandler.GetListAsync(
                            state: State.all,
                            favorite: true,
                            tag: null,
                            search: null,
                            count: count,
                            offset: offset);
                        break;

                    case "Archives":
                        Logger.Logger.L($"Tap {tag}");
                        ListViewInSplitView.ItemsSource = await _pocketHandler.GetListAsync(
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
                            searchList = await _pocketHandler.GetListAsync(
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
                            searchList = await _pocketHandler.GetListAsync(
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

        private async void Appbar_Click(object sender, RoutedEventArgs e)
        {
            var tag = sender is Control c ? c?.Tag?.ToString()?.ToLower() : sender is string s ? s : "";
            switch (tag)
            {
                case "unfavorite":
                    FavBtn.Tag = FavBtn.Content = "Favorite";
                    await _pocketHandler.Client.Unfavorite(_pocketHandler.CurrentPocketItem);
                    MainPage.Notifier.Show("Remove from Favorite", 2000);
                    break;

                case "favorite":
                    FavBtn.Tag = FavBtn.Content = "Unfavorite";
                    await _pocketHandler.Client.Favorite(_pocketHandler.CurrentPocketItem);
                    MainPage.Notifier.Show("Saved as Favorite", 2000);
                    break;

                case "share":
                    DataTransferManager.ShowShareUI();
                    break;

                case "unarchive":
                    ArchiveBtn.Tag = "Archive";
                    ArchiveBtn.Content = "";
                    await ArchiveArticleAsync(_pocketHandler.CurrentPocketItem, false, true);
                    break;

                case "archive":
                    ArchiveBtn.Tag = "Unarchive";
                    ArchiveBtn.Content = "";
                    await ArchiveArticleAsync(_pocketHandler.CurrentPocketItem, true, true);
                    break;

                case "copy":
                    Utils.CopyToClipboard(_pocketHandler.CurrentPocketItem?.Uri?.AbsoluteUri);
                    MainPage.Notifier.Show("Copied", 2000);
                    break;

                case "webview":
                    WebViewBtn.Tag = "articleView";
                    WebViewBtn.Content = "Open in ArticleView";
                    HandleViewVisibilities(tag);
                    webView.Navigate(_pocketHandler.CurrentPocketItem?.Uri);
                    break;

                case "articleview":
                    WebViewBtn.Tag = "webView";
                    WebViewBtn.Content = "Open in WebView";
                    HandleViewVisibilities(tag);
                    OpenInArticleView(_pocketHandler.CurrentPocketItem, true);
                    break;

                case "delete":
                    await DeleteArticleAsync(_pocketHandler.CurrentPocketItem);
                    break;

                case "back":
                    splitView.IsPaneOpen = !splitView.IsPaneOpen;
                    break;
            }
        }

        private async Task ArchiveArticleAsync(PocketItem item, bool archiveIt, bool notify = true)
        {
            if (notify) MainPage.Notifier.Show(archiveIt ? "Archived" : "Added", 2000);
            if (archiveIt)
            {
                await _pocketHandler.Client.Archive(item);
                _myList.Remove(item);
            }
            else
            {
                await _pocketHandler.Client.Unarchive(item);
                _myList.Insert(0, item);
            }
        }

        private async Task DeleteArticleAsync(PocketItem pocketItem, bool notify = true)
        {
            try
            {
                if (pocketItem == null) return;
                await _pocketHandler.Delete(pocketItem);
                _myList.Remove(pocketItem);
                if (notify) MainPage.Notifier.Show("Deleted", 2000);
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
                markdownText.Text = "";
                HandleViewVisibilities("markdownGrid");
                var content = await _pocketHandler.Read(item?.Uri, force);
                if (item.ID != _pocketHandler.CurrentPocketItem.ID) return;
                markdownText.Text = content;
                markdownText.UriPrefix = item?.Uri?.AbsoluteUri;
                HandleAppBarStatus();
                BindingsUpdate();
                TopBar.Visibility = Visibility.Visible;
            }
            catch
            {
                if (item.ID == _pocketHandler.CurrentPocketItem.ID) HandleViewVisibilities("errorView");
            }
            MarkdownLoading.IsLoading = false;
        }
    }
}