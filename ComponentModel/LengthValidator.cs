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
	public class LengthValidator<TCollectiom, TItem> : BaseValidator<TCollectiom>
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

		/// <summary>
		/// Initializes a new instance of the <see cref="LengthValidator&lt;TCollectiom, TItem&gt;"/> class.
		/// </summary>
		/// <param name="length">The length.</param>
		public LengthValidator(Range<int> length)
		{
			Length = length;
		}

		#endregion

		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>The length.</value>
		public Range<int> Length { get; private set; }

		#region BaseValidator<ICollection<T>> Members

		/// <summary>
		/// Validates the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		public override void Validate(TCollectiom value)
		{
			if (value.IsNull())
				throw new ArgumentNullException("value");

			if (!Length.Contains(value.Count))
				throw new ArgumentOutOfRangeException("value");
		}

		#endregion
	}

	/// <summary>
	/// 
	/// </summary>
	public class LengthAttribute : BaseValidatorAttribute
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

		/// <summary>
		/// Initializes a new instance of the <see cref="LengthAttribute"/> class.
		/// </summary>
		/// <param name="minLength">Length of the min.</param>
		/// <param name="maxLength">Length of the max.</param>
		public LengthAttribute(int minLength, int maxLength)
		{
			Length = new Range<int>(minLength, maxLength);
		}

		#endregion

		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>The length.</value>
		public Range<int> Length { get; private set; }

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