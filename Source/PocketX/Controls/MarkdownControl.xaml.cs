using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PocketSharp.Models;
using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views;

namespace PocketX.Controls
{
    public sealed partial class MarkdownControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public MarkdownControl()
        {
            InitializeComponent();
            MarkdownHandler = new MarkdownHandler(MarkdownCtrl);
            AudioHandler = new AudioHandler(Media, PocketHandler.GetInstance().TextProviderForAudioPlayer)
            {
                MediaStartAction = () => { MarkdownAppBar.MaxWidth = 48; },
                MediaEndAction = () => { MarkdownAppBar.MaxWidth = 500; }
            };
            MarkdownCtrl.Loaded += async (s, e)
                => MarkdownText = await Utils.TextFromAssets(@"Assets\Icons\Home.md");
        }
        internal Settings Settings => SettingsHandler.Settings;
        internal MarkdownHandler MarkdownHandler { get; set; }
        private AudioHandler AudioHandler { get; }
        private ICommand _textToSpeech;
        private string _markdownText;

        public string MarkdownText
        {
            get => _markdownText;
            set
            {
                _markdownText = value;
                OnPropertyChanged(nameof(MarkdownText));
            }
        }
        public string FavLabel => Article?.IsFavorite ?? false ? "UnFavorite" : "Favorite";
        public string ArchiveLabel => Article?.IsArchive ?? false ? "Add" : "Archive";
        public IconElement ArchiveIcon => new SymbolIcon((Article?.IsArchive ?? false) ? Symbol.Add : Symbol.Accept);
        internal ICommand TextToSpeech => _textToSpeech ?? (_textToSpeech = new SimpleCommand(async param => await AudioHandler.Toggle()));
        private void Reload_ArticleView(object sender, RoutedEventArgs e) => OpenInArticleView(true);

        #region SplitView
        public SplitView SplitView
        {
            get => (SplitView)GetValue(SplitViewProperty);
            set
            {
                SetValue(SplitViewProperty, value);
                SplitView.PaneOpened += SplitViewOnPaneOpened;
            }
        }

        private void SplitViewOnPaneOpened(SplitView sender, object args)
        {
            if (BackBtn == null) return;
            BackBtn.Glyph = SplitView.IsPaneOpen ? "" : "";
        }

        public static readonly DependencyProperty SplitViewProperty =
            DependencyProperty.Register("SplitView"
                , typeof(SplitView)
                , typeof(MainContent)
                , new PropertyMetadata(0));

        private void ToggleSplitView() => SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
        #endregion

        #region Parameters
        public PocketItem Article
        {
            get => (PocketItem)GetValue(ArticleProperty);
            set
            {
                SetValue(ArticleProperty, value);
                AppBar_Click("articleview", null);
                Bindings.Update();
            }
        }

        public static readonly DependencyProperty ArticleProperty =
            DependencyProperty.Register("Article"
                , typeof(PocketItem)
                , typeof(MarkdownControl)
                , new PropertyMetadata(0));

        public Func<PocketItem, Task> ToggleArchiveArticleAsync { get; set; }
        public Func<PocketItem, Task> DeleteArticleAsync { get; set; }
        public Func<PocketItem, Task> ToggleFavoriteArticleAsync { get; set; }
        #endregion

        private async void AppBar_Click(object sender, RoutedEventArgs e)
        {
            var tag = sender is Control c ? c?.Tag?.ToString()?.ToLower() : sender is string s ? s : "";
            switch (tag)
            {
                case "favorite":
                    await ToggleFavoriteArticleAsync(Article);
                    OnPropertyChanged(nameof(FavLabel));
                    break;
                case "share":
                    DataTransferManager.ShowShareUI();
                    break;
                case "archive":
                    await ToggleArchiveArticleAsync(Article);
                    OnPropertyChanged(nameof(ArchiveLabel));
                    break;
                case "copy":
                    Utils.CopyToClipboard(Article?.Uri?.AbsoluteUri);
                    NotificationHandler.InAppNotification("Copied", 2000);
                    break;
                case "webview":
                    WebViewBtn.Tag = "articleView";
                    WebViewBtn.Content = "Open in ArticleView";

                    FindName(nameof(WebView));
                    WebView.Visibility = Visibility.Visible;
                    MarkdownGrid.Visibility = Visibility.Collapsed;
                    if (ErrorView != null) ErrorView.Visibility = Visibility.Collapsed;

                    if (Article?.Uri != null) WebView.Navigate(Article.Uri);
                    break;
                case "articleview":
                    WebViewBtn.Tag = "webView";
                    WebViewBtn.Content = "Open in WebView";

                    MarkdownGrid.Visibility = Visibility.Visible;
                    if (ErrorView != null) ErrorView.Visibility = Visibility.Collapsed;
                    if (WebView != null) WebView.Visibility = Visibility.Collapsed;

                    OpenInArticleView(true);
                    break;
                case "delete":
                    await DeleteArticleAsync(Article);
                    break;
                case "back":
                    ToggleSplitView();
                    break;
            }
        }

        public async void OpenInArticleView(bool force = false)
        {
            if (Article == null) return;
            try
            {
                MarkdownLoading.IsLoading = true;
                MarkdownAppBar.Visibility = Visibility.Collapsed;
                MarkdownText = "";

                MarkdownGrid.Visibility = Visibility.Visible;
                if (ErrorView != null) ErrorView.Visibility = Visibility.Collapsed;
                if (WebView != null) WebView.Visibility = Visibility.Collapsed;

                var content = await PocketHandler.GetInstance().Read(Article?.Uri, force);
                MarkdownCtrl.UriPrefix = Article?.Uri?.AbsoluteUri;
                MarkdownText = content;
                WebViewBtn.Tag = "webView";
                WebViewBtn.Content = "Open in WebView";
                MarkdownAppBar.Visibility = Visibility.Visible;
            }
            catch
            {
                MarkdownGrid.Visibility = Visibility.Collapsed;
                FindName(nameof(ErrorView));
                ErrorView.Visibility = Visibility.Visible;
                if (WebView != null) WebView.Visibility = Visibility.Collapsed;
            }
            MarkdownLoading.IsLoading = false;
        }

    }
}
