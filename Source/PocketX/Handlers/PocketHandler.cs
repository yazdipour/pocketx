using Akavache;
using Microsoft.Toolkit.Uwp.Helpers;
using PocketSharp;
using PocketSharp.Models;
using ReadSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static Logger.Logger;

namespace PocketX.Handlers
{
    internal class PocketHandler : INotifyPropertyChanged
    {
        public PocketClient Client;
        public PocketUser User { get; set; }
        private static PocketHandler _pocketHandler;
        private PocketItem _currentPocketItem;
        public ObservableCollection<string> Tags { set; get; } = new ObservableCollection<string>();
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public PocketItem CurrentPocketItem
        {
            get => _currentPocketItem;
            set
            {
                _currentPocketItem = value;
                OnPropertyChanged(nameof(CurrentPocketItem));
            }
        }
        public static PocketHandler GetInstance() => _pocketHandler ?? (_pocketHandler = new PocketHandler());

        #region Login\Logout
        public void LoadCacheClient()
        {
            var cache = new LocalObjectStorageHelper().Read(Keys.PocketClientCache, "");
            Client = cache == "" ? null : new PocketClient(Keys.Pocket, cache);
            User = new LocalObjectStorageHelper().Read<PocketUser>(Keys.PocketClientCache + "user");
        }

        internal void Logout()
        {
            L("Logout");
            Client = null;
            User = null;
            _pocketHandler = null;
            SettingsHandler.Clear();
            BlobCache.LocalMachine.InvalidateAll();
            BlobCache.LocalMachine.Vacuum();
            new LocalObjectStorageHelper().Save(Keys.PocketClientCache, "");
            new LocalObjectStorageHelper().Save(Keys.PocketClientCache + "user", "");
        }

        public async Task<bool> LoginAsync()
        {
            User = await Client.GetUser();
            if (User == null) return false;
            new LocalObjectStorageHelper().Save(Keys.PocketClientCache, User.Code);
            new LocalObjectStorageHelper().Save(Keys.PocketClientCache + "user", User);
            return true;
        }

        public async Task<Uri> LoginUriAsync()
        {
            Client = new PocketClient(Keys.Pocket, callbackUri: App.Protocol);
            await Client.GetRequestCode();
            return Client.GenerateAuthenticationUri();
        }

        #endregion Login\Logout

        #region Cache Items

        internal async Task<IEnumerable<PocketItem>> GetItemsCache()
        {
            var ls = await BlobCache.LocalMachine.GetObject<List<string[]>>(Keys.MainList).Catch(Observable.Return(new List<string[]>()));
            var pls = new List<PocketItem>();
            foreach (var item in ls)
            {
                var pi = new PocketItem();
                try
                {
                    pi.ID = item[0];
                    pi.Uri = new Uri(item[1]);
                    pi.Title = item[2];
                    pi.LeadImage.Uri = new Uri(item[3]);
                }
                catch { }
                pls.Add(pi);
            }
            return pls;
        }

        private async Task SetItemsCache(IEnumerable<PocketItem> get)
        {
            var ls = new List<string[]>();
            var lsget = get.ToList();
            for (var i = 0; i < lsget.Count; i++)
            {
                var item = lsget[i];
                ls.Add(new[] { item.ID, item.Uri.AbsoluteUri, item.Title, item.LeadImage?.Uri?.AbsoluteUri });
                if (i == 60) break;
            }

            await BlobCache.LocalMachine.InsertObject(Keys.MainList, ls);
        }

        internal async Task SetItemCache(int index, PocketItem item)
        {
            var ls = await BlobCache.LocalMachine.GetObject<List<string[]>>(Keys.MainList).Catch(Observable.Return(new List<string[]>()));
            var itemGen = new[] { item.ID, item.Uri.AbsoluteUri, item.Title };
            ls.Insert(index, itemGen);
            await BlobCache.LocalMachine.InsertObject(Keys.MainList, ls);
        }

        #endregion Cache Items

