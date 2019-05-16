using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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

        public ArticlesListControl()
        {
            this.InitializeComponent();
        }

        public ObservableCollection<PocketItem> ItemsSource
        {
            get => (ObservableCollection<PocketItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private async void SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            //var item = args.SwipeControl?.DataContext as PocketItem;
            //if (string.Equals(sender.Text, "Add", StringComparison.OrdinalIgnoreCase))
            //    await ArchiveFuncAsync(item, false);
            //else if (string.Equals(sender.Text, "Archive", StringComparison.OrdinalIgnoreCase))
            //    await ArchiveFuncAsync(item, true);
            //else if (sender.Text == "Delete")
            //    await DeleteFuncAsync(item);
        }

        private void ListViewInSplitView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void GridView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {

        }


        //private void gridView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        //{
        //    var item = ((FrameworkElement)e.OriginalSource).DataContext as PocketItem;
        //    var flyout = new MenuFlyout();
        //    var style = new Style { TargetType = typeof(MenuFlyoutPresenter) };
        //    style.Setters.Add(new Setter(RequestedThemeProperty, _vm.Settings.AppTheme));
        //    flyout.MenuFlyoutPresenterStyle = style;
        //    var el = new MenuFlyoutItem { Text = "Copy Link", Icon = new SymbolIcon(Symbol.Copy) };
        //    el.Click += (sen, ee) =>
        //    {
        //        Utils.CopyToClipboard(item?.Uri?.AbsoluteUri);
        //        MainContent.Notifier.Show("Copied", 2000);
        //    };
        //    flyout?.Items?.Add(el);
        //    el = new MenuFlyoutItem { Text = "Open in browser", Icon = new SymbolIcon(Symbol.World) };
        //    el.Click += async (sen, ee) => await Launcher.LaunchUriAsync(item?.Uri);
        //    flyout.Items.Add(el);
        //    el = new MenuFlyoutItem { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) };
        //    el.Click += async (sen, ee) => await DeleteArticleAsync(item);
        //    flyout.Items.Add(el);
        //    if (!(SplitView.Tag ?? "").ToString().Contains("Fav"))
        //    {
        //        el = new MenuFlyoutItem
        //        {
        //            Text = item.IsArchive ? "Add" : "Archive",
        //            Icon = new SymbolIcon(item.IsArchive ? Symbol.Add : Symbol.Accept)
        //        };
        //        el.Click += async (sen, ee) => { await ToggleArchiveArticleAsync(item, !item.IsArchive); };
        //        flyout.Items.Insert(0, el);
        //    }
        //    if (sender is GridView senderElement) flyout.ShowAt(senderElement, e.GetPosition(senderElement));
        //}

    }
}
