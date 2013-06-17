namespace Ecng.Xaml
{
	using System;
	using System.Net;
	using System.Windows;
	using System.Windows.Controls;
	using System.Linq;

	/// <summary>
	/// Контрол для редактирования <see cref="EndPoint"/>
	/// </summary>
	public partial class EndPointEditControl
	{
		private bool _isNeedUpdate;

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="EndPoint"/>.
		/// </summary>
		public static readonly DependencyProperty EndPointProperty =
			DependencyProperty.Register("EndPoint", typeof(EndPoint), typeof(EndPointEditControl), new PropertyMetadata(default(EndPoint), OnEndPointChange));

		/// <summary>
		/// <see cref="EndPoint"/>
		/// </summary>
		public EndPoint EndPoint
		{
			get { return (EndPoint)GetValue(EndPointProperty); }
			set { SetValue(EndPointProperty, value); }
		}

		/// <summary>
		/// Создать <see cref="EndPointEditControl"/>
		/// </summary>
		public EndPointEditControl()
		{
			InitializeComponent();
		}

		private static void OnEndPointChange(DependencyObject obj, DependencyPropertyChangedEventArgs arg)
		{
			var editor = (EndPointEditControl) obj;

			var value = (IPEndPoint) arg.NewValue ?? new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);

			var address = value.Address.ToString().Split('.');

			editor.firstOctet.Text = address[0];
			editor.secondOctet.Text = address[1];
			editor.thirdOctet.Text = address[2];
			editor.fourthOctet.Text = address[3];
			editor.port.Text = value.Port.ToString();

			editor._isNeedUpdate = true;
		}

		private void OctetChange(object sender, TextChangedEventArgs e)
		{
			TextboxTextCheck(sender, 255);

			if (_isNeedUpdate)
				EndPointChange();
		}

		private void PortChange(object sender, TextChangedEventArgs e)
		{
			TextboxTextCheck(sender, 65535);

			if (_isNeedUpdate)
				EndPointChange();
		}

		private void EndPointChange()
		{
			var address = string.Format("{0}.{1}.{2}.{3}", firstOctet.Text, secondOctet.Text, thirdOctet.Text, fourthOctet.Text);
			EndPoint = new IPEndPoint(IPAddress.Parse(address), Convert.ToInt32(port.Text));
		}

		private void TextboxTextCheck(object sender, int maxValue)
		{
			var txtbox = (TextBox)sender;

			var text = new string(txtbox.Text.ToCharArray().Where(char.IsNumber).ToArray());

			if (text.Length > 1)
				text = text.Trim(new [] {'0'});

			if (string.IsNullOrWhiteSpace(text))
			{
				txtbox.Text = "0";
			}
			else
			{
				if (Convert.ToInt32(text) > maxValue)
				{
					txtbox.Text = maxValue.ToString();
				}
				else if (Convert.ToInt32(text) < 0)
				{
					txtbox.Text = "0";
				}
				else
				{
					txtbox.Text = text;
				}
			}

			txtbox.CaretIndex = txtbox.Text.Length;
		}
	}
}
