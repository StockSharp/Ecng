namespace Ecng.Xaml.Grids
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Xaml.Fonts;

	public enum FormatRuleTypes
	{
		CellValue,
		PropertyValue,
	}

	public class FormatRule : DependencyObject, IPersistable
	{
		public FormatRule()
			: this(true)
		{
		}

		private FormatRule(bool copyFromDefault)
		{
			if (copyFromDefault)
			{
				Background = Default.Background != null ? Default.Background.Clone() : new SolidColorBrush(Colors.White);
				Foreground = Default.Foreground != null ? Default.Foreground.Clone() : new SolidColorBrush(Colors.Black);
				Font = Default.Font.Clone();
			}

			Type = FormatRuleTypes.CellValue;
			IsApplyToRow = false;
		}

		private static readonly Lazy<FormatRule> _default = new Lazy<FormatRule>(() =>
		{
			var rule = new FormatRule(false);

			var tb = new TextBox();

			rule.Background = (SolidColorBrush)tb.Background;
			rule.Foreground = (SolidColorBrush)tb.Foreground;
			rule.Font = tb.GetFont();

			return rule;
		});

		public static FormatRule Default
		{
			get { return _default.Value; }
		}

		public static readonly DependencyProperty TypeProperty = DependencyProperty.Register("Type", typeof(FormatRuleTypes), typeof(FormatRule), new PropertyMetadata());

		public FormatRuleTypes Type
		{
			get { return (FormatRuleTypes)GetValue(TypeProperty); }
			set { SetValue(TypeProperty, value); }
		}

		public static readonly DependencyProperty PropertyNameProperty = DependencyProperty.Register("PropertyName", typeof(string), typeof(FormatRule), new PropertyMetadata());

		public string PropertyName
		{
			get { return (string)GetValue(PropertyNameProperty); }
			set { SetValue(PropertyNameProperty, value); }
		}

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(FormatRule), new PropertyMetadata());

		public object Value
		{
			get { return GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}

		public static readonly DependencyProperty ConditionProperty = DependencyProperty.Register("Condition", typeof(ComparisonOperator), typeof(FormatRule), new PropertyMetadata());

		public ComparisonOperator Condition
		{
			get { return (ComparisonOperator)GetValue(ConditionProperty); }
			set { SetValue(ConditionProperty, value); }
		}

		public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof(SolidColorBrush), typeof(FormatRule), new PropertyMetadata());

		public SolidColorBrush Background
		{
			get { return (SolidColorBrush)GetValue(BackgroundProperty); }
			set { SetValue(BackgroundProperty, value); }
		}

		public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(SolidColorBrush), typeof(FormatRule), new PropertyMetadata());

		public SolidColorBrush Foreground
		{
			get { return (SolidColorBrush)GetValue(ForegroundProperty); }
			set { SetValue(ForegroundProperty, value); }
		}

		public static readonly DependencyProperty FontProperty = DependencyProperty.Register("Font", typeof(FontInfo), typeof(FormatRule), new PropertyMetadata());

		public FontInfo Font
		{
			get { return (FontInfo)GetValue(FontProperty); }
			set { SetValue(FontProperty, value); }
		}

		public static readonly DependencyProperty IsApplyToRowProperty = DependencyProperty.Register("IsApplyToRow", typeof(bool), typeof(FormatRule), new PropertyMetadata());

		public bool IsApplyToRow
		{
			get { return (bool)GetValue(IsApplyToRowProperty); }
			set { SetValue(IsApplyToRowProperty, value); }
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Background = new SolidColorBrush(storage.GetValue("Background", Colors.Black));
			Condition = storage.GetValue<ComparisonOperator>("Condition");
			//Font = storage.GetValue<FontInfo>("Font");
			Foreground = new SolidColorBrush(storage.GetValue("Foreground", Colors.Black));
			IsApplyToRow = storage.GetValue<bool>("IsApplyToRow");
			PropertyName = storage.GetValue<string>("PropertyName");
			Type = storage.GetValue<FormatRuleTypes>("Type");
			Value = storage.GetValue<object>("Value");
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("Background", Background != null ? Background.Color : Colors.Black);
			storage.SetValue("Condition", Condition);
			//storage.SetValue("Font", Font);
			storage.SetValue("Foreground", Background != null ? Foreground.Color : Colors.Black);
			storage.SetValue("IsApplyToRow", IsApplyToRow);
			storage.SetValue("PropertyName", PropertyName);
			storage.SetValue("Type", Type);
			storage.SetValue("Value", Value);
		}
	}
}