using Akavache;

using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp.Helpers;

using PocketSharp;
using PocketSharp.Models;

using ReadSharp;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;

namespace PocketX.Handlers
{
    internal class PocketHandler : IIncrementalSource<PocketItem>
    {
        public PocketClient Client { get; set; }
        public PocketItem CurrentPocketItem;
        private static PocketHandler _pocketHandler;
        private ObservableCollection<string> _tags = new ObservableCollection<string>();

        #region Login\Logout

        public static PocketHandler GetInstance() => _pocketHandler ?? (_pocketHandler = new PocketHandler());


        public PocketClient LoadCacheClient()
        {
            string cache = new LocalObjectStorageHelper().Read(Keys.PocketClientCache, "");
            if (cache == "") return null;
            return new PocketClient(Keys.Pocket, cache);
        }

        private void SaveCacheUser(PocketUser user)
            => new LocalObjectStorageHelper().Save(Keys.PocketClientCache, user.Code);

        internal void Logout(Frame frame)
        {
            Logger.Logger.L("Logout");
            Client = null;
            SettingsHandler.Clear();
            BlobCache.LocalMachine.InvalidateAll();
            BlobCache.LocalMachine.Vacuum();
            new LocalObjectStorageHelper().Save(Keys.PocketClientCache, "");
            frame.Navigate(typeof(Views.LoginPage));
            frame.BackStack.Clear();
        }

        internal async Task<bool> LoginAsync()
        {
            var user = await Client.GetUser();
            if (user == null) return false;
            SaveCacheUser(user);
            return true;
        }

        internal async Task<Uri> LoginUriAsync()
        {
            Client = new PocketClient(Keys.Pocket, callbackUri: App.Protocol);
            string requestCode = await Client.GetRequestCode();
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
                await _pocketHandler.LoadCacheClient().Add(url);
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
                if (offset == 0) await SetItemsCache(pocketItems);
                return pocketItems;
            }
            catch
            {
                if (offset == 0) return await GetItemsCache();
                else return null;
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

        internal async Task<ObservableCollection<string>> GetTagsAsync(bool cache = true)
        {
            if (_tags?.Count > 0) return _tags;
            if (cache)
            {
                _tags = await BlobCache.LocalMachine.GetObject<ObservableCollection<string>>("tags");
                    //.Catch(Observable.Return(new List<string>()));
                if (_tags?.Count > 0) return _tags;
            }
            var tags = (await Client.GetTags()).ToList().Select(o => o.Name);
            await BlobCache.LocalMachine.InsertObject("tags", _tags);
            _tags?.Clear();
            foreach (var t in tags) _tags?.Add(t);
            return _tags;
        }

        public async Task<IEnumerable<PocketItem>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default(CancellationToken))
            => await GetListAsync(State.unread, false, null, null, pageSize, pageIndex * pageSize);

        internal async Task Delete(PocketItem pocketItem)
        {
            await Client.Delete(pocketItem);
            await BlobCache.LocalMachine.Invalidate(pocketItem.Uri.AbsoluteUri);
            await BlobCache.LocalMachine.Invalidate("plain_" + pocketItem.Uri.AbsoluteUri);
        }
    }
}