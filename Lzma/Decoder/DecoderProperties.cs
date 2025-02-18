using System;

namespace Lzma
{
	/// <summary>
	/// LZMA decoder properties.
	/// </summary>
	[CLSCompliant(false)]
	public struct DecoderProperties
	{
		#region Fields

		private uint dictionarySize;
		private int lc;
		private int lp;
		private int pb;

		/// <summary>
		/// The default settings.
		/// </summary>
		public static readonly DecoderProperties Default = new DecoderProperties(1 << 24, 3, 0, 2);

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets size of the dictionary in bytes.
		/// </summary>
		public uint DictionarySize
		{
			get { return this.dictionarySize; }
			set { this.dictionarySize = value; }
		}

		/// <summary>
		/// Gets or sets the number of high bits of the previous byte to use as a context for literal decoding.
		/// </summary>
		public int LC
		{
			get { return this.lc; }
			set
			{
				this.lc = value;
				if (value < 0 || value > 8)
					throw new ArgumentException("Invalid value.");
			}
		}

		/// <summary>
		/// Gets or sets the number of low bits of the dictionary position to include in literal position state.
		/// </summary>
		public int LP
		{
			get { return this.lp; }
			set
			{
				this.lp = value;
				if (value < 0 || value > 4)
					throw new ArgumentException("Invalid value.");
			}
		}

		/// <summary>
		/// Gets or sets the number of low bits of the dictionary position to include in position state.
		/// </summary>
		public int PB
		{
			get { return this.pb; }
			set
			{
				this.pb = value;
				if (value < 0 || value > 4)
					throw new ArgumentException("Invalid value.");
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Creates new decoder properties.
		/// </summary>
		/// <param name="dictionarySize">The size of the dictionary in bytes.</param>
		/// <param name="lc">The number of literal context bits.</param>
		/// <param name="lp">The number of literal position bits.</param>
		/// <param name="pb">The number of position bits.</param>
		public DecoderProperties(uint dictionarySize, int lc, int lp, int pb)
		{
			this.dictionarySize = dictionarySize;
			this.lc = lc;
			this.lp = lp;
			this.pb = pb;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Compares two decoder property structures.
		/// </summary>
		/// <param name="prop">The other properties.</param>
		/// <returns></returns>
		public bool Compare(DecoderProperties prop)
		{
			return this.dictionarySize == prop.dictionarySize &&
				this.lc == prop.lc &&
				this.lp == prop.lp &&
				this.pb == prop.pb;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"dict = 0x{this.dictionarySize:X8}; lc = {this.lc}; lp = {this.lp}; pb = {this.pb}";
		}

		#endregion
	}
}
