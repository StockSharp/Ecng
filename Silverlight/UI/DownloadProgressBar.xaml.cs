namespace Ecng.UI
{
	using System;

	using Ecng.Common;

	public partial class DownloadProgressBar
	{
		public DownloadProgressBar()
		{
			InitializeComponent();
		}

		private string _text;

		public string Text
		{
			get { return _text; }
			set
			{
				_text = value;
				RefreshText();
			}
		}

		private double _value;

		public double Value
		{
			get { return _value; }
			set
			{
				_value = value;
				this.ValueCtrl.Value = value;
				RefreshText();
			}
		}

		private void RefreshText()
		{
			this.TextCtrl.Text = "({0}%) {1}{2}".Put(this.ValueCtrl.Value, this.Text, _dots);
		}

		private string _dots = ".";

		private void LoadingAnimation_Completed(object sender, EventArgs e)
		{
			if (this.Value < 100)
			{
				if (_dots.Length < 3)
					_dots += '.';
				else
					_dots = ".";

				this.ValueCtrl.Value++;
				RefreshText();

				this.LoadingAnimation.Begin();
			}
		}

		public void StartAnimation()
		{
			this.LoadingAnimation.Begin();
		}
	}
}