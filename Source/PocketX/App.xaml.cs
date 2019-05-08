using System;
using PocketX.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using PocketX.Handlers;

namespace PocketX
{
	sealed partial class App : Application
	{
        internal static string Protocol = "pocketx://auth";
        internal static readonly bool DEBUGMODE = System.Diagnostics.Debugger.IsAttached;

        public App()
		{
			InitializeComponent();
			Suspending += OnSuspending;
			Akavache.BlobCache.ApplicationName = typeof(App).Namespace;
			SettingsHandler.Load();
		}

		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			Frame rootFrame = Window.Current.Content as Frame;
			if (rootFrame == null)
			{
				rootFrame = new Frame();
				rootFrame.NavigationFailed += OnNavigationFailed;
				Window.Current.Content = rootFrame;
			}

			if (e == null || e?.PrelaunchActivated == false)
			{
				if (rootFrame.Content == null)
				{
					if (new Handlers.PocketHandler().LoadCacheClient() == null)
						rootFrame.Navigate(typeof(LoginPage), e?.Arguments);
					else
					{
						var pocketHandler = new Handlers.PocketHandler();
						pocketHandler.Client = pocketHandler.LoadCacheClient();
						rootFrame.Navigate(typeof(MainPage), e?.Arguments);
					}
				}
				Window.Current.Activate();
			}
		}

		void OnNavigationFailed(object sender, NavigationFailedEventArgs e) =>
			throw new Exception("Failed to load Page " + e.SourcePageType.FullName);

		private void OnSuspending(object sender, SuspendingEventArgs e) =>
			e.SuspendingOperation.GetDeferral().Complete();

		//OnShare
		protected async override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
		{
			var shareOperation = args.ShareOperation;
			await System.Threading.Tasks.Task.Factory.StartNew(async () =>
			{
				if (shareOperation.Data.Contains(StandardDataFormats.WebLink))
				{
					var url = await shareOperation?.Data.GetWebLinkAsync();
					await addToPocketAsync(url.AbsoluteUri, false);
					shareOperation.ReportCompleted();
				}
			});
		}

		// Protocol & CMD
		protected async override void OnActivated(IActivatedEventArgs args)
		{
			if (args.Kind == ActivationKind.Protocol)
			{
				ProtocolActivatedEventArgs protocolArgs = (ProtocolActivatedEventArgs)args;
				string arg = protocolArgs.Uri.ToString().Replace("pocketx://", "", StringComparison.InvariantCultureIgnoreCase);
				await addToPocketAsync(arg);
			}
			//From CommandLine
			else if (args.Kind == ActivationKind.CommandLineLaunch)
			{
				var arg = (args as CommandLineActivatedEventArgs)?.Operation?.Arguments ?? "";
				if (arg.Length > 3 && Uri.IsWellFormedUriString(arg, UriKind.Absolute))
					await addToPocketAsync(arg);
				else OnLaunched(null);
			}
		}

		private async System.Threading.Tasks.Task addToPocketAsync(string arg, bool exit = true)
		{
			try
			{
				var messages = await Handlers.PocketHandler.AddFromShare(new Uri(arg));
				Handlers.Utils.ToastIt(messages.Item1, messages.Item2);
				if (exit) Current.Exit();
			}
			catch { }
		}
	}
}
