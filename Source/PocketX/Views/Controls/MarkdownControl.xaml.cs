using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PocketSharp.Models;
using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views.Dialog;

namespace PocketX.Views.Controls
{
    public sealed partial class MarkdownControl : UserControl, INotifyPropertyChanged
    {
        #region Parameters

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
                if (value == null || value == Article) return;
                SetValue(ArticleProperty, value);
                IsArchive = value?.IsArchive ?? false;
                IsInTextView = false; // AppBar_Click action based on IsInTextView
                AppBar_Click("view", null);
                Bindings.Update();
                WebView.NavigateToString("");
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
        private CancellationTokenSource _cancellationSource;
        private Settings Settings => SettingsHandler.Settings;
        private AudioHandler AudioHandler { get; }
        private ICommand _textToSpeech;
        private string _markdownText;
        private bool IsArchive { get; set; }
        private bool IsInTextView { get; set; } = true;

        #endregion

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
                    MarkdownText = await Utils.TextFromAssets(@"Assets\Markdown\Home.md");
            };
        }

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

        internal ICommand TextToSpeech => _textToSpeech ??
                                          (_textToSpeech =
                                              new SimpleCommand(async param => await AudioHandler.Toggle()));

        private async void Reload_ArticleView(object sender, RoutedEventArgs e) => await OpenInArticleView();

        private async void AppBar_Click(object sender, RoutedEventArgs e)
        {
            var tag = sender is Control c ? c?.Tag?.ToString()?.ToLower() : sender is string s ? s : "";
            switch (tag)
            {
                case "tag":
                    if (Utils.HasInternet)
                    {
                        await new AddDialog { PrimaryBtnText = "Save" }.ShowAsync();
                        OnPropertyChanged(nameof(Article));
                    }
                    else await UiUtils.ShowDialogAsync("You need to connect to the internet first");

                    break;
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
                case "error":
                    IsInTextView = true;
                    AppBar_Click("view", null);
                    break;
                case "view":
                    if (IsInTextView)
                    {
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
                        await OpenInArticleView();
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

        public async Task OpenInArticleView()
        {
            if (Article == null) return;
            try
            {
                if (_cancellationSource != null && _cancellationSource.Token != CancellationToken.None)
                    _cancellationSource.Cancel();
                MarkdownLoading.IsLoading = true;
                MarkdownAppBar.Visibility = Visibility.Collapsed;
                MarkdownText = "";
                MarkdownGrid.Visibility = Visibility.Visible;
                if (ErrorView != null) ErrorView.Visibility = Visibility.Collapsed;
                if (WebView != null) WebView.Visibility = Visibility.Collapsed;
                _cancellationSource = new CancellationTokenSource();
                var content = await PocketHandler.GetInstance().Read(Article?.ID, Article?.Uri, _cancellationSource);
                MarkdownCtrl.UriPrefix = Article?.Uri?.AbsoluteUri;
                MarkdownText = content;
                MarkdownAppBar.Visibility = Visibility.Visible;
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException || e is TaskCanceledException) return;
                MarkdownGrid.Visibility = Visibility.Collapsed;
                FindName(nameof(ErrorView));
                ErrorView.Visibility = Visibility.Visible;
                if (WebView != null) WebView.Visibility = Visibility.Collapsed;
            }
            finally
            {
                MarkdownLoading.IsLoading = false;
                _cancellationSource = null;
            }
        }
    }
}