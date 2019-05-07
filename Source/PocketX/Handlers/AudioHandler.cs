using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PocketX.Handlers
{
    class AudioHandler
    {
        private static MediaElement media;
        public MediaElement Media
        {
            get { return media; }
            set { if (media == null) media = value; }
        }

        public async Task Start(string text)
        {
            // The object for controlling the speech synthesis engine (voice).
            using (var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer())
            {
                // Generate the audio stream from plain text.
                Windows.Media.SpeechSynthesis.SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(text);
                // Send the stream to the media object.
                Media.SetSource(stream, stream.ContentType);
                Media.Play();
            }
        }
    }
}
