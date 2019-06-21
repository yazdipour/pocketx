using Microsoft.Toolkit.Uwp.Helpers;
using PocketSharp;
using PocketSharp.Models;
using ReadSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Logger.Logger;
using Cache = CacheManager.CacheManager;
using Lru = CacheManager.Lru<string, string>;

namespace PocketX.Handlers
{
    internal class PocketHandler : INotifyPropertyChanged
    {
        public PocketClient Client;
        public PocketUser User { get; set; }
        public ObservableCollection<string> Tags { set; get; } = new ObservableCollection<string>();
        public event PropertyChangedEventHandler PropertyChanged;
        private static PocketHandler _pocketHandler;
        private PocketItem _currentPocketItem;
        private Reader _reader;
        private const string LruKey = "ArticlesContent";
        private const int LruCapacity = 20;
        private readonly LocalObjectStorageHelper _localCache = new LocalObjectStorageHelper();
        protected virtual void OnPropertyChanged(string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

        #region Login-Logout

        public void LoadCacheClient()
        {
            var cache = _localCache.Read(Keys.PocketClientCache, "");
            Client = cache == "" ? null : new PocketClient(Keys.Pocket, cache);
            User = _localCache.Read<PocketUser>(Keys.PocketClientCache + "user");
        }

        internal void Logout()
        {
            L("Logout");
            Client = null;
            User = null;
            _pocketHandler = null;
            SettingsHandler.Clear();
            Cache.Kill();
            _localCache.Save(Keys.PocketClientCache, "");
            _localCache.Save(Keys.PocketClientCache + "user", "");
        }

        public async Task<bool> LoginAsync()
        {
            User = await Client.GetUser();
            if (User == null) return false;
            _localCache.Save(Keys.PocketClientCache, User.Code);
            _localCache.Save(Keys.PocketClientCache + "user", User);
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
            var ls = await Cache.GetObject(Keys.MainList, new List<string[]>());
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
                catch (Exception e)
                {
                    E(e);
                }

                pls.Add(pi);
            }

            return pls;
        }

        private static async Task PutItemsInCache(IEnumerable<PocketItem> get)
        {
            var ls = new List<string[]>();
            var lsget = get.ToList();
            for (var i = 0; i < lsget.Count; i++)
            {
                var item = lsget[i];
                ls.Add(new[] { item.ID, item.Uri.AbsoluteUri, item.Title, item.LeadImage?.Uri?.AbsoluteUri });
                if (i == 60) break;
            }

            await Cache.InsertObject(Keys.MainList, ls);
        }

        internal async Task PutItemInCache(int index, PocketItem item)
        {
            var ls = await Cache.GetObject(Keys.MainList, new List<string[]>());
            var itemGen = new[] { item.ID, item.Uri.AbsoluteUri, item.Title };
            ls.Insert(index, itemGen);
            await Cache.InsertObject(Keys.MainList, ls);
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
                await Cache.InsertObject("tags", Tags);
            }
        }

        public async Task FetchOfflineTagsAsync()
        {
            foreach (var t in await Cache.GetObject<IEnumerable<string>>("tags", null))
                if (t != null)
                    Tags?.Add(t);
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

        public async Task<string> Read(string id, Uri url, CancellationTokenSource cancellationSource)
        {
            if (!Lru.IsOpen)
            {
                var old = await Cache.GetObject<Dictionary<string, CacheManager.Node<string, string>>>(LruKey, null);
                Lru.Init(LruCapacity, old);
            }

            var cacheContent = Lru.Get(id);
            if (cacheContent?.Length > 0) return HtmlToMarkdown(cacheContent);
            if (_reader == null)
            {
                var options = HttpOptions.CreateDefault();
                options.RequestTimeout = 60;
                options.UseMobileUserAgent = true;
                _reader = new Reader(options);
            }
            var readContent = await _reader.Read(url,
                new ReadOptions { PrettyPrint = true, PreferHTMLEncoding = true, HasHeaderTags = false, UseDeepLinks = true },
                cancellationSource.Token);
            //Fix Medium Images
            var content = readContent?.Content.Replace(".medium.com/freeze/max/60/", ".medium.com/freeze/max/360/");
            if (readContent?.Content?.Length < 1) return content;
            Lru.Put(id, content);
            await Lru.SaveAllToCache(LruKey);
            return HtmlToMarkdown(content);
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
            catch (Exception e)
            {
                return (failed, e.Message);
            }
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
                    await PutItemsInCache(pocketItems);
                return pocketItems;
            }
            catch
            {
                return state == State.unread && tag == null && search == null && offset == 0
                    ? await GetItemsCache()
                    : null;
            }
        }

        public async Task DeleteArticle(PocketItem pocketItem)
        {
            try
            {
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
                await Client.Archive(pocketItem);
            }
            catch (Exception e)
            {
                E(e);
            }
        }

        public string TextProviderForAudioPlayer() => HtmlToRaw(Lru.Get(CurrentPocketItem?.ID));

        private static string HtmlToRaw(string html) => HtmlUtilities.ConvertToPlainText(html);

        private static string HtmlToMarkdown(string html) => BFound.HtmlToMarkdown.MarkDownDocument.FromHtml(html);
    }
}