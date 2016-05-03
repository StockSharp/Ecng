namespace Ecng.Xaml.DevExp
{
	using System;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.Editors.Helpers;
	using DevExpress.Xpf.Editors.Services;
	using DevExpress.Xpf.Editors.Settings;
	using DevExpress.Xpf.Editors.Validation.Native;

	using Ecng.Common;

	class DateTimeOffsetEditor : DateEditSettings
	{
		static DateTimeOffsetEditor()
		{
			RegisterCustomEdit();
		}

		public static void RegisterCustomEdit()
		{
			EditorSettingsProvider.Default.RegisterUserEditor(
				typeof(DateTimeOffsetEdit), typeof(DateTimeOffsetEditor), 
				() => new DateTimeOffsetEdit(), () => new DateTimeOffsetEditor());
		}
	}

	class DateTimeOffsetEdit : DateEdit
	{
		static DateTimeOffsetEdit()
		{
			DateTimeOffsetEditor.RegisterCustomEdit();
		}

		public DateTimeOffsetEdit()
		{
			ValidateOnTextInput = false;
		}

		protected override EditStrategyBase CreateEditStrategy()
		{
			return new DateTimeOffsetEditStrategy(this);
		}
	}

	class DateTimeOffsetEditStrategy : DateEditStrategy
	{
		public DateTimeOffsetEditStrategy(DateTimeOffsetEdit edit)
			: base(edit)
		{
		}

		protected override void RegisterUpdateCallbacks()
		{
			base.RegisterUpdateCallbacks();
			PropertyUpdater.Register(BaseEdit.EditValueProperty, DateTimeOffsetConverter.GetDateTimeOffset, DateTimeOffsetConverter.GetDateTimeOffset);
		}

		public override bool ProvideEditValue(object value, out object provideValue, UpdateEditorSource updateSource)
		{
			base.ProvideEditValue(value, out provideValue, updateSource);
			provideValue = DateTimeOffsetConverter.GetDateTimeOffset(value);
			return true;
		}

		protected override EditorSpecificValidator CreateEditorValidatorService()
		{
			base.CreateEditorValidatorService();
			return new DateTimeOffsetValidator(Editor);
		}
	}

	static class DateTimeOffsetConverter
	{
		public static object GetDateTimeOffset(object value)
		{
			if (value is DateTimeOffset)
				return value;

			if (value is DateTime)
				return new DateTimeOffset((DateTime)value);

			var str = value as string;

			if (str.IsEmpty())
				return null;

			DateTimeOffset offset;
			if (DateTimeOffset.TryParse(str, out offset))
				return offset;

			return null;
		}

		public static object GetDateTime(object value)
		{
			if (value is DateTime)
				return value;

			if (value is DateTimeOffset)
				return ((DateTimeOffset)value).LocalDateTime;

			var str = value as string;
			DateTimeOffset offset;

			return !str.IsEmpty() && DateTimeOffset.TryParse(str, out offset) 
				? (object)offset 
				: null;
		}
	}

	class DateTimeOffsetValidator : DateEditValidator
	{
		public DateTimeOffsetValidator(TextEditBase editor)
			: base(editor)
		{
		}

		protected override StrategyValidatorBase CreateValidator()
		{
			return new DateTimeOffsetStrategyValidatorBase(OwnerEdit);
		}
	}

	class DateTimeOffsetStrategyValidatorBase : StrategyValidatorBase
	{
		public DateTimeOffsetStrategyValidatorBase(BaseEdit edit)
			: base(edit)
		{
		}

		public override object ProcessConversion(object value)
		{
			return DateTimeOffsetConverter.GetDateTimeOffset(value);
		}
	}
}