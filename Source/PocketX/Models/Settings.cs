using Windows.UI.Xaml;
using Newtonsoft.Json;

namespace PocketX.Models
{
    internal class Settings
    {
        [JsonProperty("app_theme")]
        public ElementTheme AppTheme = ElementTheme.Light;

        //READER
        [JsonProperty("reader_bg")]
        public string ReaderBg = "#ffffff";
        [JsonProperty("reader_theme")]
        public ElementTheme ReaderTheme = ElementTheme.Light;
        [JsonProperty("reader_font_size")]
        public int ReaderFontSize = 16;
        [JsonProperty("reader_font_family")]
        public string ReaderFontFamily = "Calibri";
        [JsonIgnore] public string Thumbnail { get; set; }
    }
}
