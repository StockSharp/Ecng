namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	/// <summary>
	/// Class for validation throughout <see cref="Range"/>. 
	/// </summary>
	/// <typeparam name="T">The type of objects that can be validated.</typeparam>
	public class RangeValidator<T> : BaseValidator<T>
		where T : IComparable<T>
	{
		#region RangeValidator.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="RangeValidator{T}"/> class.
		/// </summary>
		public RangeValidator()
			: this(new Range<T>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RangeValidator{T}"/> class.
		/// </summary>
		/// <param name="minValue">The min value.</param>
		/// <param name="maxValue">The max value.</param>
		public RangeValidator(T minValue, T maxValue)
			: this(new Range<T>(minValue, maxValue))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RangeValidator{T}"/> class.
		/// </summary>
		/// <param name="range">The range.</param>
		public RangeValidator(Range<T> range)
		{
			Range = range;
		}

		#endregion

		/// <summary>
		/// Gets the range.
		/// </summary>
		/// <value>The range.</value>
		public Range<T> Range { get; }

		#region BaseValidator<T> Members

		/// <summary>
		/// Validates the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		public override void Validate(T value)
		{
			if (!Range.Contains(value))
				throw new ArgumentOutOfRangeException(nameof(value));
		}

		#endregion
	}

	/// <summary>
	/// 
	/// </summary>
	public class RangeAttribute : BaseValidatorAttribute
	{
		/// <summary>
		/// Gets or sets the min value.
		/// </summary>
		/// <value>The min value.</value>
		public virtual object MinValue { get; set; }

		/// <summary>
		/// Gets or sets the max value.
		/// </summary>
		/// <value>The max value.</value>
		public virtual object MaxValue { get; set; }

		#region BaseValidatorAttribute Members

		/// <summary>
		/// Creates the validator.
		/// </summary>
		/// <param name="validationType"></param>
		/// <returns>Validator.</returns>
		public override BaseValidator CreateValidator(Type validationType)
		{
			if (validationType is null)
				throw new ArgumentNullException(nameof(validationType));

			if (validationType.IsByRef)
				validationType = validationType.GetElementType();

			var method = typeof(RangeAttribute).GetMember<MethodInfo>("CreateValidator").Make(validationType);
			return (BaseValidator)method.Invoke(this, null);
		}

		#endregion

		private BaseValidator<T> CreateValidator<T>()
			where T : IComparable<T>
		{
			var validator = new RangeValidator<T>();

			if (MinValue != null)
				validator.Range.Min = MinValue.To<T>();

			if (MaxValue != null)
				validator.Range.Max = MaxValue.To<T>();

			return validator;
		}
	}
}