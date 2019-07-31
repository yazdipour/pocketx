using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using static Akavache.BlobCache;

namespace CacheManager
{
    public static class CacheManager
    {
        public static void Init(Type type)
        {
            ApplicationName = type.Namespace;
            //Akavache.Sqlite3.Registrations.Start(type.Namespace, SQLitePCL.Batteries_V2.Init);
        }

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