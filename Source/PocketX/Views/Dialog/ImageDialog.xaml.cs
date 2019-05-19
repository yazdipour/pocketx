using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PocketX.Views.Dialog
{
    public sealed partial class ImageDialog : ContentDialog
    {
        internal Uri Uri;
        public ImageDialog(Uri link)
        {
            this.InitializeComponent();
            Uri = link;
            DataTransferManager.GetForCurrentView().DataRequested += (sender, args) =>
            {
                var request = args.Request;
                request.Data.SetText(Uri?.ToString() ?? "");
                request.Data.Properties.Title = "Shared by PocketX";
            };
        }

        private async void AppBar_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Control)?.Tag?.ToString();
            switch (tag)
            {
                case "Save":
                    await Handlers.Utils.DownloadFile(Uri.AbsoluteUri, Handlers.Utils.UnixTimeStamp() + ".jpg", null);
                    break;
                case "Share":
                    DataTransferManager.ShowShareUI();
                    break;
                default:
                    Hide();
                    break;
            }
        }

        private void ImageTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) => Hide();
    }
}
