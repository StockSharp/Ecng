﻿namespace Ecng.Common
{
	/// <summary>
	/// Extension class for <see cref="Currency"/>.
	/// </summary>
	public static class CurrencyHelper
	{
		/// <summary>
		/// Determines the specified type is crypto currency.
		/// </summary>
		/// <param name="type">Currency type.</param>
		/// <returns>Check result.</returns>
		public static bool IsCrypto(this CurrencyTypes type)
			=> type.GetAttributeOfType<CryptoAttribute>() != null;

		/// <summary>
		/// Cast <see cref="decimal"/> to <see cref="Currency"/>.
		/// </summary>
		/// <param name="value">Currency value.</param>
		/// <param name="type">Currency type.</param>
		/// <returns>Currency.</returns>
		public static Currency ToCurrency(this decimal value, CurrencyTypes type)
			=> new() { Type = type, Value = value };
	}
}