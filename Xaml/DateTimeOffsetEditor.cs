namespace Ecng.Xaml
{
	using System;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.Editors.Helpers;
	using DevExpress.Xpf.Editors.Services;
	using DevExpress.Xpf.Editors.Settings;
	using DevExpress.Xpf.Editors.Validation.Native;

	using Ecng.Common;

	public class DateTimeOffsetEditor : DateEditSettings
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

	public class DateTimeOffsetEdit : DateEdit
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

	public class DateTimeOffsetEditStrategy : DateEditStrategy
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

			if (value is DateTime dt)
				return new DateTimeOffset(dt);

			var str = value as string;

			if (str.IsEmpty())
				return null;

			if (DateTimeOffset.TryParse(str, out var offset))
				return offset;

			return null;
		}

		public static object GetDateTime(object value)
		{
			if (value is DateTime)
				return value;

			if (value is DateTimeOffset dto)
				return dto.LocalDateTime;

			var str = value as string;

			return !str.IsEmpty() && DateTimeOffset.TryParse(str, out var offset)
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

		public override object ProcessConversion(object value, UpdateEditorSource updateEditorSource)
		{
			return DateTimeOffsetConverter.GetDateTimeOffset(value);
		}
	}
}