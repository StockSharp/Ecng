namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;
	using System.Reflection;
	using System.Text.RegularExpressions;

	using Ecng.Common;
	using Ecng.Localization;

	#endregion

	/// <summary>
	/// 
	/// </summary>
	public class StringValidator : BaseValidator<string>
	{
		#region StringValidator.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="StringValidator"/> class.
		/// </summary>
		public StringValidator()
			: this(new Range<int>(0, int.MaxValue))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StringValidator"/> class.
		/// </summary>
		/// <param name="length">The length.</param>
		public StringValidator(Range<int> length)
		{
			Length = length;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StringValidator"/> class.
		/// </summary>
		/// <param name="length">The length.</param>
		/// <param name="regex">The regex.</param>
		public StringValidator(Range<int> length, string regex)
			: this(length)
		{
			Regex = regex;
		}

		#endregion

		#region Length

		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>The length.</value>
		public Range<int> Length { get; }

		#endregion

		#region Regex

		private static readonly FieldInfo _parrernField = typeof(FieldInfo).GetField("pattern", BindingFlags.NonPublic | BindingFlags.Instance);

		private Regex _regex;

		/// <summary>
		/// Gets or sets the regex.
		/// </summary>
		/// <value>The regex.</value>
		public string Regex
		{
			get
			{
				if (_regex != null)
					return (string)_parrernField.GetValue(_regex);
				else
					return null;
			}
			set => _regex = new Regex(value, Options);
		}

		#endregion

		#region Options

		private RegexOptions _options =
			RegexOptions.IgnoreCase |
			RegexOptions.Multiline |
			RegexOptions.CultureInvariant |
			RegexOptions.IgnorePatternWhitespace |
			RegexOptions.Compiled;

		/// <summary>
		/// Gets or sets the options.
		/// </summary>
		/// <value>The options.</value>
		public RegexOptions Options
		{
			get => _options;
			set => _options = value;
		}

		#endregion

		#region BaseValidator<string> Members

		/// <summary>
		/// Validates the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		public override void Validate(string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (!Length.Contains(value.Length))
				throw new ArgumentOutOfRangeException(nameof(value), "Value is {0}. Length must be between {1}.".Translate().Put(value, Length));

			if (_regex != null && !_regex.IsMatch(value))
				throw new ArgumentException(nameof(value));
		}

		#endregion
	}

	/// <summary>
	/// 
	/// </summary>
	public class StringAttribute : LengthAttribute
	{
		#region StringAttribute.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="StringAttribute"/> class.
		/// </summary>
		public StringAttribute()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StringAttribute"/> class.
		/// </summary>
		/// <param name="maxLength">Length of the max.</param>
		public StringAttribute(int maxLength)
			: base(maxLength)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StringAttribute"/> class.
		/// </summary>
		/// <param name="minLength">Length of the min.</param>
		/// <param name="maxLength">Length of the max.</param>
		public StringAttribute(int minLength, int maxLength)
			: base(minLength, maxLength)
		{
		}

		#endregion

		/// <summary>
		/// Gets or sets the regex pattern.
		/// </summary>
		/// <value>The regex pattern.</value>
		public string Regex { get; set; }

		#region BaseValidatorAttribute Members

		/// <summary>
		/// Creates the validator.
		/// </summary>
		/// <returns>Validator.</returns>
		public override BaseValidator CreateValidator(Type validationType)
		{
			var validator = new StringValidator(Length);
			
			if (!Regex.IsEmpty())
				validator.Regex = Regex;

			return validator;
		}

		#endregion
	}
}