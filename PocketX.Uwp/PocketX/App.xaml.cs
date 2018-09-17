using System;
using PocketX.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Windows.ApplicationModel.DataTransfer;

namespace PocketX
{
    sealed partial class App : Application
    {
        public static string Protocol = "pocketx://auth";
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            AppCenter.Start(Handlers.Keys.AppCenter, typeof(Analytics));
            Akavache.BlobCache.ApplicationName = typeof(App).Namespace;
            Models.SettingsHandler.Load();
        }

        protected async override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            var shareOperation = args.ShareOperation;
            await System.Threading.Tasks.Task.Factory.StartNew(async () =>
            {
                if (shareOperation.Data.Contains(StandardDataFormats.WebLink))
                {
                    var url = await shareOperation.Data.GetWebLinkAsync();
                    var messages = await Handlers.PocketHandler.AddFromShare(url);
                    Handlers.Utils.ToastIt(messages.Item1, messages.Item2);
                    shareOperation.ReportCompleted();
                }
            });
        }
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    if (new Handlers.PocketHandler().LoadCacheClient() == null)
                        rootFrame.Navigate(typeof(LoginPage), e.Arguments);
                    else
                    {
                        var pocketHandler = new Handlers.PocketHandler();
                        pocketHandler.Client = pocketHandler.LoadCacheClient();
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);

                    }
                }
                Window.Current.Activate();
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e) => 
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);

        private void OnSuspending(object sender, SuspendingEventArgs e) => 
            e.SuspendingOperation.GetDeferral().Complete();


        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs protocolArgs = (ProtocolActivatedEventArgs)args;
                string uri = protocolArgs.Uri.ToString();

            }
        }
    }
}
