using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PocketSharp.Models;
using PocketX.Annotations;
using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views;

namespace PocketX.Controls
{
    public sealed partial class MarkdownControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            set => SetValue(ArticleProperty, value);
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
                    //OnPropertyChanged(nameof(ArchiveIcon));
                    break;
                case "copy":
                    Utils.CopyToClipboard(Article?.Uri?.AbsoluteUri);
                    MainContent.Notifier.Show("Copied", 2000);
                    break;
                case "webview":
                    WebViewBtn.Tag = "articleView";
                    WebViewBtn.Content = "Open in ArticleView";
                    HandleViewVisibilities(nameof(WebView));
                    if (Article?.Uri != null) WebView.Navigate(Article?.Uri);
                    break;
                case "articleview":
                    WebViewBtn.Tag = "webView";
                    WebViewBtn.Content = "Open in WebView";
                    HandleViewVisibilities(nameof(WebView));
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

        private void HandleViewVisibilities(string theOne)
        {
            FindName(theOne);
            try
            {
                switch (theOne)
                {
                    case nameof(WebView):
                        WebView.Visibility = Visibility.Visible;
                        MarkdownGrid.Visibility = Visibility.Collapsed;
                        ErrorView.Visibility = Visibility.Collapsed;
                        break;
                    case nameof(MarkdownGrid):
                        FindName(nameof(MarkdownCtrl));
                        MarkdownGrid.Visibility = Visibility.Visible;
                        ErrorView.Visibility = Visibility.Collapsed;
                        WebView.Visibility = Visibility.Collapsed;
                        break;
                    case nameof(ErrorView):
                        MarkdownGrid.Visibility = Visibility.Collapsed;
                        WebView.Visibility = Visibility.Collapsed;
                        break;
                }
            }
            catch { }
        }

        public async void OpenInArticleView(bool force = false)
        {
            if (Article == null) return;
            MarkdownLoading.IsLoading = true;
            MarkdownAppBar.Visibility = Visibility.Collapsed;
            try
            {
                MarkdownText = "";
                HandleViewVisibilities(nameof(MarkdownGrid));
                var content = await PocketHandler.GetInstance().Read(Article.Uri, force);
                //if (item.ID != Article?.ID) return;
                MarkdownCtrl.UriPrefix = Article.Uri?.AbsoluteUri;
                MarkdownText = content;
                WebViewBtn.Tag = "webView";
                WebViewBtn.Content = "Open in WebView";
                MarkdownAppBar.Visibility = Visibility.Visible;
            }
            catch
            {
                HandleViewVisibilities(nameof(ErrorView));
            }
            MarkdownLoading.IsLoading = false;
        }

    }
}
