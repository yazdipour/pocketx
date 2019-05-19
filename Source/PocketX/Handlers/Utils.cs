using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Networking.BackgroundTransfer;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Notifications;

namespace PocketX.Handlers
{
    class Utils
    {
        internal static int UnixTimeStamp() => (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        
        internal static bool CheckConnection => NetworkInformation.GetInternetConnectionProfile() != null;

        internal static async Task DownloadFile(string url, string name, StorageFolder folder)
        {
            try
            {
                if (folder == null)
                {
                    var picker = new FolderPicker
                    {
                        ViewMode = PickerViewMode.Thumbnail,
                        SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                        FileTypeFilter = { "*" }
                    };
                    folder = await picker.PickSingleFolderAsync();
                }
                CancellationToken token;
                if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri source))
                    throw new Exception("Invalid URI");
                var destinationFile = await folder.CreateFileAsync(name, CreationCollisionOption.GenerateUniqueName);
                var download = new BackgroundDownloader().CreateDownload(source, destinationFile);
                download.Priority = BackgroundTransferPriority.High;
                await download.StartAsync().AsTask(token);
            }
            catch (Exception e) { throw e; }
        }

        internal static System.Collections.Generic.List<string> GetAllFonts()
        {
            var fonts = Microsoft.Graphics.Canvas.Text.CanvasTextFormat.GetSystemFontFamilies();
            var fontList = new System.Collections.Generic.List<string>(fonts);
            fontList.Insert(0, "Segoe UI");
            fontList.Insert(0, "Times New Roman");
            fontList.Insert(0, "Arial");
            fontList.Insert(0, "Calibri");
            return fontList;
        }

        internal static void CopyToClipboard(string text)
        {
            var pkg = new Windows.ApplicationModel.DataTransfer.DataPackage();
            pkg.SetText(text??"");
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pkg);
        }

        internal static void ToastIt(string str1, string str2)
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(str1));
            toastTextElements[1].AppendChild(toastXml.CreateTextNode(str2));
            // Set the duration on the toast
            var toastNode = toastXml.SelectSingleNode("/toast");
            ((XmlElement)toastNode)?.SetAttribute("duration", "long");
            // Create the actual toast object using this toast specification.
            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        internal static async Task<string> TextFromAssets(string path)
        {
            var sFile = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(path);
            return FileIO.ReadTextAsync(sFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
