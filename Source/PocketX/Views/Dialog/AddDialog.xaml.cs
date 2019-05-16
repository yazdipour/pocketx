﻿using System;
using System.Collections.Generic;
using System.Linq;

using PocketX.Handlers;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PocketX.Views.Dialog
{
    public sealed partial class AddDialog : ContentDialog
    {
        private IEnumerable<string> _selectedOptions = new string[0];
        public PocketSharp.Models.PocketItem PocketItem { get; set; }

        public AddDialog(ElementTheme appTheme)
        {
            this.InitializeComponent();
            this.RequestedTheme = appTheme;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Hide();

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var foo = PocketHandler.GetInstance().Client.Add(new Uri(urlTextBox.Text.Trim()), _selectedOptions.Cast<string>().ToArray());
                PocketItem = await foo;
                foo.Wait();
                Hide();
            }
            catch { urlTextBox.Background = UiUtils.HexColorToSolidColor("#5fED243B"); }
        }

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
            => chipsList.AvailableChips = PocketHandler.GetInstance().Tags;
    }
}