        #region Tags
        public async Task FetchOnlineTagsAsync()
        {
            var tags = (await Client.GetTags()).ToArray().Select(o => o.Name).ToArray();
            if (!tags.Any()) return;
            Tags.Clear();
            foreach (var t in tags) Tags?.Add(t);
            if (Tags?.Count > 0)
            {
                OnPropertyChanged(nameof(Tags));
                await BlobCache.LocalMachine.InsertObject("tags", Tags);
            }
        }
        public async Task FetchOfflineTagsAsync()
        {
            foreach (var t in await BlobCache.LocalMachine.GetObject<IEnumerable<string>>("tags")) Tags?.Add(t);
            if (Tags?.Count > 0) OnPropertyChanged(nameof(Tags));
        }
        public async Task FetchTagsAsync()
        {
            if (Tags.Count > 0) return;
            try
            {
                await FetchOfflineTagsAsync();
            }
            catch (Exception e)
            {
                E(e);
            }
            try
            {
                await FetchOnlineTagsAsync();
            }
            catch (Exception e)
            {
                E(e);
            }
        }
        #endregion

        public async Task<PocketStatistics> UserStatistics() => await Client.GetUserStatistics();

        public async Task<string> Read(Uri url, bool force)
        {
            if (!force)
            {
                var cache = await BlobCache.LocalMachine.GetObject<string>(url?.AbsoluteUri)
                    .Catch(Observable.Return(""));
                if (cache?.Trim()?.Length > 0) return cache;
            }
            var r = await new Reader().Read(url, new ReadOptions { PrettyPrint = true, PreferHTMLEncoding = false });
            var content = BFound.HtmlToMarkdown.MarkDownDocument.FromHtml(r?.Content);
            content = content.Replace(".medium.com/freeze/max/60/", ".medium.com/freeze/max/360/"); //Fix Medium Images
            if (!(r?.Content?.Length > 0)) return content;
            await BlobCache.LocalMachine.InsertObject(url?.AbsoluteUri, content);
            await BlobCache.LocalMachine.InsertObject("plain_" + url?.AbsoluteUri, r?.PlainContent);
            return content;
        }

        public async Task<(string, string)> AddFromShare(Uri url)
        {
            const string success = "Successfully Saved to Pocket";
            const string failed = "FAILED (Be Sure You Are Logged In)";
            if (Client != null)
            {
                await Client.Add(url);
                return (success, url.AbsoluteUri);
            }
            try
            {
                _pocketHandler.LoadCacheClient();
                await _pocketHandler.Client.Add(url);
                return (success, url.AbsoluteUri);
            }
            catch (Exception e) { return (failed, e.Message); }
        }

        public async Task<IEnumerable<PocketItem>> GetListAsync(
            State state, bool? favorite, string tag, string search, int count, int offset)
        {
            try
            {
                if (!Utils.HasInternet)
                    throw new Exception();
                var pocketItems = await Client.Get(
                    state: state, favorite: favorite,
                    tag: tag, contentType: null,
                    sort: Sort.newest, search: search,
                    domain: null, since: null,
                    count: count, offset: offset);

                if (state == State.unread && tag == null && search == null && offset == 0)
                    await SetItemsCache(pocketItems);
                return pocketItems;
            }
            catch
            {
                return state == State.unread && tag == null && search == null && offset == 0 ? await GetItemsCache() : null;
            }
        }

        public async Task DeleteArticle(PocketItem pocketItem)
        {
            try
            {
                await ClearArticleCache(pocketItem);
                await Client.Delete(pocketItem);
            }
            catch (Exception e)
            {
                E(e);
            }
        }

        public async Task ArchiveArticle(PocketItem pocketItem)
        {
            try
            {
                await ClearArticleCache(pocketItem);
                await Client.Archive(pocketItem);
            }
            catch (Exception e)
            {
                E(e);
            }
        }
        public async Task ClearArticleCache(PocketItem pocketItem)
        {
            try
            {
                await BlobCache.LocalMachine.Invalidate(pocketItem.Uri.AbsoluteUri);
                await BlobCache.LocalMachine.Invalidate("plain_" + pocketItem.Uri.AbsoluteUri);
            }
            catch (Exception e)
            {
                E(e);
            }
        }

        public async Task<string> TextProviderForAudioPlayer()
            => await BlobCache.LocalMachine.GetObject<string>("plain_" + CurrentPocketItem?.Uri?.AbsoluteUri).Catch(Observable.Return(""));
    }
}