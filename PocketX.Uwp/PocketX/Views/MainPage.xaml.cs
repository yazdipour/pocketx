using System;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views.Dialog;

using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace PocketX.Views
{
	public sealed partial class MainPage : Page
	{
		private string[] _tags = new[] { "MyList", "Favorites", "Archives" };
		private string[] _tags2 = new[] { "Settings", "Reading", "Tags", "Refresh" };
		public static Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification Notifier { get; set; }
		private Settings settings = SettingsHandler.Settings;

		public MainPage()
		{
			this.InitializeComponent();
			Loaded += (s, e) =>
			{
				var uIHandler = new UIHandler();
				uIHandler.TitleBarVisiblity(false, Titlebar);
				uIHandler.TitleBarButton_TranparentBackground(settings.app_theme == Windows.UI.Xaml.ElementTheme.Dark);
				insideFrame.Navigate(typeof(MainContent));
			};
			Notifier = _notifer;
		}

		private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			string tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
			if (tag == null) return;
			var mainContent = (insideFrame.Content as MainContent);
			if (mainContent != null) await mainContent.ParentCommandAsync(tag);
		}

		private async void PaneFooter_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			var tag = (sender as NavigationViewItem).Tag?.ToString();
			var index = Array.IndexOf(_tags2, tag);
			if (index < 0) return;
			if (tag.Equals(_tags2[2]))
			{
				var dialog = new TagsDialog();
				await dialog.ShowAsync();
				navView.SelectedItem = -1;
				NavigationViewExtensions.SetSelectedIndex(navView, -1);
				if (dialog.Tag != null) await (insideFrame.Content as MainContent).ParentCommandAsync(dialog.Tag.ToString());
			}
			else if (tag.Equals(_tags2[3]))
				await (insideFrame.Content as MainContent).ParentCommandAsync((navView.SelectedItem as Control)?.Tag.ToString(), 0);
			else
			{
				var dialog = new SettingsDialog(index);
				await dialog.ShowAsync();
				if (dialog.Tag?.ToString() == Keys.Logout)
				{
					new PocketHandler().Logout(Frame);
					return;
				}
				(insideFrame.Content as MainContent).BindingsUpdate();
				Bindings.Update();
				SettingsHandler.Save();
			}
		}

		private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			var tag = args?.QueryText;
			NavigationViewExtensions.SetSelectedIndex(navView, -1);
			await (insideFrame.Content as MainContent).ParentCommandAsync(tag);
		}

		private async void Pin_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (!ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
				await new Windows.UI.Popups.MessageDialog("You System does not support Compact Mode").ShowAsync();
			else
			{
				if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
				{
					ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
					compactOptions.CustomSize = new Windows.Foundation.Size(520, 400);
					await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
				}
				else
					await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
			}
		}
	}
}