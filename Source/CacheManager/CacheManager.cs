using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using static Akavache.BlobCache;

namespace CacheManager
{
    public static class CacheManager
    {
        public static void Init(Type type) => ApplicationName = type.Namespace;

        public static void Kill()
        {
            LocalMachine.InvalidateAll();
            LocalMachine.Vacuum();
        }

        public static async Task<T> GetObject<T>(string key, T defaultValue)
            => await LocalMachine.GetObject<T>(key).Catch(Observable.Return(defaultValue));

        public static async Task InsertObject<T>(string key, T value)
            => await LocalMachine.InsertObject(key, value);
    }
}