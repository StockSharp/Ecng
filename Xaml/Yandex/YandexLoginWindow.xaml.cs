namespace Ecng.Xaml.Yandex
{
	using System;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Navigation;
	using Disk.SDK;
	using Disk.SDK.Provider;
	using Ecng.ComponentModel;
	using Ecng.Localization;
	using Ecng.Common;

	partial class YandexLoginWindow
	{
		private class LoadingContext : NotifiableObject
		{
			private string _title;

			public string Title
			{
				get => _title;
				set
				{
					_title = value;
					NotifyChanged(nameof(Title));
				}
			}
		}

		private const string _clientId = "fa16e5e894684f479fd32f7578f0d4a4";
		private const string _returnUrl = "https://oauth.yandex.ru/verification_code";

		private bool _authCompleted;

		public event EventHandler<GenericSdkEventArgs<string>> AuthCompleted;
		
		private readonly LoadingContext _loadingContext = new LoadingContext();

		public YandexLoginWindow()
		{
			InitializeComponent();

			Title = Title.Translate();

			Browser.Visibility = Visibility.Hidden;

			BusyIndicator.SplashScreenDataContext = _loadingContext;
			_loadingContext.Title = "Authorization...".Translate();
			BusyIndicator.IsSplashScreenShown = true;

			Browser.Navigated += BrowserNavigated;
		}

		private void BrowserNavigated(object sender, NavigationEventArgs e)
		{
			if (_authCompleted)
				return;

			Browser.Visibility = Visibility.Visible;
			BusyIndicator.IsSplashScreenShown = false;
		}

		private void YandexLoginWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			new DiskSdkClient(string.Empty).AuthorizeAsync(new WebBrowserWrapper(Browser), _clientId, _returnUrl, CompleteCallback);
		}

		private void CompleteCallback(object sender, GenericSdkEventArgs<string> e)
		{
			_authCompleted = true;

			Browser.Visibility = Visibility.Hidden;

			_loadingContext.Title = "File loading...".Translate();
			BusyIndicator.IsSplashScreenShown = true;

			Task.Factory
				.StartNew(() => AuthCompleted?.Invoke(this, new GenericSdkEventArgs<string>(e.Result)))
				.ContinueWith(res =>
				{
					BusyIndicator.IsSplashScreenShown = false;
					DialogResult = true;
				}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static Func<Tuple<string, bool>> Authorize(Window owner)
		{
			Tuple<string, bool> Do()
			{
				var isCanceled = false;

				string token = null;
				Exception error = null;

				YandexLoginWindow CreateWindow()
				{
					var loginWindow = new YandexLoginWindow();

					loginWindow.AuthCompleted += (s, e) =>
					{
						if (e.Error == null)
							token = e.Result;
						else
							error = e.Error;
					};

					return loginWindow;
				}

				var retVal = owner?.GuiSync(() => CreateWindow().ShowModal(owner)) ?? CreateWindow().ShowDialog() == true;

				if (!retVal)
					isCanceled = true;

				error?.Throw();

				return Tuple.Create(token, isCanceled);
			}

			return Do;
		}
	}
}
