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
        private PocketHandler pocketHandler = new PocketHandler();
        private AudioHandler audioHandler = new AudioHandler();
        private IncrementalLoadingCollection<PocketHandler, PocketItem> myList = new IncrementalLoadingCollection<PocketHandler, PocketItem>();
        private Settings settings = SettingsHandler.Settings;

        private bool IsSmallWidth(double width) => width < 720;

        #region On PageInit

        public MainContent()
        {
            InitializeComponent();
            //pocketHandler.Client = pocketHandler.LoadCacheClient();
            DataTransferManager.GetForCurrentView().DataRequested += (sender, args) =>
            {
                DataRequest request = args.Request;
                request.Data.SetText(pocketHandler.currentPocketItem?.Uri?.ToString());
                request.Data.Properties.Title = "Shared by PocketX";
            };
            SizeChanged += (s, e) =>
            {
                if (!splitView.IsPaneOpen && !IsSmallWidth(e.NewSize.Width))
                {
                    MainGrid.Margin = new Thickness(0, 52, 0, 0);
                    splitView.IsPaneOpen = true;
                }
                else if (splitView.IsPaneOpen && IsSmallWidth(e.NewSize.Width))
                {
                    if (ActualWidth < 620)
                    {
                        MainGrid.Margin = new Thickness(0, 0, 0, 0);
                    }
                    splitView.IsPaneOpen = false;
                }
            };
            header_title.Text = "PocketX Tips";
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            markdownText.Text = await Utils.TextFromAssets(@"Assets\Icons\Home.md");
            if (!Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                listViewInSplitView.ItemsSource = await pocketHandler.GetItemsCache();
            else await ParentCommandAsync("MyList");
            try { await pocketHandler.GetTagsAsync(false); }
            catch { }
        }

        #endregion On PageInit

        #region Miscellaneous

        private async void Add_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                var dialog = new AddDialog(settings.app_theme);
                await dialog.ShowAsync();
                if (dialog.pocketItem == null) return;
                myList.Insert(0, dialog.pocketItem);
                await pocketHandler.SetItemCache(0, dialog.pocketItem);
            }
            else await new Windows.UI.Popups.MessageDialog("You need to connect to the internet first").ShowAsync();
        }

        private void Reload_ArticleView(object sender, RoutedEventArgs e) => OpenInArticleView(pocketHandler.currentPocketItem, true);

        private void HandleAppbarStatus()
        {
            var item = pocketHandler.currentPocketItem;
            WebViewBtn.Tag = "webView";
            WebViewBtn.Content = "Open in WebView";
            ArchiveBtn.Content = item.IsArchive ? "" : "";
            ArchiveBtn.Tag = item.IsArchive ? "Unarchive" : "Archive";
            FavBtn.Tag = FavBtn.Content = item.IsFavorite ? "Unfavorite" : "Favorite";
        }

        private void HandleViewVisibilies(string theOne)
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
            if (audioHandler.Media == null) audioHandler.Media = media;
            if (audioHandler.Media.CurrentState == MediaElementState.Playing)
            {
                audioHandler.Media.Stop();
                Media_MediaEnded(null, null);
            }
            else
            {
                string text = await BlobCache.LocalMachine.GetObject<string>('_' + pocketHandler.currentPocketItem?.Uri?.AbsoluteUri)
                    .Catch(Observable.Return(markdownText?.Text));
                if (!String.IsNullOrEmpty(text)) await audioHandler.Start(text);
                else await new Windows.UI.Popups.MessageDialog("No Content to Read").ShowAsync();
            }
        }

        private async void Media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
           => await new Windows.UI.Popups.MessageDialog(e.ErrorMessage).ShowAsync();

        private void Media_MediaOpened(object sender, RoutedEventArgs e)
        {
            Text2Speech_Button.Content = "";
            media.AreTransportControlsEnabled = true;
        }

        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            Text2Speech_Button.Content = "";
            media.AreTransportControlsEnabled = false;
        }

        #endregion MediaElement Events

        #region Controls Events

        private void splitView_PaneOpened(SplitView sender, object args)
        {
            if (BackBtn != null)
            {
                BackBtn.Content = splitView.IsPaneOpen ? new SymbolIcon(Symbol.Back) : new SymbolIcon(Symbol.Forward);
                if (!splitView.IsPaneOpen && pocketHandler.currentPocketItem != null)
                    TopBar.Visibility = Visibility.Visible;
                else if (splitView.IsPaneOpen && IsSmallWidth(ActualWidth))
                    TopBar.Visibility = Visibility.Collapsed;
            }
        }

        private void listViewInSplitView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (splitView.IsPaneOpen && IsSmallWidth(ActualWidth)) splitView.IsPaneOpen = false;
            pocketHandler.currentPocketItem = e.ClickedItem as PocketItem;
            OpenInArticleView(pocketHandler.currentPocketItem);
            FindName("TopBar");
        }

        private void gridView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as PocketItem;
            MenuFlyout myFlyout = new MenuFlyout();
            Style style = new Style { TargetType = typeof(MenuFlyoutPresenter) };
            style.Setters.Add(new Setter(RequestedThemeProperty, settings.app_theme));
            myFlyout.MenuFlyoutPresenterStyle = style;
            MenuFlyoutItem el = new MenuFlyoutItem { Text = "Copy Link", Icon = new SymbolIcon(Symbol.Copy) };
            el.Click += (sen, ee) => { Utils.CopyToClipboard(item.Uri.AbsoluteUri); MainPage.Notifier.Show("Copied", 2000); };
            myFlyout.Items.Add(el);
            el = new MenuFlyoutItem { Text = "Open in browser", Icon = new SymbolIcon(Symbol.World) };
            el.Click += async (sen, ee) => { await Launcher.LaunchUriAsync(item.Uri); };
            myFlyout.Items.Add(el);

            el = new MenuFlyoutItem { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) };
            el.Click += async (sen, ee) =>
            {
                await DeleteArticleAsync(item);
            };
            myFlyout.Items.Add(el);
            if (!splitView.Tag.ToString().Contains("Fav"))
            {
                el = new MenuFlyoutItem
                {
                    Text = item.IsArchive ? "Add" : "Archive",
                    Icon = new SymbolIcon(item.IsArchive ? Symbol.Add : Symbol.Accept)
                };
                el.Click += async (sen, ee) => { await ArchiveArticleAsync(item, !item.IsArchive); };
                myFlyout.Items.Insert(0, el);
            }
            var senderElement = sender as GridView;
            myFlyout.ShowAt(senderElement, e.GetPosition(senderElement));
        }

        private async void SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            var item = args.SwipeControl?.DataContext as PocketItem;
            switch (sender.Text)
            {
                case "Add":
                    await ArchiveArticleAsync(item, false);
                    break;

                case "Archive":
                    await ArchiveArticleAsync(item, true);
                    break;

                case "Delete":
                    await DeleteArticleAsync(item);
                    break;
            }
        }

        #endregion Controls Events

        #region Markdown

        private async void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri link))
                await Launcher.LaunchUriAsync(link);
        }

        private async void MarkdownText_ImageClicked(object sender, LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri link))
                await new ImageDialog(link).ShowAsync();
        }

        #endregion Markdown

        public async Task ParentCommandAsync(string tag, int count = 40, int offset = 0)
        {
            if (tag?.Length < 2) return;
            LoadingControl.IsLoading = true;
            if (!splitView.IsPaneOpen) splitView.IsPaneOpen = true;
            splitView.Tag = tag;
            leftSwipeItems.Clear();
            try
            {
                switch (tag)
                {
                    case "MyList":
                        leftSwipeItems.Add(leftSwipeArchive);
                        if (count == 0)
                        {
                            await myList.RefreshAsync();
                            break;
                        }
                        Utils.AppCenterLog($"Tap {tag}");
                        listViewInSplitView.ItemsSource = myList;
                        BindingsUpdate();
                        break;

                    case "Favorites":
                        Utils.AppCenterLog($"Tap {tag}");
                        listViewInSplitView.ItemsSource = await pocketHandler.GetListAsync(
                            state: State.all,
                            favorite: true,
                            tag: null,
                            search: null,
                            count: count,
                            offset: offset);
                        break;

                    case "Archives":
                        Utils.AppCenterLog($"Tap {tag}");
                        listViewInSplitView.ItemsSource = await pocketHandler.GetListAsync(
                            state: State.archive,
                            favorite: null,
                            tag: null,
                            search: null,
                            count: count,
                            offset: offset);
                        leftSwipeItems.Add(leftSwipeAdd);
                        break;

                    default:

                        IEnumerable<PocketItem> search_list = null;
                        if (tag[0] == '#')
                        {
                            Utils.AppCenterLog($"Tag {tag}");
                            search_list = await pocketHandler.GetListAsync(
                                state: State.all,
                                favorite: null,
                                tag: tag.Substring(1),
                                search: null,
                                count: count,
                                offset: offset);
                        }
                        else
                        {
                            Utils.AppCenterLog("Search");
                            search_list = await pocketHandler.GetListAsync(
                                state: State.all,
                                favorite: null,
                                tag: null,
                                search: tag,
                                count: count,
                                offset: offset);
                        }
                        listViewInSplitView.ItemsSource = search_list;
                        leftSwipeItems.Add(leftSwipeArchive);
                        break;
                }
            }
            catch { }
            LoadingControl.IsLoading = false;
        }

        private async void Appbar_Click(object sender, RoutedEventArgs e)
        {
            String tag = "";
            if (sender is Control) tag = (sender as Control)?.Tag?.ToString();
            else if (sender is string) tag = sender as string;
            Utils.AppCenterLog($"Tap {tag}");
            switch (tag)
            {
                case "Unfavorite":
                    FavBtn.Tag = FavBtn.Content = "Favorite";
                    await pocketHandler.Client.Unfavorite(pocketHandler.currentPocketItem);
                    MainPage.Notifier.Show("Remove from Favorite", 2000);
                    break;

                case "Favorite":
                    FavBtn.Tag = FavBtn.Content = "Unfavorite";
                    await pocketHandler.Client.Favorite(pocketHandler.currentPocketItem);
                    MainPage.Notifier.Show("Saved as Favorite", 2000);
                    break;

                case "Share":
                    DataTransferManager.ShowShareUI();
                    break;

                case "Unarchive":
                    ArchiveBtn.Tag = "Archive";
                    ArchiveBtn.Content = "";
                    await ArchiveArticleAsync(pocketHandler.currentPocketItem, false, true);
                    break;

                case "Archive":
                    ArchiveBtn.Tag = "Unarchive";
                    ArchiveBtn.Content = "";
                    await ArchiveArticleAsync(pocketHandler.currentPocketItem, true, true);
                    break;

                case "Copy":
                    Utils.CopyToClipboard(pocketHandler.currentPocketItem?.Uri?.AbsoluteUri);
                    MainPage.Notifier.Show("Copied", 2000);
                    break;

                case "webView":
                    WebViewBtn.Tag = "articleView";
                    WebViewBtn.Content = "Open in ArticleView";
                    HandleViewVisibilies(tag);
                    webView.Navigate(pocketHandler.currentPocketItem?.Uri);
                    break;

                case "articleView":
                    WebViewBtn.Tag = "webView";
                    WebViewBtn.Content = "Open in WebView";
                    HandleViewVisibilies(tag);
                    OpenInArticleView(pocketHandler.currentPocketItem, true);
                    break;

                case "Delete":
                    await DeleteArticleAsync(pocketHandler.currentPocketItem);
                    break;

                case "Back":
                    splitView.IsPaneOpen = !splitView.IsPaneOpen;
                    break;
            }
        }

        private async Task ArchiveArticleAsync(PocketItem item, bool archiveIt, bool notify = true)
        {
            if (notify) MainPage.Notifier.Show(archiveIt ? "Archived" : "Added", 2000);
            if (archiveIt)
            {
                await pocketHandler.Client.Archive(item);
                myList.Remove(item);
            }
            else
            {
                await pocketHandler.Client.Unarchive(item);
                myList.Insert(0, item);
            }
        }

        private async Task DeleteArticleAsync(PocketItem pocketItem, bool notify = true)
        {
            try
            {
                if (pocketItem == null) return;
                await pocketHandler.Delete(pocketItem);
                myList.Remove(pocketItem);
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
                HandleViewVisibilies("markdownGrid");
                var content = await pocketHandler.Read(item?.Uri, force);
                if (item.ID != pocketHandler.currentPocketItem.ID) return;
                markdownText.Text = content;
                markdownText.UriPrefix = item?.Uri?.AbsoluteUri;
                HandleAppbarStatus();
                BindingsUpdate();
                TopBar.Visibility = Visibility.Visible;
            }
            catch
            {
                if (item.ID == pocketHandler.currentPocketItem.ID) HandleViewVisibilies("errorView");
            }
            MarkdownLoading.IsLoading = false;
        }
    }
}