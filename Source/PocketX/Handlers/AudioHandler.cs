using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PocketX.Handlers
{
    internal class AudioHandler
    {
        private readonly MediaElement _media;

        public AudioHandler(MediaElement media, Func<string> textProviderFunc)
        {
            _media = media;
            _media.MediaFailed += OnMediaFailed;
            _media.MediaOpened += OnMediaOpened;
            _media.MediaEnded += OnMediaEnded;
            TextProvider = textProviderFunc;
        }

        public async void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
            => await UiUtils.ShowDialogAsync(e.ErrorMessage);

        public void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            _media.AreTransportControlsEnabled = true;
            MediaStartAction();
        }

        public void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            _media.AreTransportControlsEnabled = false;
            MediaEndAction();
        }

        public Action MediaEndAction { get; set; }
        public Action MediaStartAction { get; set; }
        public Func<string> TextProvider { get; set; }

        public async Task Start(string text)
        {
            // The object for controlling the speech synthesis engine (voice).
            using (var synthesis = new Windows.Media.SpeechSynthesis.SpeechSynthesizer())
            {
                // Generate the audio stream from plain text.
                var stream = await synthesis.SynthesizeTextToStreamAsync(text);
                // Send the stream to the media object.
                _media.SetSource(stream, stream.ContentType);
                _media.Play();
            }
        }

        public async Task Toggle()
        {
            if (_media.CurrentState == MediaElementState.Playing) _media.Stop();
            else
            {
                var text = TextProvider();
                if (!string.IsNullOrEmpty(text)) await Start(text);
                else await UiUtils.ShowDialogAsync("No Content to Read");
            }
        }
    }
}
