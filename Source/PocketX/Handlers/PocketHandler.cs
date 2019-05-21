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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PocketX.Handlers
{
    internal class PocketHandler : INotifyPropertyChanged
    {
        public PocketClient Client;
        private static PocketHandler _pocketHandler;
        private PocketItem _currentPocketItem;
        public ObservableCollection<string> Tags { set; get; } = new ObservableCollection<string>();
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public PocketItem CurrentPocketItem
        {
            get => _currentPocketItem;
            set
            {
                _currentPocketItem = value;
                OnPropertyChanged(nameof(CurrentPocketItem));
            }
        }

        public PocketUser User { get; set; }

        #region Login\Logout

        public static PocketHandler GetInstance() => _pocketHandler ?? (_pocketHandler = new PocketHandler());

        public async void LoadCacheClient()
        {
            var cache = new LocalObjectStorageHelper().Read(Keys.PocketClientCache, "");
            Client = cache == "" ? null : new PocketClient(Keys.Pocket, cache);
            try { if (Client != null) User = await Client.GetUser(); }
            catch { }
        }

        private void SaveCacheUser(PocketUser user)
            => new LocalObjectStorageHelper().Save(Keys.PocketClientCache, user.Code);

        internal void Logout()
        {
            Logger.Logger.L("Logout");
            Client = null;
            _pocketHandler = null;
            SettingsHandler.Clear();
            BlobCache.LocalMachine.InvalidateAll();
            BlobCache.LocalMachine.Vacuum();
            new LocalObjectStorageHelper().Save(Keys.PocketClientCache, "");
        }

        internal async Task<bool> LoginAsync()
        {
            User = await Client.GetUser();
            if (User == null) return false;
            SaveCacheUser(User);
            return true;
        }

        internal async Task<Uri> LoginUriAsync()
        {
            Client = new PocketClient(Keys.Pocket, callbackUri: App.Protocol);
            await Client.GetRequestCode();
            return Client.GenerateAuthenticationUri();
        }

        #endregion Login\Logout

        internal async Task<(string, string)> AddFromShare(Uri url)
        {
            var SUCCESS = "Successfully Saved to Pocket";
            var FAILED = "FAILED (Be Sure You Are Logged In)";
            if (Client != null)
            {
                await Client.Add(url);
                return (SUCCESS, url.AbsoluteUri);
            }
            try
            {
                _pocketHandler.LoadCacheClient();
                await _pocketHandler.Client.Add(url);
                return (SUCCESS, url.AbsoluteUri);
            }
            catch (Exception e) { return (FAILED, e.Message); }
        }

        internal async Task<IEnumerable<PocketItem>> GetListAsync(
            State state, bool? favorite, string tag, string search, int count, int offset)
        {
            try
            {
                if (!Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
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

        internal async Task<string> Read(Uri url, bool force)
        {
            var cache = await BlobCache.LocalMachine.GetObject<string>(url?.AbsoluteUri)
                .Catch(Observable.Return(""));
            if (!force && cache?.Trim()?.Length > 0) return cache;
            var r = await new Reader().Read(url, new ReadOptions { PrettyPrint = true, PreferHTMLEncoding = false });
            var content = BFound.HtmlToMarkdown.MarkDownDocument.FromHtml(r?.Content);
            //Fix Medium Images
            content = content.Replace(".medium.com/freeze/max/60/", ".medium.com/freeze/max/360/");
            if (!(r?.Content?.Length > 0)) return content;
            await BlobCache.LocalMachine.InsertObject(url?.AbsoluteUri, content);
            await BlobCache.LocalMachine.InsertObject("plain_" + url?.AbsoluteUri, r?.PlainContent);
            return content;
        }

        internal async Task FetchTagsAsync()
        {
            try
            {
                var tags = (await Client.GetTags()).ToArray().Select(o => o.Name).ToArray();
                if (!tags.Any()) return;
                Tags.Clear();
                foreach (var t in tags) Tags?.Add(t);
                await BlobCache.LocalMachine.InsertObject("tags", Tags);
            }
            catch (Exception e)
            {
                Logger.Logger.E(e);
            }
            if (Tags?.Count > 0)
            {
                OnPropertyChanged(nameof(Tags));
                return;
            }
            try
            {
                foreach (var t in await BlobCache.LocalMachine.GetObject<IEnumerable<string>>("tags")) Tags?.Add(t);
            }
            catch (Exception e)
            {
                Logger.Logger.E(e);
            }
            OnPropertyChanged(nameof(Tags));
        }

        internal async Task Delete(PocketItem pocketItem)
        {
            try
            {
                await BlobCache.LocalMachine.Invalidate(pocketItem.Uri.AbsoluteUri);
                await BlobCache.LocalMachine.Invalidate("plain_" + pocketItem.Uri.AbsoluteUri);
                await Client.Delete(pocketItem);
            }
            catch (Exception e)
            {
                Logger.Logger.E(e);
            }
        }

        public async Task<string> TextProviderForAudioPlayer()
            => await BlobCache.LocalMachine.GetObject<string>("plain_" + CurrentPocketItem?.Uri?.AbsoluteUri).Catch(Observable.Return(""));
    }
}