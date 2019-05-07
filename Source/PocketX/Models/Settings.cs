using Windows.UI.Xaml;

namespace PocketX.Models
{
    class Settings
    {
        //APP
        public ElementTheme app_theme = ElementTheme.Light;
        public Windows.UI.Xaml.Controls.ScrollBarVisibility listview_scrollbar = Windows.UI.Xaml.Controls.ScrollBarVisibility.Hidden;

        //READER
        public string reader_bg = "#ffffff";
        public ElementTheme reader_theme = ElementTheme.Light;
        public int reader_font_size = 16;
        public string reader_font_family = "Calibri";
    }
}
