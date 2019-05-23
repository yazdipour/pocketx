using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp;
using PocketSharp.Models;
using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views;
using PocketX.Views.Dialog;

namespace PocketX.ViewModels
{
    internal class MainContentViewModel : INotifyPropertyChanged
    {
        public readonly IncrementalLoadingCollection<PocketIncrementalSource.Articles, PocketItem> ArticlesList
            = new IncrementalLoadingCollection<PocketIncrementalSource.Articles, PocketItem>();
        public readonly IncrementalLoadingCollection<PocketIncrementalSource.Archives, PocketItem> ArchivesList
            = new IncrementalLoadingCollection<PocketIncrementalSource.Archives, PocketItem>();
        public readonly IncrementalLoadingCollection<PocketIncrementalSource.Favorites, PocketItem> FavoritesList
            = new IncrementalLoadingCollection<PocketIncrementalSource.Favorites, PocketItem>();
        public ObservableCollection<PocketItem> SearchList = new ObservableCollection<PocketItem>();
        internal Settings Settings => SettingsHandler.Settings;
        internal PocketHandler PocketHandler => PocketHandler.GetInstance();
        public event PropertyChangedEventHandler PropertyChanged;
        private ICommand _addArticle;
        private bool _listIsLoading;
        public int PivotListSelectedIndex { get; set; }
        protected virtual void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public bool ListIsLoading
        {
            get => _listIsLoading;
            //|| ArticlesList.IsLoading || ArchivesList.IsLoading || FavoritesList.IsLoading;
            set
            {
                _listIsLoading = value;
                OnPropertyChanged(nameof(ListIsLoading));
            }
        }

        public ObservableCollection<PocketItem> CurrentList()
        {
            switch (PivotListSelectedIndex)
            {
                case 1:
                    return FavoritesList;
                case 2:
                    return ArchivesList;
                case 3:
                    return SearchList;
                default:
                    return ArticlesList;
            }
        }
        public async Task SearchCommand(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return;
            Logger.Logger.L("Search");
            q = Uri.EscapeUriString(q);
            SearchList.Clear();
            var task = q[0] == '#'
                ? PocketHandler.GetListAsync(state: State.all, favorite: null, tag: q.Substring(1), search: null, count: 40, 0)
                : PocketHandler.GetListAsync(state: State.all, favorite: null, tag: null, search: q, count: 40, offset: 0);
            ListIsLoading = true;
            foreach (var pocketItem in await task)
                SearchList.Add(pocketItem);
            OnPropertyChanged(nameof(SearchList));
            ListIsLoading = false;
        }
        internal ICommand AddArticle =>
            _addArticle ?? (_addArticle = new SimpleCommand(async param =>
            {
                if (!Utils.HasInternet)
                {
                    await UiUtils.ShowDialogAsync("You need to connect to the internet first");
                    return;
                }
                var dialog = new AddDialog();
                await dialog?.ShowAsync();
                if (dialog.PocketItem == null) return;
                ArticlesList.Insert(0, dialog.PocketItem);
                await PocketHandler.SetItemCache(0, dialog.PocketItem);
            }));
        internal async void PinBtnClicked() => await new UiUtils().PinAppWindow(520, 400);
        internal void ShareArticle(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            request.Data.SetText(PocketHandler?.CurrentPocketItem?.Uri?.ToString() ?? "");
            request.Data.Properties.Title = "Shared by PocketX";
        }
        public async Task ToggleArchiveArticleAsync(PocketItem pocketItem, bool IsArchive)
        {
            if (pocketItem == null) return;
            try
            {
                if (IsArchive) // Want to add
                {
                    await PocketHandler.Client.Unarchive(pocketItem);
                    NotificationHandler.InAppNotification("Added", 2000);
                    if (ArticlesList.Count > 0 && ArticlesList[0] != pocketItem) ArticlesList.Insert(0, pocketItem);
                    ArchivesList.Remove(pocketItem);
                }
                else // Want to Archive
                {
                    await PocketHandler.Client.Archive(pocketItem);
                    if (ArchivesList.Count > 0 && ArchivesList[0] != pocketItem) ArchivesList.Insert(0, pocketItem);
                    ArticlesList.Remove(pocketItem);
                    NotificationHandler.InAppNotification("Archived", 2000);
                }
            }
            catch (Exception e) { NotificationHandler.InAppNotification(e.Message, 2000); }
        }
        public async Task DeleteArticleAsync(PocketItem pocketItem)
        {
            if (pocketItem == null) return;
            await PocketHandler.Delete(pocketItem);
            CurrentList()?.Remove(pocketItem);
            NotificationHandler.InAppNotification("Deleted", 2000);
        }
        public async Task ToggleFavoriteArticleAsync(PocketItem pocketItem)
        {
            if (pocketItem == null) return;
            if (pocketItem.IsFavorite)
            {
                await PocketHandler.GetInstance().Client.Unfavorite(pocketItem);
                if (FavoritesList.Count != 0) FavoritesList.Remove(pocketItem);
                NotificationHandler.InAppNotification("Remove from Favorite", 2000);
            }
            else
            {
                await PocketHandler.GetInstance().Client.Favorite(pocketItem);
                if (FavoritesList.Count != 0) FavoritesList.Add(pocketItem);
                NotificationHandler.InAppNotification("Saved as Favorite", 2000);
            }
        }
        public void ItemRightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as PocketItem;
            var flyout = new MenuFlyout();
            var el = new MenuFlyoutItem { Text = "Copy Link", Icon = new SymbolIcon(Symbol.Copy) };
            el.Click += (sen, ee) =>
            {
                Utils.CopyToClipboard(item?.Uri?.AbsoluteUri);
                NotificationHandler.InAppNotification("Copied", 2000);
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
            el.Click += async (sen, ee) => await ToggleArchiveArticleAsync(item, item.IsArchive);
            flyout?.Items?.Insert(0, el);
            if (sender is StackPanel parent) flyout.ShowAt(parent, e.GetPosition(parent));
        }
        public async Task SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            var item = args.SwipeControl?.DataContext as PocketItem;
            if (sender.Text == "Delete")
                await DeleteArticleAsync(item);
        }
    }
}
