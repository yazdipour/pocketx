using Microsoft.Toolkit.Uwp.Helpers;

namespace PocketX.Models
{
    class SettingsHandler
    {
        public static Settings Settings = new Settings();
        

        public static void Load()
        {
            try
            {
                var temp = new LocalObjectStorageHelper().Read<Settings>(Handlers.Keys.Settings);
                if (temp != null) Settings = temp;
            }
            catch { }
        }
        public static void Save() => new LocalObjectStorageHelper().Save(Handlers.Keys.Settings, Settings);

        public static void Clear()
        {
            Settings = new Settings();
            Save();
        }
    }
}
