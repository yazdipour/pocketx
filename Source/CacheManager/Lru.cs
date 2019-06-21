using System.Collections;
using System.Threading.Tasks;

namespace CacheManager
{
    public class Lru<K, V>
    {
        private static LruCache<K, V> _lruCache;

        public static bool IsOpen => _lruCache != null;

        public static void Init(int capacity, IDictionary oldDictionary) => _lruCache = new LruCache<K, V>(capacity, oldDictionary);

        public static async Task SaveAllToCache(string key)
            => await CacheManager.InsertObject(key, _lruCache.GetAll());

        public static void Put(K key, V valueTuple) => _lruCache.Put(key, valueTuple);

        public static V Get(K key) => _lruCache.Get(key);

    }
}