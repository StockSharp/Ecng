namespace Ecng.Xaml.Grids
{
	using System;
	using System.Windows;

	using Ecng.Interop.Dde;
	using Ecng.Localization;

	public partial class DdeSettingsWindow
	{
		public DdeSettingsWindow()
		{
			InitializeComponent();

			Title = Title.Translate();

			StartStop.Content = ((string)StartStop.Content).Translate();
			Flush.Content = ((string)Flush.Content).Translate();
		}

		private XlsDdeClient _ddeClient;

		public XlsDdeClient DdeClient
		{
			get { return _ddeClient; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_ddeClient = value;
				DdeSettingsGrid.SelectedObject = value.Settings.Clone();

				RefreshButtons();
			}
		}

		public Action StartedAction;
		public Action StoppedAction;
		public Action FlushAction;

		private void RefreshButtons()
		{
			StartStop.Content = DdeClient.IsStarted ? "Остановить" : "Запустить";
			Flush.IsEnabled = !DdeClient.IsStarted;
		}

		private void StartStop_OnClick(object sender, RoutedEventArgs e)
		{
			ApplySettings();

			if (DdeClient.IsStarted)
			{
				StoppedAction();
				DdeClient.Stop();
			}
			else
			{
				DdeClient.Start();
				StartedAction();
			}

			RefreshButtons();
		}

		private void Export_OnClick(object sender, RoutedEventArgs e)
		{
			ApplySettings();
			FlushAction();
		}

		private void Ok_OnClick(object sender, RoutedEventArgs e)
		{
			ApplySettings();
			DialogResult = true;
		}

		private void ApplySettings()
		{
			DdeClient.Settings.Apply((DdeSettings)DdeSettingsGrid.SelectedObject);
		}
	}
}