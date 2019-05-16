using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using PocketX.Handlers;

namespace PocketX.Controls
{
    public sealed partial class TagsListControl : UserControl
    {
        private IEnumerable<string> Tags => PocketHandler.GetInstance().Tags;
        public Func<string, Task> SearchAsync { get; set; }
        public TagsListControl() => InitializeComponent();
        private async void ItemClick(object sender, ItemClickEventArgs e) => await SearchAsync("#" + e.ClickedItem);
    }
}
