namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;

	#endregion

	/// <summary>
	/// Acts as a base class for deriving a validation class so that a value of an object can be verified.
	/// </summary>
	public abstract class BaseValidator
	{
		/// <summary>
		/// Validates the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		public abstract void Validate(object value);
	}

	/// <summary>
	/// Acts as a base class for deriving a validation class so that a value of an object can be verified.
	/// </summary>
	/// <typeparam name="T">The type of objects that can be validated.</typeparam>
	public abstract class BaseValidator<T> : BaseValidator
	{
		/// <summary>
		/// Validates the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		public abstract void Validate(T value);

		#region BaseValidator Members

		/// <summary>
		/// Validates the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		public override void Validate(object value)
		{
			Validate((T)value);
		}

		#endregion
	}

	/// <summary>
	/// 
	/// </summary>
	[AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event)]
	public abstract class BaseValidatorAttribute : Attribute
	{
		/// <summary>
		/// Creates the validator.
		/// </summary>
		/// <param name="validationType">Type of the validation.</param>
		/// <returns>Validator.</returns>
		public abstract BaseValidator CreateValidator(Type validationType);
	}
}