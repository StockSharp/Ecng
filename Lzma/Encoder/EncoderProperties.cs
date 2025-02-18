using System;
using System.Diagnostics.Contracts;

namespace Lzma
{
	/// <summary>
	/// LZMA encoder properties.
	/// </summary>
	[CLSCompliant(false)]
	public struct EncoderProperties
	{
		#region Fields

		private uint workingSize;
		private uint dictionarySize;

		private int lc;
		private int lp;
		private int pb;

		private bool writeEndMarker;

		/// <summary>
		/// The default value.
		/// </summary>
		public static readonly EncoderProperties Default = new EncoderProperties(1u << 12, 1u << 24, 3, 0, 2, true);

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the encoding buffer size.
		/// </summary>
		public uint WorkingSize
		{
			get { return this.workingSize; }
			set { this.workingSize = value; }
		}

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

		/// <summary>
		/// Gets or sets a value indicating whether an end marker should be written when the encoder has finished.
		/// </summary>
		public bool WriteEndMarker
		{
			get { return this.writeEndMarker; }
			set { this.writeEndMarker = value; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Creates new decoder properties.
		/// </summary>
		/// <param name="workingSize">The maximum size of the encoding buffer.</param>
		/// <param name="dictionarySize">The size of the dictionary in bytes.</param>
		/// <param name="lc">The number of literal context bits.</param>
		/// <param name="lp">The number of literal position bits.</param>
		/// <param name="pb">The number of position bits.</param>
		/// <param name="writeEndMarker">Determines whether an end marker is written on close.</param>
		public EncoderProperties(uint workingSize, uint dictionarySize, int lc, int lp, int pb, bool writeEndMarker)
		{
			this.workingSize = workingSize;
			this.dictionarySize = dictionarySize;

			this.lc = lc;
			this.lp = lp;
			this.pb = pb;

			this.writeEndMarker = writeEndMarker;
		}

		#endregion

		#region Methods

		/// <inheritdoc />
		[Pure]
		public override bool Equals(object obj)
		{
			if (!(obj is EncoderProperties))
				return false;

			return this.Equals((EncoderProperties)obj);
		}

		/// <summary>
		/// Gets the decoder properties compatible with the encoder properties.
		/// </summary>
		/// <returns></returns>
		[Pure]
		public DecoderProperties GetDecoderProperties()
		{
			return new DecoderProperties
			{
				DictionarySize = this.DictionarySize,
				LC = this.LC,
				LP = this.LP,
				PB = this.PB
			};
		}

		/// <summary>
		/// Compares two encoder property structures.
		/// </summary>
		/// <param name="prop">The other properties.</param>
		/// <returns></returns>
		[Pure]
		public bool Equals(EncoderProperties prop)
		{
			return this.workingSize == prop.workingSize && 
				this.dictionarySize == prop.dictionarySize &&
				this.lc == prop.lc &&
				this.lp == prop.lp &&
				this.pb == prop.pb &&
				this.writeEndMarker == prop.writeEndMarker;
		}

		/// <inheritdoc />
		[Pure]
		public override int GetHashCode()
		{
			uint hash = (this.workingSize ^ this.dictionarySize) ^ (((uint)this.lc) | ((uint)this.lp << 4) | ((uint)this.pb << 8));
			if (this.writeEndMarker)
				hash ^= 0xFFFFFFFFu;
			return unchecked((int)hash);
		}

		/// <inheritdoc />
		[Pure]
		public override string ToString()
		{
			return $"dict = 0x{this.dictionarySize:X8}; lc = {this.lc}; lp = {this.lp}; pb = {this.pb}";
		}

		#endregion
	}
}
