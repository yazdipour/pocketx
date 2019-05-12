using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PocketX.Handlers;
using Windows.UI.Xaml.Controls;

namespace PocketX.Views.Dialog
{
    public sealed partial class TagsDialog : ContentDialog
    {
        private ObservableCollection<string> _tags;

        public TagsDialog()
        {
            this.InitializeComponent();
            Loaded += async (s, e) =>
                listView.ItemsSource = _tags = await PocketHandler.GetInstance().GetTagsAsync();
        }

        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Tag = "#" + e.ClickedItem;
            Hide();
        }

        private void searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var q = args.QueryText.Trim();
            listView.ItemsSource = q.Length > 0 ? _tags.ToList().FindAll(o => o.Contains(q)) : _tags.ToList();
        }
    }
}
