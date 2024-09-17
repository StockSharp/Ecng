namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TCollectiom"></typeparam>
	/// <typeparam name="TItem"></typeparam>
	/// <remarks>
	/// Initializes a new instance of the <see cref="LengthValidator&lt;TCollectiom, TItem&gt;"/> class.
	/// </remarks>
	/// <param name="length">The length.</param>
	public class LengthValidator<TCollectiom, TItem>(Range<int> length) : BaseValidator<TCollectiom>
		where TCollectiom : ICollection<TItem>
	{
		#region LengthValidator.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="LengthValidator&lt;TCollectiom, TItem&gt;"/> class.
		/// </summary>
		public LengthValidator()
			: this(new Range<int>(0, int.MaxValue))
		{
		}

		#endregion

		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>The length.</value>
		public Range<int> Length { get; } = length;

		#region BaseValidator<ICollection<T>> Members

		/// <summary>
		/// Validates the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		public override void Validate(TCollectiom value)
		{
			if (value.IsNull())
				throw new ArgumentNullException(nameof(value));

			if (!Length.Contains(value.Count))
				throw new ArgumentOutOfRangeException(nameof(value));
		}

		#endregion
	}

	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="LengthAttribute"/> class.
	/// </remarks>
	/// <param name="minLength">Length of the min.</param>
	/// <param name="maxLength">Length of the max.</param>
	public class LengthAttribute(int minLength, int maxLength) : BaseValidatorAttribute
	{
		#region LengthAttribute.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="LengthAttribute"/> class.
		/// </summary>
		public LengthAttribute()
			: this(int.MaxValue)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LengthAttribute"/> class.
		/// </summary>
		/// <param name="maxLength">Length of the max.</param>
		public LengthAttribute(int maxLength)
			: this(0, maxLength)
		{
		}

		#endregion

		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>The length.</value>
		public Range<int> Length { get; } = new Range<int>(minLength, maxLength);

		#region BaseValidatorAttribute Members

		/// <summary>
		/// Creates the validator.
		/// </summary>
		/// <returns>Validator.</returns>
		public override BaseValidator CreateValidator(Type validationType)
		{
			return typeof(LengthValidator<,>).Make(validationType, validationType.GetItemType()).CreateInstance<BaseValidator>(Length);
		}

		#endregion
	}
}