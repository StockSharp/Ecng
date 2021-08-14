namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;

	using Ecng.Common;

	#endregion

	/// <summary>
	/// Provide validation for not null property.
	/// </summary>
	public class NotNullValidator<T> : BaseValidator<T>
		where T : class
	{
		#region BaseValidator Members

		/// <summary>
		/// Determines whether the value of an object is valid.
		/// </summary>
		/// <param name="value">The object value.</param>
		public override void Validate(T value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
		}

		#endregion
	}

	/// <summary>
	/// Declaratively instructs to perform not null validation.
	/// </summary>
	public class NotNullAttribute : BaseValidatorAttribute
	{
		#region BaseValidatorAttribute Members

		/// <summary>
		/// Creates the validator.
		/// </summary>
		/// <returns>Validator.</returns>
		public override BaseValidator CreateValidator(Type validationType)
		{
			if (validationType.IsByRef)
				validationType = validationType.GetElementType();

			return typeof(NotNullValidator<>).Make(validationType).CreateInstance<BaseValidator>();
		}

		#endregion
	}
}