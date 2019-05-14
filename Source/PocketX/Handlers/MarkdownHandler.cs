using System;
using Windows.System;
using Microsoft.Toolkit.Uwp.UI.Controls;
using PocketX.Views.Dialog;

namespace PocketX.Handlers
{
    public class MarkdownHandler
    {
        private readonly MarkdownTextBlock _markdownText;

        public MarkdownHandler(MarkdownTextBlock markdownText)
        {
            _markdownText = markdownText;
            _markdownText.ImageClicked += ImageClicked;
            _markdownText.LinkClicked += LinkClicked;
        }

        public string Text => _markdownText.Text;

        public async void LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out var link))
                await Launcher.LaunchUriAsync(link);
        }

        public async void ImageClicked(object sender, LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out var link))
                await new ImageDialog(link).ShowAsync();
        }
    }
}
