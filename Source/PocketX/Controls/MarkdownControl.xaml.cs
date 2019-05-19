using System;
using System.ComponentModel;
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
        public MarkdownControl()
        {
            InitializeComponent();
            new MarkdownHandler(MarkdownCtrl);
            AudioHandler = new AudioHandler(Media, PocketHandler.GetInstance().TextProviderForAudioPlayer)
            {
                MediaStartAction = () => { MarkdownAppBar.MaxWidth = 48; },
                MediaEndAction = () => { MarkdownAppBar.MaxWidth = 500; }
            };
            MarkdownCtrl.Loaded += async (s, e) =>
            {
                if (string.IsNullOrEmpty(MarkdownText))
                    MarkdownText = await Utils.TextFromAssets(@"Assets\Icons\Home.md");
            };
        }

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region Parameters
        private Settings Settings => SettingsHandler.Settings;
        private AudioHandler AudioHandler { get; }
        private ICommand _textToSpeech;
        private string _markdownText;
        private bool IsArchive { get; set; }
        private bool IsInTextView { get; set; } = true;
        public string MarkdownText
        {
            get => _markdownText;
            set
            {
                _markdownText = value;
                OnPropertyChanged(nameof(MarkdownText));
            }
        }
        public PocketItem Article
        {
            get => (GetValue(ArticleProperty) is PocketItem i) ? i : null;
            set
            {
                if (value == null) return;
                SetValue(ArticleProperty, value);
                IsArchive = value?.IsArchive ?? false;
                IsInTextView = false; // AppBar_Click action based on IsInTextView
                AppBar_Click("view", null);
                Bindings.Update();
            }
        }

        public static readonly DependencyProperty ArticleProperty =
            DependencyProperty.Register("Article"
                , typeof(PocketItem)
                , typeof(MarkdownControl)
                , new PropertyMetadata(0));

        public Func<PocketItem, bool, Task> ToggleArchiveArticleAsync { get; set; }
        public Func<PocketItem, Task> DeleteArticleAsync { get; set; }
        public Func<PocketItem, Task> ToggleFavoriteArticleAsync { get; set; }
        #endregion

        #region SplitView
        public SplitView SplitView
        {
            get => (SplitView)GetValue(SplitViewProperty);
            set => SetValue(SplitViewProperty, value);
        }

        public static readonly DependencyProperty SplitViewProperty =
            DependencyProperty.Register("SplitView"
                , typeof(SplitView)
                , typeof(MainContent)
                , new PropertyMetadata(0));

        private void ToggleSplitView() => SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
        #endregion

        internal ICommand TextToSpeech => _textToSpeech ?? (_textToSpeech = new SimpleCommand(async param => await AudioHandler.Toggle()));
        private void Reload_ArticleView(object sender, RoutedEventArgs e) => OpenInArticleView(true);
        private async void AppBar_Click(object sender, RoutedEventArgs e)
        {
            var tag = sender is Control c ? c?.Tag?.ToString()?.ToLower() : sender is string s ? s : "";
            switch (tag)
            {
                case "favorite":
                    await ToggleFavoriteArticleAsync(Article);
                    Article.IsFavorite = !Article.IsFavorite;
                    OnPropertyChanged(nameof(Article));
                    break;
                case "share":
                    DataTransferManager.ShowShareUI();
                    break;
                case "archive":
                    await ToggleArchiveArticleAsync(Article, IsArchive);
                    IsArchive = !IsArchive;
                    OnPropertyChanged(nameof(IsArchive));
                    break;
                case "copy":
                    Utils.CopyToClipboard(Article?.Uri?.AbsoluteUri);
                    NotificationHandler.InAppNotification("Copied", 2000);
                    break;
                case "view":
                    if (IsInTextView)
                    {
                        FindName(nameof(WebView));
                        WebView.Visibility = Visibility.Visible;
                        MarkdownGrid.Visibility = Visibility.Collapsed;
                        if (ErrorView != null) ErrorView.Visibility = Visibility.Collapsed;
                        if (Article?.Uri != null) WebView.Navigate(Article.Uri);
                    }
                    else
                    {
                        MarkdownGrid.Visibility = Visibility.Visible;
                        if (ErrorView != null) ErrorView.Visibility = Visibility.Collapsed;
                        if (WebView != null) WebView.Visibility = Visibility.Collapsed;
                        OpenInArticleView(true);
                    }

                    IsInTextView = !IsInTextView;
                    OnPropertyChanged(nameof(IsInTextView));
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
                MarkdownAppBar.Visibility = Visibility.Visible;
            }
            catch
            {
                MarkdownGrid.Visibility = Visibility.Collapsed;
                FindName(nameof(ErrorView));
                ErrorView.Visibility = Visibility.Visible;
                if (WebView != null) WebView.Visibility = Visibility.Collapsed;
            }
            finally
            {
                MarkdownLoading.IsLoading = false;
            }
        }
    }
}
