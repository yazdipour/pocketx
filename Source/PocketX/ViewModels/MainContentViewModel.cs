using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Input;
using Akavache;
using Microsoft.Toolkit.Uwp;
using PocketSharp.Models;
using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views.Dialog;

namespace PocketX.ViewModels
{
    internal class MainContentViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        internal MarkdownHandler MarkdownHandler { get; set; }
        internal Settings Settings => SettingsHandler.Settings;
        internal PocketHandler PocketHandler => PocketHandler.GetInstance();
        internal AudioHandler AudioHandler { get; set; }

        internal readonly IncrementalLoadingCollection<PocketHandler, PocketItem> ArticlesList
            = new IncrementalLoadingCollection<PocketHandler, PocketItem>();

        private ICommand _textToSpeech;
        private ICommand _addArticle;
        internal ICommand TextToSpeech
            => _textToSpeech ?? (_textToSpeech = new SimpleCommand(async param => await AudioHandler.Toggle()));

        internal ICommand AddArticle =>
            _addArticle ?? (_addArticle = new SimpleCommand(async param =>
            {
                if (Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation
                    .IsInternetAvailable)
                {
                    var dialog = new AddDialog(Settings.AppTheme);
                    await dialog?.ShowAsync();
                    if (dialog.PocketItem == null) return;
                    ArticlesList.Insert(0, dialog.PocketItem);
                    await PocketHandler.SetItemCache(0, dialog.PocketItem);
                }
                else await UiUtils.ShowDialogAsync("You need to connect to the internet first");
            }));

        internal async Task<string> TextProviderForAudioPlayer()
        {
            return await BlobCache.LocalMachine.GetObject<string>
                    ("plain_" + PocketHandler.CurrentPocketItem?.Uri?.AbsoluteUri)
                .Catch(Observable.Return(MarkdownHandler.Text));
        }

        internal void ShareArticle(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            request.Data.SetText(PocketHandler?.CurrentPocketItem?.Uri?.ToString() ?? "");
            request.Data.Properties.Title = "Shared by PocketX";
        }
    }
}
