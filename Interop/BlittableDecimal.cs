namespace Ecng.Interop
{
	using System.Runtime.InteropServices;

	using Ecng.Common;

	/// <summary>
	/// Represents a blittable version of a decimal value.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct BlittableDecimal
	{
		private int _bit0;
		private int _bit1;
		private int _bit2;
		private int _bit3;

		/// <summary>
		/// Gets or sets the decimal value.
		/// </summary>
		public decimal Value
		{
			get => new[] { _bit0, _bit1, _bit2, _bit3 }.To<decimal>();
			set
			{
				var bits = value.To<int[]>();

				_bit0 = bits[0];
				_bit1 = bits[1];
				_bit2 = bits[2];
				_bit3 = bits[3];
			}
		}

		/// <summary>
		/// Converts a decimal value to a <see cref="BlittableDecimal"/>.
		/// </summary>
		/// <param name="value">The decimal value to convert.</param>
		/// <returns>A <see cref="BlittableDecimal"/> representing the decimal value.</returns>
		public static explicit operator BlittableDecimal(decimal value)
		{
			return new BlittableDecimal { Value = value };
		}

		/// <summary>
		/// Converts a <see cref="BlittableDecimal"/> to a decimal value.
		/// </summary>
		/// <param name="value">The <see cref="BlittableDecimal"/> to convert.</param>
		/// <returns>A decimal value.</returns>
		public static implicit operator decimal(BlittableDecimal value)
		{
			return value.Value;
		}

		/// <summary>
		/// Returns a string that represents the decimal value.
		/// </summary>
		/// <returns>A string representation of the decimal value.</returns>
		public override string ToString() => Value.ToString();
	}
}