using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace PocketX.Handlers
{
    internal class AudioHandler
    {
        public static async Task Start(MediaElement media, string text)
        {
            // The object for controlling the speech synthesis engine (voice).
            using (var synthesis = new Windows.Media.SpeechSynthesis.SpeechSynthesizer())
            {
                // Generate the audio stream from plain text.
                var stream = await synthesis.SynthesizeTextToStreamAsync(text);
                // Send the stream to the media object.
                media.SetSource(stream, stream.ContentType);
                media.Play();
            }
        }
    }
}
