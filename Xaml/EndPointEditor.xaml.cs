namespace Ecng.Xaml
{
	using System;
	using System.Globalization;
	using System.Net;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;

	using MoreLinq;

	/// <summary>
	/// Editor for <see cref="EndPointEditor.EndPoint"/>.
	/// </summary>
	public partial class EndPointEditor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EndPointEditor"/>.
		/// </summary>
		public EndPointEditor()
		{
			InitializeComponent();
			//Address.Mask = @"[а-яА-Яa-zA-Z0-9\.\-]+:?\d+";
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="EndPointEditor.EndPoint"/>.
		/// </summary>
		public static readonly DependencyProperty EndPointProperty =
			DependencyProperty.Register("EndPoint", typeof(EndPoint), typeof(EndPointEditor), new PropertyMetadata(default(EndPoint)));

		/// <summary>
		/// Address.
		/// </summary>
		public EndPoint EndPoint
		{
			get { return (EndPoint)GetValue(EndPointProperty); }
			set { SetValue(EndPointProperty, value); }
		}
	}

	class EndPointValidationRule : ValidationRule
	{
		public bool Multi { get; set; }

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (value == null)
				return new ValidationResult(false, "Incorrect address.");

			try
			{
				if (Multi)
					value.To<string>().Split(",").ForEach(v => v.To<EndPoint>());
				else
					value.To<EndPoint>();

				return ValidationResult.ValidResult;
			}
			catch (Exception)
			{
				return new ValidationResult(false, "Incorrect address.");
			}
		}
	}
}