namespace Ecng.Xaml
{
	using System.Net;
	using System.Windows;
	using System.Windows.Controls;
	using System.Linq;

	using Common;

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
		/// <see cref="DependencyProperty"/> для <see cref="DnsOrIp"/>.
		/// </summary>
		public static readonly DependencyProperty DnsOrIpProperty =
			DependencyProperty.Register("DnsOrIp", typeof(bool), typeof(EndPointEditControl), new PropertyMetadata(default(bool)));

		/// <summary>
		/// Поле ввода в виде dns или маски IP.
		/// </summary>
		public bool DnsOrIp
		{
			get { return (bool)GetValue(DnsOrIpProperty); }
			set { SetValue(DnsOrIpProperty, value); }
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
			var editor = (EndPointEditControl)obj;

			if (arg.NewValue is IPEndPoint)
			{
				editor.DnsOrIp = false;

				var value = (IPEndPoint)arg.NewValue;

				var address = value.Address.ToString().Split('.');

				editor.firstOctet.Text = address[0];
				editor.secondOctet.Text = address[1];
				editor.thirdOctet.Text = address[2];
				editor.fourthOctet.Text = address[3];
				editor.port.Text = value.Port.To<string>();
			}
			else
			{
				editor.DnsOrIp = true;

				var value = (DnsEndPoint)arg.NewValue ?? new DnsEndPoint("127.0.0.1", 0);

				editor.dns.Text = value.Host;
				editor.port.Text = value.Port.To<string>();
			}

			editor._isNeedUpdate = true;
		}

		private void OctetChange(object sender, TextChangedEventArgs e)
		{
			TextboxTextCheck(sender, 255);

			if (_isNeedUpdate)
				EndPointChange();
		}

		private void DnsChange(object sender, TextChangedEventArgs e)
		{
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
			if (DnsOrIp)
			{
				EndPoint = new DnsEndPoint(dns.Text, port.Text.To<int>());
			}
			else
			{
				var address = "{0}.{1}.{2}.{3}".Put(firstOctet.Text, secondOctet.Text, thirdOctet.Text, fourthOctet.Text);
				EndPoint = new IPEndPoint(IPAddress.Parse(address), port.Text.To<int>());
			}
		}

		private void TextboxTextCheck(object sender, int maxValue)
		{
			var txtbox = (TextBox)sender;

			var text = new string(txtbox.Text.ToCharArray().Where(char.IsNumber).ToArray());

			if (text.Length > 1)
				text = text.TrimStart(new[] { '0' });

			if (string.IsNullOrWhiteSpace(text))
			{
				txtbox.Text = "0";
			}
			else
			{
				if (text.To<int>() > maxValue)
				{
					txtbox.Text = maxValue.To<string>();
				}
				else if (text.To<int>() < 0)
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
