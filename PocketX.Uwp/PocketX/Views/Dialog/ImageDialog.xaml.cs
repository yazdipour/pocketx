using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PocketX.Views.Dialog
{
    public sealed partial class ImageDialog : ContentDialog
    {
        Uri uri;
        public ImageDialog(Uri link)
        {
            this.InitializeComponent();
            uri = link;
            DataTransferManager.GetForCurrentView().DataRequested += (sender, args) =>
            {
                DataRequest request = args.Request;
                request.Data.SetText(uri?.ToString());
                request.Data.Properties.Title = "Shared by PocketX";
            };
        }

        private async void Appbar_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Control)?.Tag?.ToString();
            switch (tag)
            {
                case "Save":
                    await Handlers.Utils.DownloadFile(uri.AbsoluteUri, Handlers.Utils.UnixTimeStamp() + ".jpg", null);
                    break;
                case "Share":
                    DataTransferManager.ShowShareUI();
                    break;
                default:
                    Hide();
                    break;
            }
        }
    }
}
