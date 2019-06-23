using Microsoft.Toolkit.Uwp.Helpers;
using PocketX.Models;

namespace PocketX.Handlers
{
    internal class SettingsHandler
    {
        public static Settings Settings { get; set; } = new Settings();

        public static void Load()
        {
            try
            {
                var temp = new LocalObjectStorageHelper().Read<Settings>(Keys.Settings);
                if (temp != null) Settings = temp;
            }
            catch { }
        }
        public static void Save() => new LocalObjectStorageHelper().Save(Keys.Settings, Settings);

        public static void Clear()
        {
            Settings = new Settings();
            Save();
        }
    }
}
