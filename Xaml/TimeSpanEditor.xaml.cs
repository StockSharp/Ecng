namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Data;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Localization;

	/// <summary>
	/// <see cref="TimeSpan"/> editor.
	/// </summary>
	public partial class TimeSpanEditor
	{
		private TextBox _focusedTextBox;
		private bool _suspendChanged;

		private readonly List<Key> _validKey = new List<Key>
		{
			Key.D0,
			Key.D1,
			Key.D2,
			Key.D3,
			Key.D4,
			Key.D5,
			Key.D6,
			Key.D7,
			Key.D8,
			Key.D9,
			Key.Up,
			Key.Down,
			Key.Left,
			Key.Right,
			Key.Delete,
			Key.Back
		};

		/// <summary>
		/// Default editor parts mask.
		/// </summary>
		public const TimeSpanEditorMask DefaultMask = TimeSpanEditorMask.Hours | TimeSpanEditorMask.Minutes | TimeSpanEditorMask.Seconds;

		#region Dependency properties

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Value"/>.
		/// </summary>
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(TimeSpan?), typeof(TimeSpanEditor),
			new UIPropertyMetadata(TimeSpan.FromMinutes(1), OnValueChanged));

		private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var control = (TimeSpanEditor)sender;
			var value = (TimeSpan?)e.NewValue;

			//control.HasValueCheckBox.Visibility = control.IsNullable ? Visibility.Visible : Visibility.Collapsed;

			control._suspendChanged = true;

			try
			{
				if (value != null)
				{
					control.HasValue = true;
					control.Seconds = value.Value.Seconds;
					control.Minutes = value.Value.Minutes;
					control.Hours = value.Value.Hours;
					control.Days = value.Value.Days;
					control.Milliseconds = value.Value.Milliseconds;
					control.Microseconds = value.Value.GetMicroseconds();
				}
				else
				{
					control.HasValue = false;
					control.Seconds = 0;
					control.Minutes = 0;
					control.Hours = 0;
					control.Days = 0;
					control.Milliseconds = 0;
					control.Microseconds = 0;
				}
			}
			finally
			{
				control._suspendChanged = false;
			}

			control.ValueChanged?.Invoke(value);
        }

		/// <summary>
		/// The <see cref="TimeSpan"/> value.
		/// </summary>
		public TimeSpan? Value
		{
			get => (TimeSpan?)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="HasValue"/>.
		/// </summary>
		public static readonly DependencyProperty IsNullableProperty = DependencyProperty.Register(nameof(IsNullable), typeof(bool), typeof(TimeSpanEditor),
			new UIPropertyMetadata(false));

		/// <summary>
		/// Is nullable.
		/// </summary>
		public bool IsNullable
		{
			get => (bool)GetValue(IsNullableProperty);
			set => SetValue(IsNullableProperty, value);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="HasValue"/>.
		/// </summary>
		public static readonly DependencyProperty HasValueProperty = DependencyProperty.Register(nameof(HasValue), typeof(bool), typeof(TimeSpanEditor),
			new UIPropertyMetadata(true, OnTimeChanged));

		/// <summary>
		/// Has value.
		/// </summary>
		public bool HasValue
		{
			get => (bool)GetValue(HasValueProperty);
			set => SetValue(HasValueProperty, value);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Microseconds"/>.
		/// </summary>
		public static readonly DependencyProperty MicrosecondsProperty = DependencyProperty.Register(nameof(Microseconds), typeof(int), typeof(TimeSpanEditor),
			new UIPropertyMetadata(0, OnTimeChanged));

		/// <summary>
		/// Microseconds.
		/// </summary>
		public int Microseconds
		{
			get => (int)GetValue(MicrosecondsProperty);
			set
			{
				if (value > 999)
					value = 0;

				if (value < 0)
					value = 999;

				SetValue(MicrosecondsProperty, value);
			}
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Milliseconds"/>.
		/// </summary>
		public static readonly DependencyProperty MillisecondsProperty = DependencyProperty.Register(nameof(Milliseconds), typeof(int), typeof(TimeSpanEditor),
			new UIPropertyMetadata(0, OnTimeChanged));

		/// <summary>
		/// Milliseconds.
		/// </summary>
		public int Milliseconds
		{
			get => (int)GetValue(MillisecondsProperty);
			set
			{
				if (value > 999)
					value = 0;

				if (value < 0)
					value = 999;

				SetValue(MillisecondsProperty, value);
			}
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Seconds"/>.
		/// </summary>
		public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register(nameof(Seconds), typeof(int), typeof(TimeSpanEditor),
			new UIPropertyMetadata(0, OnTimeChanged));

		/// <summary>
		/// Seconds.
		/// </summary>
		public int Seconds
		{
			get => (int)GetValue(SecondsProperty);
			set
			{
				if (value > 59)
					value = 0;

				if (value < 0)
					value = 59;

				SetValue(SecondsProperty, value);
			}
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Minutes"/>.
		/// </summary>
		public static readonly DependencyProperty MinutesProperty = DependencyProperty.Register(nameof(Minutes), typeof(int), typeof(TimeSpanEditor),
			new UIPropertyMetadata(1, OnTimeChanged));

		/// <summary>
		/// Minutes.
		/// </summary>
		public int Minutes
		{
			get => (int)GetValue(MinutesProperty);
			set
			{
				if (value > 59)
					value = 0;

				if (value < 0)
					value = 59;

				SetValue(MinutesProperty, value);
			}
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Hours"/>.
		/// </summary>
		public static readonly DependencyProperty HoursProperty = DependencyProperty.Register(nameof(Hours), typeof(int), typeof(TimeSpanEditor),
			new UIPropertyMetadata(0, OnTimeChanged));

		/// <summary>
		/// Hours.
		/// </summary>
		public int Hours
		{
			get => (int)GetValue(HoursProperty);
			set
			{
				if (value > 23)
					value = 0;

				if (value < 0)
					value = 23;

				SetValue(HoursProperty, value);
			}
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Days"/>.
		/// </summary>
		public static readonly DependencyProperty DaysProperty = DependencyProperty.Register(nameof(Days), typeof(int), typeof(TimeSpanEditor),
			new UIPropertyMetadata(0, OnTimeChanged));

		/// <summary>
		/// The days value.
		/// </summary>
		public int Days
		{
			get => (int)GetValue(DaysProperty);
			set
			{
				if (value > 364)
					value = 0;

				if (value < 0)
					value = 364;

				SetValue(DaysProperty, value);
			}
		}

		private static void OnTimeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var control = (TimeSpanEditor)sender;

			if (control._suspendChanged)
				return;

			control.Value = control.HasValue 
				? new TimeSpan(control.Days, control.Hours, control.Minutes, control.Seconds, control.Milliseconds).AddMicroseconds(control.Microseconds) 
				: (TimeSpan?)null;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Mask"/>.
		/// </summary>
		public static readonly DependencyProperty MaskProperty = DependencyProperty.Register(nameof(Mask), typeof(TimeSpanEditorMask), typeof(TimeSpanEditor),
			new UIPropertyMetadata(DefaultMask));

		/// <summary>
		/// Show parts mask.
		/// </summary>
		public TimeSpanEditorMask Mask
		{
			get => (TimeSpanEditorMask)GetValue(MaskProperty);
			set => SetValue(MaskProperty, value);
		}

		#endregion

		/// <summary>
		/// The <see cref="Value"/> changed event.
		/// </summary>
		public event Action<TimeSpan?> ValueChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="TimeSpanEditor"/>.
		/// </summary>
		public TimeSpanEditor()
		{
			InitializeComponent();

			LocalizeToolTip(TxbDays);
			LocalizeToolTip(TxbHours);
			LocalizeToolTip(TxbMinutes);
			LocalizeToolTip(TxbSeconds);
			LocalizeToolTip(TxbMilliseconds);
			LocalizeToolTip(TxbMicroseconds);

			_focusedTextBox = TxbMinutes;
		}

		private static void LocalizeToolTip(TextBox tb)
		{
			var str = tb.ToolTip as string;

			if (str == null)
				return;

			tb.ToolTip = str.Translate();
		}

		private void ButtonUpDownClick(object sender, RoutedEventArgs e)
		{
			if (_focusedTextBox == null)
				return;

			var button = (RepeatButton)sender;

			switch (_focusedTextBox.Name)
			{
				case nameof(TxbMicroseconds):
					if (button.Name == nameof(BtnUp))
						Microseconds++;
					else
						Microseconds--;
					break;

				case nameof(TxbMilliseconds):
					if (button.Name == nameof(BtnUp))
						Milliseconds++;
					else
						Milliseconds--;
					break;

				case nameof(TxbSeconds):
					if (button.Name == nameof(BtnUp))
						Seconds++;
					else
						Seconds--;
					break;

				case nameof(TxbMinutes):
					if (button.Name == nameof(BtnUp))
						Minutes++;
					else
						Minutes--;
					break;

				case nameof(TxbHours):
					if (button.Name == nameof(BtnUp))
						Hours++;
					else
						Hours--;
					break;

				case nameof(TxbDays):
					if (button.Name == nameof(BtnUp))
						Days++;
					else
						Days--;
					break;
			}
		}

		private void KeyPressed(object sender, KeyEventArgs e)
		{
			if (!_validKey.Contains(e.Key))
			{
				e.Handled = true;
				return;
			}

			switch (((TextBox)sender).Name)
			{
				case nameof(TxbMicroseconds):
					if (e.Key == Key.Up) Microseconds++;
					if (e.Key == Key.Down) Microseconds--;
					break;

				case nameof(TxbMilliseconds):
					if (e.Key == Key.Up) Milliseconds++;
					if (e.Key == Key.Down) Milliseconds--;
					break;

				case nameof(TxbSeconds):
					if (e.Key == Key.Up) Seconds++;
					if (e.Key == Key.Down) Seconds--;
					break;

				case nameof(TxbMinutes):
					if (e.Key == Key.Up) Minutes++;
					if (e.Key == Key.Down) Minutes--;
					break;

				case nameof(TxbHours):
					if (e.Key == Key.Up) Hours++;
					if (e.Key == Key.Down) Hours--;
					break;

				case nameof(TxbDays):
					if (e.Key == Key.Up) Days++;
					if (e.Key == Key.Down) Days--;
					break;
			}
		}

		private void TextChanged(object sender, TextChangedEventArgs e)
		{
			var control = (TextBox)sender;
			int num;

			switch (control.Name)
			{
				case nameof(TxbMicroseconds):
					num = ValidateNumber(Microseconds, 999, control.Text);
					Microseconds = num;
					control.Text = num.ToString(CultureInfo.InvariantCulture);
					break;

				case nameof(TxbMilliseconds):
					num = ValidateNumber(Milliseconds, 999, control.Text);
					Milliseconds = num;
					control.Text = num.ToString(CultureInfo.InvariantCulture);
					break;

				case nameof(TxbSeconds):
					num = ValidateNumber(Seconds, 59, control.Text);
					Seconds = num;
					control.Text = num.ToString(CultureInfo.InvariantCulture);
					break;

				case nameof(TxbMinutes):
					num = ValidateNumber(Minutes, 59, control.Text);
					Minutes = num;
					control.Text = num.ToString(CultureInfo.InvariantCulture);
					break;

				case nameof(TxbHours):
					num = ValidateNumber(Hours, 23, control.Text);
					Hours = num;
					control.Text = num.ToString(CultureInfo.InvariantCulture);
					break;

				case nameof(TxbDays):
					num = ValidateNumber(Days, 364, control.Text);
					Days = num;
					control.Text = num.ToString(CultureInfo.InvariantCulture);
					break;
			}
		}

		private static int ValidateNumber(int lastValue, int max, string text)
		{
			if (text.Length > max.ToString().Length)
				return lastValue;

			if (!int.TryParse(text, out var num))
				return 0;

			if (num > max) num = 0;
			if (num < 0) num = max;

			return num;
		}

		private void FocusedTextBox(object sender, RoutedEventArgs e)
		{
			_focusedTextBox = (TextBox)sender;
		}

		private void OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			switch (((TextBox)sender).Name)
			{
				case nameof(TxbMicroseconds):
				{
					if (e.Delta > 0)
						Microseconds++;
					else
						Microseconds--;

					break;
				}

				case nameof(TxbMilliseconds):
				{
					if (e.Delta > 0)
						Milliseconds++;
					else
						Milliseconds--;

					break;
				}

				case nameof(TxbSeconds):
				{
					if (e.Delta > 0)
						Seconds++;
					else
						Seconds--;

					break;
				}

				case nameof(TxbMinutes):
				{
					if (e.Delta > 0)
						Minutes++;
					else
						Minutes--;

					break;
				}

				case nameof(TxbHours):
				{
					if (e.Delta > 0)
						Hours++;
					else
						Hours--;

					break;
				}

				case nameof(TxbDays):
				{
					if (e.Delta > 0)
						Days++;
					else
						Days--;

					break;
				}
			}
		}
	}

	class TimeSpanEditorMaskToVisibilityConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null || parameter == null)
				return Visibility.Visible;

			var mask = (TimeSpanEditorMask)value;
			var p = (TimeSpanEditorMask)parameter;

			return (mask & p) == p ? Visibility.Visible : Visibility.Collapsed;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}