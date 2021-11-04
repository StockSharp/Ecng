namespace Ecng.Common
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Currency.
	/// </summary>
	[DataContract]
	[Serializable]
	public class Currency : Equatable<Currency>
	{
		/// <summary>
		/// Currency type. The default is <see cref="CurrencyTypes.USD"/>.
		/// </summary>
		[DataMember]
		public CurrencyTypes Type { get; set; } = CurrencyTypes.USD;

		/// <summary>
		/// Absolute value in <see cref="CurrencyTypes"/>.
		/// </summary>
		[DataMember]
		public decimal Value { get; set; }

		/// <summary>
		/// Create a copy of <see cref="Currency"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Currency Clone() => new() { Type = Type, Value = Value };

		/// <summary>
		/// Compare <see cref="Currency"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(Currency other)
			=> Type == other.Type && Value == other.Value;

		/// <summary>
		/// Get the hash code of the object <see cref="Currency"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode() => Type.GetHashCode() ^ Value.GetHashCode();

		/// <inheritdoc />
		public override string ToString() => $"{Value} {Type}";

		/// <summary>
		/// Cast <see cref="decimal"/> object to the type <see cref="Currency"/>.
		/// </summary>
		/// <param name="value"><see cref="decimal"/> value.</param>
		/// <returns>Object <see cref="Currency"/>.</returns>
		[Obsolete]
		public static implicit operator Currency(decimal value) => new() { Value = value };

		/// <summary>
		/// Cast object from <see cref="Currency"/> to <see cref="decimal"/>.
		/// </summary>
		/// <param name="value">Object <see cref="Currency"/>.</param>
		/// <returns><see cref="decimal"/> value.</returns>
		[Obsolete]
		public static explicit operator decimal(Currency value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			return value.Value;
		}

		private static void CheckArgs(Currency c1, Currency c2)
		{
			if (c1 is null)
				throw new ArgumentNullException(nameof(c1));

			if (c2 is null)
				throw new ArgumentNullException(nameof(c2));

			if (c1.Type != c2.Type)
				throw new InvalidOperationException($"c1.{c1.Type} != c2.{c2.Type}");
		}

		/// <summary>
		/// Add the two objects <see cref="Currency"/>.
		/// </summary>
		/// <param name="c1">First object <see cref="Currency"/>.</param>
		/// <param name="c2">Second object <see cref="Currency"/>.</param>
		/// <returns>The result of addition.</returns>
		/// <remarks>
		/// The values must be the same <see cref="Type"/>.
		/// </remarks>
		public static Currency operator +(Currency c1, Currency c2)
		{
			CheckArgs(c1, c2);

			return (c1.Value + c2.Value).ToCurrency(c1.Type);
		}

		/// <summary>
		/// Subtract one value from another value.
		/// </summary>
		/// <param name="c1">First object <see cref="Currency"/>.</param>
		/// <param name="c2">Second object <see cref="Currency"/>.</param>
		/// <returns>The result of the subtraction.</returns>
		public static Currency operator -(Currency c1, Currency c2)
		{
			CheckArgs(c1, c2);

			return (c1.Value - c2.Value).ToCurrency(c1.Type);
		}

		/// <summary>
		/// Multiply one value to another.
		/// </summary>
		/// <param name="c1">First object <see cref="Currency"/>.</param>
		/// <param name="c2">Second object <see cref="Currency"/>.</param>
		/// <returns>The result of the multiplication.</returns>
		public static Currency operator *(Currency c1, Currency c2)
		{
			CheckArgs(c1, c2);

			return (c1.Value * c2.Value).ToCurrency(c1.Type);
		}

		/// <summary>
		/// Divide one value to another.
		/// </summary>
		/// <param name="c1">First object <see cref="Currency"/>.</param>
		/// <param name="c2">Second object <see cref="Currency"/>.</param>
		/// <returns>The result of the division.</returns>
		public static Currency operator /(Currency c1, Currency c2)
		{
			CheckArgs(c1, c2);

			return (c1.Value / c2.Value).ToCurrency(c1.Type);
		}
	}
}