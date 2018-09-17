using System;
using System.Collections.Generic;
using System.Linq;

using PocketX.Handlers;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PocketX.Views.Dialog
{
    public sealed partial class AddDialog : ContentDialog
    {
        private PocketHandler pocketHander = new PocketHandler();
        private IEnumerable<string> _selectedOptions = new string[0];
        public PocketSharp.Models.PocketItem pocketItem { get; set; }

        public AddDialog(ElementTheme app_theme)
        {
            this.InitializeComponent();
            this.RequestedTheme = app_theme;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Hide();

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var foo = pocketHander.Client.Add(new Uri(urlTextBox.Text.Trim()), _selectedOptions.Cast<string>().ToArray());
                pocketItem = await foo;
                foo.Wait();
                Hide();
            }
            catch { urlTextBox.Background = UIHandler.HexColorToSolidColor("#5fED243B"); }
        }

        private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
            => chipsList.AvailableChips = (await pocketHander.GetTagsAsync());
    }
}