#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: YandexLoginWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace Ecng.Backup.Yandex
{
	using System;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Navigation;

	using Disk.SDK;
	using Disk.SDK.Provider;

	using Ecng.ComponentModel;
	using Ecng.Localization;

	partial class YandexLoginWindow
	{
		private class LoadingContext : NotifiableObject
		{
			private string _title;

			public string Title
			{
				get { return _title; }
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
	}
}
