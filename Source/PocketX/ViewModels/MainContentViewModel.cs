using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Toolkit.Uwp;
using PocketSharp.Models;
using PocketX.Annotations;
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
        public ObservableCollection<PocketItem> SearchList { get; set; } = new ObservableCollection<PocketItem>();
        public int PivotListSelectedIndex { get; set; }
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
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                ((INotifyPropertyChanged)ArticlesList).PropertyChanged += value;
                ((INotifyPropertyChanged)ArchivesList).PropertyChanged += value;
                ((INotifyPropertyChanged)FavoritesList).PropertyChanged += value;
                ((INotifyPropertyChanged)SearchList).PropertyChanged += value;
            }
            remove
            {
                ((INotifyPropertyChanged)ArticlesList).PropertyChanged -= value;
                ((INotifyPropertyChanged)ArchivesList).PropertyChanged -= value;
                ((INotifyPropertyChanged)FavoritesList).PropertyChanged -= value;
                ((INotifyPropertyChanged)SearchList).PropertyChanged -= value;
            }
        }

        internal Settings Settings => SettingsHandler.Settings;
        internal PocketHandler PocketHandler => PocketHandler.GetInstance();

        private ICommand _addArticle;
        private ICommand _topAppBarClick;
        //internal async void PinBtnClicked() => await new UiUtils().PinAppWindow(520, 400);
        public async Task SearchCommand(string q)
        {
            Logger.Logger.L("Search");
            foreach (var pocketItem in await PocketHandler.GetListAsync(state: State.all, favorite: null, tag: null, search: q, count: 40, offset: 0)) SearchList.Add(pocketItem);
        }

        public async void LoadTagCommand(string tag)
        {
            Logger.Logger.L($"Tag {tag}");
            SearchList = (ObservableCollection<PocketItem>)await PocketHandler.GetListAsync(
                state: State.all,
                favorite: null,
                tag: tag.Substring(1),
                search: null,
                count: 40,
                offset: 0);
        }

        //public async void LoadArchiveCommand()
        //{
        //    Logger.Logger.L("Tap LoadArchiveCommand");
        //    ArchivesList = await PocketHandler.GetListAsync(
        //        state: State.archive,
        //        favorite: null,
        //        tag: null,
        //        search: null,
        //        count: 40,
        //        offset: 0);
        //}

        //public async void LoadFavCommand()
        //{
        //    Logger.Logger.L($"Tap Fav");
        //    FavoritesList = (ObservableCollection<PocketItem>)await PocketHandler.GetListAsync(
        //        state: State.all,
        //        favorite: true,
        //        tag: null,
        //        search: null,
        //        count: 40,
        //        offset: 0);
        //}
        public async void LoadHomeCommand()
        {
            Logger.Logger.L("Tap Home");
            if (ArticlesList.Count == 0) await ArticlesList.RefreshAsync();
        }
        internal ICommand AddArticle =>
            _addArticle ?? (_addArticle = new SimpleCommand(async param =>
            {
                if (Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation
                    .IsInternetAvailable)
                {
                    var dialog = new AddDialog(Settings.AppTheme);
                    await dialog?.ShowAsync();
                    if (dialog.PocketItem == null) return;
                    ArticlesList.Insert(0, dialog.PocketItem);
                    await PocketHandler.SetItemCache(0, dialog.PocketItem);
                }
                else await UiUtils.ShowDialogAsync("You need to connect to the internet first");
            }));

        internal ICommand TopAppBarClick =>
            _topAppBarClick ?? (_topAppBarClick = new SimpleCommand(async param =>
            {
                var dialog = new SettingsDialog(0);
                await dialog.ShowAsync();
                if (dialog.Tag?.ToString() == Keys.Logout)
                {
                    PocketHandler.GetInstance().Logout();
                    //frame?.Navigate(typeof(Views.LoginPage));
                    //frame?.BackStack.Clear();
                    return;
                }
                OnPropertyChanged(nameof(Settings));
            }));


        internal async void PinBtnClicked() => await new UiUtils().PinAppWindow(520, 400);
        internal void ShareArticle(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            request.Data.SetText(PocketHandler?.CurrentPocketItem?.Uri?.ToString() ?? "");
            request.Data.Properties.Title = "Shared by PocketX";
        }

        public async Task ToggleArchiveAsync([CanBeNull] PocketItem pocketItem)
        {
            if (pocketItem == null) return;
            try
            {
                if (pocketItem.IsArchive)
                {
                    await PocketHandler.Client.Unarchive(pocketItem);
                    MainContent.Notifier.Show("Added", 2000);
                    ArticlesList.Insert(0, pocketItem);
                    ArchivesList.Remove(pocketItem);
                }
                else
                {
                    await PocketHandler.Client.Archive(pocketItem);
                    ArchivesList.Insert(0, pocketItem);
                    ArticlesList.Remove(pocketItem);
                    MainContent.Notifier.Show("Archived", 2000);
                }
            }
            catch (Exception e) { MainContent.Notifier.Show(e.Message, 2000); }
        }

        public async Task DeleteArticleAsync([CanBeNull] PocketItem pocketItem)
        {
            if (pocketItem == null) return;
            await PocketHandler.Delete(pocketItem);
            CurrentList()?.Remove(pocketItem);
            MainContent.Notifier?.Show("Deleted", 2000);
        }

        public async Task ToggleFavoriteArticleAsync([CanBeNull] PocketItem pocketItem)
        {
            if (pocketItem == null) return;
            if (pocketItem.IsFavorite)
            {
                await PocketHandler.GetInstance().Client.Unfavorite(pocketItem);
                FavoritesList.Remove(pocketItem);
                MainContent.Notifier.Show("Remove from Favorite", 2000);
            }
            else
            {
                await PocketHandler.GetInstance().Client.Favorite(pocketItem);
                FavoritesList.Add(pocketItem);
                MainContent.Notifier.Show("Saved as Favorite", 2000);
            }
        }
    }
}
