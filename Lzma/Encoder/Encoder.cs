using System;
using System.IO;

namespace Lzma
{
	/// <summary>
	/// Represents the LZMA encoder.
	/// </summary>
	public abstract class Encoder
	{
		#region Fields

		// setup.
		private readonly EncoderProperties properties;
		private readonly RangeEncoder rangeEncoder;
		private readonly SlidingWindow window;

		// special encoders.
		private readonly LiteralEncoder literalEncoder;
		private readonly DistanceEncoder distanceEncoder;
		private readonly LengthEncoder lengthEncoder;
		private readonly LengthEncoder repeatedLengthEncoder;

		// probabilities of control bits.
		private readonly BitEncoder[] isMatch;
		private readonly BitEncoder[] isRep;
		private readonly BitEncoder[] isRep0;
		private readonly BitEncoder[] isRep0Long;
		private readonly BitEncoder[] isRep1;
		private readonly BitEncoder[] isRep2;

		// constants.
		protected readonly uint PositionMask;

		// encode loop state
		protected uint Rep0;
		protected uint Rep1;
		protected uint Rep2;
		protected uint Rep3;
		protected State State;
		
		#endregion

		#region Properties

		/// <summary>
		/// Gets the encoder properties.
		/// </summary>
		public EncoderProperties Properties => this.properties;

