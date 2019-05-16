using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;

namespace PocketX.Controls
{
    public sealed partial class TagsListControl : UserControl
    {
        private ObservableCollection<string> _tags;

        public TagsListControl()
        {
            this.InitializeComponent();
            //listView.ItemsSource = _tags = await PocketHandler.GetInstance().GetTagsAsync();

        }

        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Tag = "#" + e.ClickedItem;
            //Hide();
        }

        private void searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var q = args.QueryText.Trim();
            //listView.ItemsSource = q.Length > 0 ? _tags.ToList().FindAll(o => o.Contains(q)) : _tags.ToList();
        }
    }
}
