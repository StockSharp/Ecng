namespace Ecng.Interop
{
	using System.Runtime.InteropServices;

	using Ecng.Common;

	[StructLayout(LayoutKind.Sequential)]
	public struct BlittableDecimal
	{
		private int _bit0;
		private int _bit1;
		private int _bit2;
		private int _bit3;

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

		public static explicit operator BlittableDecimal(decimal value)
		{
			return new BlittableDecimal { Value = value };
		}

		public static implicit operator decimal(BlittableDecimal value)
		{
			return value.Value;
		}

		public override string ToString() => Value.ToString();
	}
}