using System;
using PocketX.Handlers;
using PocketX.Models;
using PocketX.Views.Dialog;

using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using PocketX.ViewModels;

namespace PocketX.Views
{
	public sealed partial class MainPage : Page
	{
		public static Microsoft.Toolkit.Uwp.UI.Controls.InAppNotification Notifier { get; set; }
        private readonly MainPageViewModel _vm = new MainPageViewModel();

        internal readonly string[] _tags = { "MyList", "Favorites", "Archives" };
        internal readonly string[] _tags2 = { "Settings", "Reading", "Tags", "Refresh" };
        internal readonly Settings _settings = SettingsHandler.Settings;
        public MainPage()
        {
            InitializeComponent();
            //DataContext = _vm;
			Notifier = _notifer;
			Loaded += (s, e) =>
			{
                Logger.Logger.InitOnlineLogger(Keys.AppCenter);
                Logger.Logger.SetDebugMode(App.DEBUGMODE);
				var uIHandler = new UiUtils();
                uIHandler.TitleBarVisibility(false, WindowBorder);
                uIHandler.TitleBarButtonTransparentBackground(_settings.AppTheme == Windows.UI.Xaml.ElementTheme.Dark);
				insideFrame.Navigate(typeof(MainContent));
			};
		}

		private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
			if (tag == null) return;
			if (insideFrame.Content is MainContent mainContent) await mainContent.ParentCommandAsync(tag);
		}

		private async void PaneFooter_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			var tag = ((NavigationViewItem) sender).Tag?.ToString();
			var index = Array.IndexOf(_tags2, tag);
            var m = (MainContent) insideFrame.Content;
            if (index < 0) return;
			if (_tags2[2].Equals(tag))
			{
				var dialog = new TagsDialog();
				await dialog.ShowAsync();
				NavView.SelectedItem = -1;
				//NavigationViewExtensions.SetSelectedIndex(navView, -1);
				if (dialog.Tag != null) await m.ParentCommandAsync(dialog.Tag.ToString());
			}
			else if (_tags2[3].Equals(tag))
				await m?.ParentCommandAsync((NavView.SelectedItem as Control)?.Tag?.ToString(), 0);
			else
			{
				var dialog = new SettingsDialog(index);
				await dialog.ShowAsync();
				if (dialog.Tag?.ToString() == Keys.Logout)
				{
					new PocketHandler().Logout(Frame);
					return;
				}
				m?.BindingsUpdate();
				Bindings.Update();
				SettingsHandler.Save();
			}
		}

		private async void Pin_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (!ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
				await new Windows.UI.Popups.MessageDialog("You System does not support Compact Mode").ShowAsync();
			else
			{
				if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
				{
					var compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
					compactOptions.CustomSize = new Windows.Foundation.Size(520, 400);
					await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
				}
				else
					await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
			}
		}
	}
}