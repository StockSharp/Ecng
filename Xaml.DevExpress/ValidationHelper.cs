namespace Ecng.Xaml.DevExp
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.Editors.Settings;

	public class ValidationRulesCollection : List<ValidationRule>
	{
	}

	public static class ValidationHelper
	{
		public static ValidationRulesCollection GetValidationRules(DependencyObject obj)
		{
			return (ValidationRulesCollection)obj.GetValue(ValidationRulesProperty);
		}

		public static void SetValidationRules(DependencyObject obj, ValidationRulesCollection value)
		{
			obj.SetValue(ValidationRulesProperty, value);
		}

		public static readonly DependencyProperty ValidationRulesProperty = DependencyProperty.RegisterAttached("ValidationRules", typeof(ValidationRulesCollection), 
			typeof(ValidationHelper), new PropertyMetadata(null));

		public static BaseEdit GetBaseEdit(BaseEditSettings settings)
		{
			return (BaseEdit)settings.GetValue(BaseEditProperty);
		}

		public static void SetBaseEdit(BaseEditSettings settings, BaseEdit edit)
		{
			settings.SetValue(BaseEditProperty, edit);
		}

		public static readonly DependencyProperty BaseEditProperty = DependencyProperty.RegisterAttached("BaseEdit", typeof(BaseEdit),
			typeof(ValidationHelper), new PropertyMetadata(null, OnBaseEditChanged));

		private static readonly Dictionary<BaseEditSettings, BaseEdit> _edits = new Dictionary<BaseEditSettings, BaseEdit>();

		private static void OnBaseEditChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is BaseEditSettings settings))
				return;

			var oldEdit = _edits.TryGetValue(settings);

			if (oldEdit != null)
				oldEdit.Validate -= OnValidate;

			if (e.NewValue == null)
				return;

			var newEdit = (BaseEdit)e.NewValue;

			_edits[settings] = newEdit;
			newEdit.Validate += OnValidate;
		}

		private static void OnValidate(object sender, ValidationEventArgs e)
		{
			var edit = (BaseEdit)sender;
			var settings = _edits.FirstOrDefault(p => Equals(p.Value, edit)).Key;

			foreach (var rule in GetValidationRules(settings))
			{
				var result = rule.Validate(e.Value, CultureInfo.CurrentCulture);

				if (result.IsValid)
					continue;

				e.IsValid = false;
				e.ErrorContent = result.ErrorContent;
				break;
			}
		}
	}
}