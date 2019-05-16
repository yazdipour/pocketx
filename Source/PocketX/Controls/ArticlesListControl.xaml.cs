using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PocketSharp.Models;
using PocketX.Handlers;
using PocketX.Views;

namespace PocketX.Controls
{
    public sealed partial class ArticlesListControl : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<PocketItem>), typeof(ArticlesListControl), new PropertyMetadata(default(ObservableCollection<PocketItem>)));
        public ArticlesListControl() => InitializeComponent();
        public ObservableCollection<PocketItem> ItemsSource
        {
            get => (ObservableCollection<PocketItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        public Action<PocketItem> OpenArticle { get; set; }
        
        public Func<PocketItem, Task> ToggleArchiveArticleAsync { get; set; }
        public Func<PocketItem, Task> DeleteArticleAsync { get; set; }
        public Func<PocketItem, Task> ToggleFavoriteArticleAsync { get; set; }

        private async void SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            var item = args.SwipeControl?.DataContext as PocketItem;
            if (sender.Text == "Delete")
                await DeleteArticleAsync(item);
        }

        private void ItemClick(object sender, ItemClickEventArgs e)
        {
            if ((e?.ClickedItem as ListViewItem)?.DataContext is PocketItem item) OpenArticle(item);
        }

        private void ItemRightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as PocketItem;
            var flyout = new MenuFlyout();
            var el = new MenuFlyoutItem { Text = "Copy Link", Icon = new SymbolIcon(Symbol.Copy) };
            el.Click += (sen, ee) =>
            {
                Utils.CopyToClipboard(item?.Uri?.AbsoluteUri);
                MainContent.Notifier.Show("Copied", 2000);
            };
            flyout?.Items?.Add(el);
            el = new MenuFlyoutItem { Text = "Open in browser", Icon = new SymbolIcon(Symbol.World) };
            el.Click += async (sen, ee) => await Launcher.LaunchUriAsync(item?.Uri);
            flyout?.Items?.Add(el);
            el = new MenuFlyoutItem { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) };
            el.Click += async (sen, ee) => await DeleteArticleAsync(item);
            flyout?.Items?.Add(el);
            el = new MenuFlyoutItem
            {
                Text = item?.IsArchive ?? false ? "Add" : "Archive",
                Icon = new SymbolIcon(item?.IsArchive ?? false ? Symbol.Add : Symbol.Accept)
            };
            el.Click += async (sen, ee) => await ToggleArchiveArticleAsync(item);
            flyout?.Items?.Insert(0, el);
            if (sender is StackPanel parent) flyout.ShowAt(parent, e.GetPosition(parent));
        }
    }
}
