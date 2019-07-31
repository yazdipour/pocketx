using System.Collections.Generic;
using System.Threading.Tasks;

namespace CacheManager
{
    public class Lru<K, V>
    {
        private static LruCache<K, V> _lruCache;

        public static bool IsOpen => _lruCache != null;

        public static async Task Init(int capacity, string lruKey)
        {
            if (IsOpen) return;
            var oldDictionary = await CacheManager.GetObject<Dictionary<K, Node<K, V>>>(lruKey, null);
            _lruCache = new LruCache<K, V>(capacity, oldDictionary);
        }

        public static async Task SaveAllToCache(string key)
            => await CacheManager.InsertObject(key, _lruCache.GetAll());

        public static void Put(K key, V valueTuple) => _lruCache.Put(key, valueTuple);

        public static V Get(K key) => _lruCache.TryGetValue(key);

    }
}