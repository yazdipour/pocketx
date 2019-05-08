using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace PocketX.Handlers
{
    internal class AudioHandler
    {
        private static MediaElement _media;
        public MediaElement Media
        {
            get => _media;
            set { if (_media == null) _media = value; }
        }

        public async Task Start(string text)
        {
            // The object for controlling the speech synthesis engine (voice).
            using (var synthesis = new Windows.Media.SpeechSynthesis.SpeechSynthesizer())
            {
                // Generate the audio stream from plain text.
                var stream = await synthesis.SynthesizeTextToStreamAsync(text);
                // Send the stream to the media object.
                Media.SetSource(stream, stream.ContentType);
                Media.Play();
            }
        }
    }
}
