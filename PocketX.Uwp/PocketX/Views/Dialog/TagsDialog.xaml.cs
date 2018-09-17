using System.Collections.Generic;
using PocketX.Handlers;
using Windows.UI.Xaml.Controls;

namespace PocketX.Views.Dialog
{
    public sealed partial class TagsDialog : ContentDialog
    {
        private List<string> tags;

        public TagsDialog()
        {
            this.InitializeComponent();
            Loaded += async (s, e) =>
                listView.ItemsSource = tags = await new PocketHandler().GetTagsAsync();
        }

        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Tag = "#"+e.ClickedItem as string;
            Hide();
        }

        private void searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var q = args.QueryText.Trim();
            listView.ItemsSource = q.Length > 0 ? tags?.FindAll(o => o.Contains(q)) : tags;
        }
    }
}