		/// <summary>
		/// Gets the sliding window.
		/// </summary>
		protected SlidingWindow Window => this.window;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new Encoder.
		/// </summary>
		protected Encoder(Stream stream, EncoderProperties properties)
		{
			// setup.
			this.properties = properties;
			this.rangeEncoder = new RangeEncoder(stream);
			this.window = new SlidingWindow(this.properties.DictionarySize + this.properties.WorkingSize);

			// special encoders.
			this.literalEncoder = new LiteralEncoder(this.properties.LP, this.properties.LC);
			this.distanceEncoder = new DistanceEncoder();
			this.lengthEncoder = new LengthEncoder(this.properties.PB);
			this.repeatedLengthEncoder = new LengthEncoder(this.properties.PB);

			// probabilities of control bits.
			this.isMatch = new BitEncoder[State.NumStates << this.properties.PB];
			this.isRep = new BitEncoder[State.NumStates];
			this.isRep0 = new BitEncoder[State.NumStates];
			this.isRep0Long = new BitEncoder[State.NumStates << this.properties.PB];
			this.isRep1 = new BitEncoder[State.NumStates];
			this.isRep2 = new BitEncoder[State.NumStates];

			// constants.
			this.PositionMask = ((1u << this.properties.PB) - 1u);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the encoder.
		/// This resets everything to the initial state.
		/// </summary>
		public virtual void Initialize()
		{
			// setup.
			this.rangeEncoder.Initialize();
			this.window.Initialize();

			// special encoders.
			this.literalEncoder.Initialize();
			this.distanceEncoder.Initialize();
			this.lengthEncoder.Initialize();
			this.repeatedLengthEncoder.Initialize();

			// probabilities of control bits.
			this.isMatch.InitializeAll();
			this.isRep.InitializeAll();
			this.isRep0.InitializeAll();
			this.isRep0Long.InitializeAll();
			this.isRep1.InitializeAll();
			this.isRep2.InitializeAll();

			// encode loop state.
			this.Rep0 = 0;
			this.Rep1 = 0;
			this.Rep2 = 0;
			this.Rep3 = 0;
			this.State.Value = 0;
		}

		/// <summary>
		/// Writes data to the encoding buffer.
		/// </summary>
		/// <param name="buffer">The buffer containing the uncompressed data to encode.</param>
		/// <param name="offset">The offset in the buffer.</param>
		/// <param name="count">The number of bytes to encode.</param>
		/// <returns>The number of bytes written. If this value is less than count, the encoding buffer is full. A call to Encode() is required in order to clear the buffer.</returns>
		public uint Write(byte[] buffer, uint offset, uint count)
		{
			// check for out-of-bounds.
			uint limit = this.properties.WorkingSize - this.window.currentWorkingSize;
			if (count > limit)
				count = limit;

			// write to sliding window.
			this.window.WriteWorking(buffer, offset, count);

			return count;
		}

		/// <summary>
		/// Encodes the data in the encoding buffer.
		/// </summary>
		/// <param name="flush">
		/// Specifies whether the current working buffer should be encoded completely. 
		/// If this is false, some data will be kept in the working buffer to ensure that no matches get truncated because the buffer runs out.
		/// </param>
		public void Encode(bool flush)
		{
			uint workingLimit;

			if (flush)
			{
				// just completely clear the working buffer.
				workingLimit = 0u;
			}
			else
			{
				// keep at least maxmatchlength * 2 bytes in the buffer to ensure we don't miss any long matches that might get truncated.
				workingLimit = (Constants.LengthMax + Constants.MinMatchLength - 1) << 1;

				// guarantee that we code at least one packet.
				if (workingLimit >= this.window.currentWorkingSize)
					workingLimit = this.window.currentWorkingSize - 1;
			}

			while (this.window.currentWorkingSize > workingLimit)
			{
				Match match;
				this.FindMatch(out match);

				uint posState = this.window.totalPosition & this.PositionMask;

				if (match.Length > 0)
				{
					// it's a match.
					this.isMatch[(this.State.Value << this.properties.PB) + posState].Encode1(this.rangeEncoder);

					// find out repeated distance index.
					int repIndex = this.GetRepeatedDistanceIndex(match.Distance);

					// is it a repeated distance?
					if (repIndex >= 0)
					{
						// it's a repeated distance.
						this.isRep[this.State.Value].Encode1(this.rangeEncoder);

						uint newRep0 = this.Rep0;

						if (repIndex == 0)
						{
							// it's rep0, a long one.
							this.isRep0[this.State.Value].Encode0(this.rangeEncoder);

							// can we encode it as short rep?
							if (match.Length == 1)
								this.isRep0Long[(this.State.Value << this.properties.PB) + posState].Encode0(this.rangeEncoder);
							else
								this.isRep0Long[(this.State.Value << this.properties.PB) + posState].Encode1(this.rangeEncoder);
						}
						else if (repIndex == 1)
						{
							// it's rep1.
							this.isRep0[this.State.Value].Encode1(this.rangeEncoder);
							this.isRep1[this.State.Value].Encode0(this.rangeEncoder);

							newRep0 = this.Rep1;
							this.Rep1 = this.Rep0;
						}
						else if (repIndex == 2)
						{
							// it's rep2.
							this.isRep0[this.State.Value].Encode1(this.rangeEncoder);
							this.isRep1[this.State.Value].Encode1(this.rangeEncoder);
							this.isRep2[this.State.Value].Encode0(this.rangeEncoder);

							newRep0 = this.Rep2;
							this.Rep2 = this.Rep1;
							this.Rep1 = this.Rep0;
						}
						else if (repIndex == 3)
						{
							// it's rep3.
							this.isRep0[this.State.Value].Encode1(this.rangeEncoder);
							this.isRep1[this.State.Value].Encode1(this.rangeEncoder);
							this.isRep2[this.State.Value].Encode1(this.rangeEncoder);

							newRep0 = this.Rep3;
							this.Rep3 = this.Rep2;
							this.Rep2 = this.Rep1;
							this.Rep1 = this.Rep0;
						}

						this.Rep0 = newRep0;

						if (repIndex == 0 && match.Length == 1)
						{
							// short rep is always length = 1.
							this.State.UpdateShortRep();
						}
						else
						{
							// encode match length.
							this.repeatedLengthEncoder.Encode(this.rangeEncoder, posState, match.Length);
							this.State.UpdateRep();
						}
					}
					else
					{
						// it's not a repeated distance.
						this.isRep[this.State.Value].Encode0(this.rangeEncoder);

						// encode match length.
						this.lengthEncoder.Encode(this.rangeEncoder, posState, match.Length);

						// update state.
						this.State.UpdateMatch();

						this.Rep3 = this.Rep2;
						this.Rep2 = this.Rep1;
						this.Rep1 = this.Rep0;
						this.Rep0 = match.Distance;

						// encode distance.
						this.distanceEncoder.Encode(this.rangeEncoder, match.Length, this.Rep0);
					}

#if VERBOSE
					for (uint i = 0; i < match.Length; i++)
					{
						byte b = this.window.ReadHistory(match.Distance);
						this.Process(1);

						if (i == 0)
							System.Console.WriteLine("copy\t{0:X2}\t'{1}'\t{2}: {3}, {4}", b, char.IsControl((char)b) ? ' ' : (char)b, repIndex >= 0 ? "rep" + repIndex : "match", match.Distance, match.Length);
						else
							System.Console.WriteLine("copy\t{0:X2}\t'{1}'", b, char.IsControl((char)b) ? ' ' : (char)b);				
					}
#else
					// skip matched bytes.
					this.Process(match.Length);
#endif
				}
				else
				{
					// get byte.
					byte b = this.window.ReadWorking(0);

					// check if we can encode a short repeat.
					if (!this.window.IsEmpty && b == this.window.ReadHistory(this.Rep0))
					{
						// it's a match.
						this.isMatch[(this.State.Value << this.properties.PB) + posState].Encode1(this.rangeEncoder);

						// it's a repeated distance.
						this.isRep[this.State.Value].Encode1(this.rangeEncoder);

						// it's rep0.
						this.isRep0[this.State.Value].Encode0(this.rangeEncoder);

						// it's a short rep.
						this.isRep0Long[(this.State.Value << this.properties.PB) + posState].Encode0(this.rangeEncoder);

#if VERBOSE
						System.Console.WriteLine("srep\t{0:X2}\t'{1}'\trep0: {2}", b, char.IsControl((char)b) ? ' ' : (char)b, this.rep0);
#endif

						this.State.UpdateShortRep();
						this.Process(1);
					}
					else
					{
						// it's not a match.
						this.isMatch[(this.State.Value << this.properties.PB) + posState].Encode0(this.rangeEncoder);

						byte previousByte = this.window.IsEmpty ? (byte)0 : this.window.ReadHistory(0);

						if (this.State.IsLiteral)
						{
							// encode literal.
							this.literalEncoder.Encode(this.rangeEncoder, this.window.totalPosition, previousByte, b);
#if VERBOSE
							System.Console.WriteLine("lit\t{0:X2}\t'{1}'", b, char.IsControl((char)b) ? ' ' : (char)b);
#endif
						}
						else
						{
							// encode delta literal.
							this.literalEncoder.EncodeDelta(this.rangeEncoder, this.window.totalPosition, previousByte, b, this.window.ReadHistory(this.Rep0));
#if VERBOSE
							System.Console.WriteLine("dlit\t{0:X2}\t'{1}'", b, char.IsControl((char)b) ? ' ' : (char)b);
#endif
						}

						// move forward 1 byte.
						this.State.UpdateLiteral();
						this.Process(1);
					}
				}
			}
		}

		/// <summary>
		/// Flush the encoder and align to byte-boundary.
		/// This action requires the decoder to be reset as well.
		/// </summary>
		public void FlushAndAlign()
		{
			this.Encode(true);
			this.rangeEncoder.Flush();
			this.rangeEncoder.Initialize();
		}

		/// <summary>
		/// Processes the specified number of bytes.
		/// </summary>
		/// <param name="numBytes"></param>
		protected virtual void Process(uint numBytes)
		{
			this.window.ProcessWorking(numBytes);
		}

		/// <summary>
		/// Gets the rep index for the specified distance.
		/// </summary>
		/// <param name="distance">The distance.</param>
		/// <returns>The rep index, or -1 if it is not a repeated distance.</returns>
		protected int GetRepeatedDistanceIndex(uint distance)
		{
			if (distance == this.Rep0) return 0;
			if (distance == this.Rep1) return 1;
			if (distance == this.Rep2) return 2;
			if (distance == this.Rep3) return 3;
			return -1;
		}

		/// <summary>
		/// Gets the price for encoding the specified match.
		/// </summary>
		/// <param name="posState"></param>
		/// <param name="distance"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		protected uint GetMatchPrice(uint posState, uint distance, uint length)
		{
			uint price = this.isMatch[(this.State.Value << this.properties.PB) + posState].Price1;

			if (distance == this.Rep0)
			{
				price += this.isRep[this.State.Value].Price1;
				price += this.isRep0[this.State.Value].Price0;

				if (length == 1)
				{
					price += this.isRep0Long[(this.State.Value << this.properties.PB) + posState].Price0;
				}
				else
				{
					price += this.isRep0Long[(this.State.Value << this.properties.PB) + posState].Price1;
					price += this.repeatedLengthEncoder.GetPrice(posState, length);
				}
			}
			else if (distance == this.Rep1)
			{
				price += this.isRep[this.State.Value].Price1;
				price += this.isRep0[this.State.Value].Price1;
				price += this.isRep1[this.State.Value].Price0;
				price += this.repeatedLengthEncoder.GetPrice(posState, length);
			}
			else if (distance == this.Rep2)
			{
				price += this.isRep[this.State.Value].Price1;
				price += this.isRep0[this.State.Value].Price1;
				price += this.isRep1[this.State.Value].Price1;
				price += this.isRep2[this.State.Value].Price0;
				price += this.repeatedLengthEncoder.GetPrice(posState, length);
			}
			else if (distance == this.Rep3)
			{
				price += this.isRep[this.State.Value].Price1;
				price += this.isRep0[this.State.Value].Price1;
				price += this.isRep1[this.State.Value].Price1;
				price += this.isRep2[this.State.Value].Price1;
				price += this.repeatedLengthEncoder.GetPrice(posState, length);
			}
			else
			{
				price += this.isRep[this.State.Value].Price0;
				price += this.lengthEncoder.GetPrice(posState, length);
				price += this.distanceEncoder.GetPrice(length, distance);
			}

			return price;
		}

		/// <summary>
		/// Finds a match for the current encoder position.
		/// </summary>
		/// <param name="match">The match that should be used to encode the next sequence of bytes.</param>
		protected abstract void FindMatch(out Match match);

		/// <summary>
		/// Writes the end marker.
		/// This adds some extra bytes to the stream but is usually safer and the decoder doesn't have to rely on a "decompressed size" value.
		/// </summary>
		private void writeEndMarker()
		{
			// the end marker is just a match with 0xFFFFFFFF (or (uint)-1) distance.
			this.isMatch[(this.State.Value << this.properties.PB) + (this.window.totalPosition & this.PositionMask)].Encode1(this.rangeEncoder);
			this.isRep[this.State.Value].Encode0(this.rangeEncoder);
			this.State.UpdateMatch();
			this.lengthEncoder.Encode(this.rangeEncoder, this.window.totalPosition & this.PositionMask, Constants.MinMatchLength);
			this.distanceEncoder.Encode(this.rangeEncoder, Constants.MinMatchLength, 0xFFFFFFFFu);

#if VERBOSE
			System.Console.WriteLine("end");
#endif
		}

		/// <summary>
		/// Processes all remaining data and flushes the range encoder.
		/// Writes an end marker if wanted.
		/// </summary>
		public void Close()
		{
			this.Encode(true);

			// write an end marker if wanted.
			if(this.properties.WriteEndMarker)
				this.writeEndMarker();

			// flush the range encoder.
			this.rangeEncoder.Flush();
		}

		#endregion
	}
}
