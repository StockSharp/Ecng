using System;
using System.IO;

namespace Lzma
{
	/// <summary>
	/// Represents a decoder for LZMA streams.
	/// </summary>
	[CLSCompliant(false)]
	public sealed class Decoder
	{
		#region Fields

		// setup.
		private readonly DecoderProperties properties;
		private readonly RangeDecoder rangeDecoder;
		private readonly SlidingWindow window;

		// special decoders.
		private readonly LiteralDecoder literalDecoder;
		private readonly DistanceDecoder distanceDecoder;
		private readonly LengthDecoder lengthDecoder;
		private readonly LengthDecoder repeatedLengthDecoder;

		// probabilities of control bits.
		private readonly BitDecoder[] isMatch;
		private readonly BitDecoder[] isRep;
		private readonly BitDecoder[] isRep0;
		private readonly BitDecoder[] isRep0Long;
		private readonly BitDecoder[] isRep1;
		private readonly BitDecoder[] isRep2;

		// constants.
		private readonly uint positionMask;

		// decode loop state.
		private bool initRangeDecoder;
		private bool foundEndMarker;
		private uint rep0;
		private uint rep1;
		private uint rep2;
		private uint rep3;
		private State state;
		private uint matchLength;
		
		#endregion

		#region Properties

		/// <summary>
		/// Gets the decoder properties.
		/// </summary>
		public DecoderProperties Properties => this.properties;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new encoder that reads from the specified stream and uses the specified properties.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="properties">The decoder properties.</param>
		public Decoder(Stream stream, DecoderProperties properties)
		{
			// setup.
			this.properties = properties;
			this.rangeDecoder = new RangeDecoder(stream);
			this.window = new SlidingWindow(properties.DictionarySize);

			// special decoders.
			this.literalDecoder = new LiteralDecoder(this.properties.LP, this.properties.LC);
			this.distanceDecoder = new DistanceDecoder();
			this.lengthDecoder = new LengthDecoder(this.properties.PB);
			this.repeatedLengthDecoder = new LengthDecoder(this.properties.PB);

			// probabilities of control bits.
			this.isMatch = new BitDecoder[State.NumStates << this.properties.PB]; // was Constants.NumPosBitsMax.
			this.isRep = new BitDecoder[State.NumStates];
			this.isRep0 = new BitDecoder[State.NumStates];
			this.isRep0Long = new BitDecoder[State.NumStates << this.properties.PB]; // was Constants.NumPosBitsMax.
			this.isRep1 = new BitDecoder[State.NumStates];
			this.isRep2 = new BitDecoder[State.NumStates];

			// constants.
			this.positionMask = ((1u << this.properties.PB) - 1u);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the decoder.
		/// This resets everything to the initial state.
		/// </summary>
		public void Initialize()
		{
			// setup.
			this.initRangeDecoder = true;
			this.window.Initialize();

			// special decoders.
			this.literalDecoder.Initialize();
			this.distanceDecoder.Initialize();
			this.lengthDecoder.Initialize();
			this.repeatedLengthDecoder.Initialize();

			// probabilities of control bits.
			this.isMatch.InitializeAll();
			this.isRep.InitializeAll();
			this.isRep0.InitializeAll();
			this.isRep0Long.InitializeAll();
			this.isRep1.InitializeAll();
			this.isRep2.InitializeAll();

			// decode loop state.
			this.foundEndMarker = false;
			this.rep0 = 0;
			this.rep1 = 0;
			this.rep2 = 0;
			this.rep3 = 0;
			this.state.Value = 0;
			this.matchLength = 0;
		}

		/// <summary>
		/// Decodes a chunk from the input stream.
		/// </summary>
		/// <param name="buffer">The buffer that the decoded data will be written to.</param>
		/// <param name="offset">The offset in the data buffer.</param>
		/// <param name="count">The maximum number of bytes to decode.</param>
		/// <returns>The number of bytes that have been decoded. Note: The value returned is not always the same as "count". If the end of the stream was reached, 0 is returned.</returns>
		public uint Decode(byte[] buffer, uint offset, uint count)
		{
			// sanity checks.
			if ((long)offset + count > buffer.LongLength)
				throw new ArgumentException("Offset + count may not be greater than the length. of the buffer.");

			// initialize the range decoder if necessary.
			if (this.initRangeDecoder)
			{
				this.rangeDecoder.Initialize();
				this.initRangeDecoder = false;
			}

			if (count == 0 || this.foundEndMarker)
				return 0;

			// set new storage.
			this.window.SetOutput(buffer, offset);

			// main decoder loop.
			uint processed = 0;
			while (true)
			{
#if VERBOSE
				int repIndex = -2; // -2 = no match, -1 = not a rep match, 0-3 = repeated match.
#endif
	
				// if len is not 0, it means we have a match-copy operation from the last Decode() call which didn't fit into the output buffer.
				if (this.matchLength == 0)
				{
					// is it a literal or a match?
					while (processed < count && this.isMatch[(this.state.Value << this.properties.PB) + (this.window.totalPosition & this.positionMask)].Decode(this.rangeDecoder) == 0)
					{
						// decode the literal.
						byte previousByte = this.window.IsEmpty ? (byte)0 : this.window.ReadHistory(0);

						if (this.state.IsLiteral)
						{
							// simple literal.
#if VERBOSE
							byte b = this.literalDecoder.Decode(this.rangeDecoder, this.window.totalPosition, previousByte);
							Console.WriteLine("lit\t{0:X2}\t'{1}'", b, char.IsControl((char)b) ? ' ' : (char)b);
							this.window.WriteHistory(b);
#else
							this.window.WriteHistory(this.literalDecoder.Decode(this.rangeDecoder, this.window.totalPosition, previousByte));
#endif
						}
						else
						{
							// if the state is >= 7 there was a match before this literal.
							// the literal we are about to decode can't be the one that would have continued the match. 
							// i.e. if the string was "test123" and we decoded a match with "test", the next literal can't be "1" because then the match would simply have been longer.
							// so LZMA uses a special method which handles this case:

							byte matchByte = this.window.ReadHistory(this.rep0);

#if VERBOSE
							byte b = this.literalDecoder.DecodeDelta(this.rangeDecoder, this.window.totalPosition, previousByte, matchByte);
							Console.WriteLine("dlit\t{0:X2}\t'{1}'", b, char.IsControl((char)b) ? ' ' : (char)b);
							this.window.WriteHistory(b);
#else
							this.window.WriteHistory(this.literalDecoder.DecodeDelta(this.rangeDecoder, this.window.totalPosition, previousByte, matchByte));
#endif
						}

						this.state.UpdateLiteral();
						processed++;
					}

					// did we fill the buffer?
					if (processed == count)
						break;

#if VERBOSE
					repIndex = -1;
#endif

					// is it a repeated match?
					if (this.isRep[this.state.Value].Decode(this.rangeDecoder) != 0)
					{
						// out window may not be empty when decoding a match.
						if (this.window.IsEmpty)
							throw new InvalidDataException("Found match but output window is empty.");

						// has the distance has already been decoded?
						// if so, figure out which of the rep0 - rep3 it is.
						if (this.isRep0[this.state.Value].Decode(this.rangeDecoder) == 0)
						{
							// it's rep0.
#if VERBOSE
							repIndex = 0;
#endif

							// could also be a short rep.
							if (this.isRep0Long[(this.state.Value << this.properties.PB) + (this.window.totalPosition & this.positionMask)].Decode(this.rangeDecoder) == 0)
							{
								// short rep, copies 1 byte at rep0 + 1.
								this.state.UpdateShortRep();

#if VERBOSE
								byte b = this.window.ReadHistory(this.rep0);
								Console.WriteLine("srep\t{0:X2}\t'{1}'\trep0: {2}", b, char.IsControl((char)b) ? ' ' : (char)b, this.rep0);
								this.window.WriteHistory(b);
#else
								this.window.WriteHistory(this.window.ReadHistory(this.rep0));
#endif

								processed++;
								continue;
							}
						}
						else
						{
							uint newRep0;
							if (this.isRep1[this.state.Value].Decode(this.rangeDecoder) == 0)
							{
								// it's rep1.
								newRep0 = this.rep1;

#if VERBOSE
								repIndex = 1;
#endif
							}
							else
							{
								if (this.isRep2[this.state.Value].Decode(this.rangeDecoder) == 0)
								{
									// it's rep2.
									newRep0 = this.rep2;

#if VERBOSE
									repIndex = 2;
#endif
								}
								else
								{
									// it's rep3.
									newRep0 = this.rep3;
									this.rep3 = this.rep2;

#if VERBOSE
									repIndex = 3;
#endif
								}

								this.rep2 = this.rep1;
							}

							this.rep1 = this.rep0;
							this.rep0 = newRep0;
						}

						// decode length of match.
						this.matchLength = this.repeatedLengthDecoder.Decode(this.rangeDecoder, this.window.totalPosition & this.positionMask);
						this.state.UpdateRep();
					}
					else
					{
						// distance isn't cached, decode it.
						// first, decode length.
						this.matchLength = this.lengthDecoder.Decode(this.rangeDecoder, this.window.totalPosition & this.positionMask);

						this.state.UpdateMatch();

						// update repeat distances and decode distance.
						this.rep3 = this.rep2;
						this.rep2 = this.rep1;
						this.rep1 = this.rep0;
						this.rep0 = this.distanceDecoder.Decode(this.rangeDecoder, this.matchLength);
						
						// rep0 == 0xFFFFFFFF is the end-of-stream marker.
						if (this.rep0 == uint.MaxValue)
						{
							// but only if the range decoder is finished as well.
							if (this.rangeDecoder.Finished)
							{
#if VERBOSE
								Console.WriteLine("end");
#endif
								this.foundEndMarker = true;
								break;
							}

							throw new InvalidDataException("Found potential end marker, but the range coder is not finished.");
						}

						// is rep0 a valid distance?
						if (!this.window.CheckDistance(this.rep0))
							throw new InvalidDataException("Invalid match: Distance is out of bounds.");
					}
				}

				// copy a match.
				while (processed < count)
				{
#if VERBOSE
					byte b = this.window.ReadHistory(this.rep0);
					this.window.WriteHistory(b);

					if (repIndex != -2)
					{
						Console.WriteLine("copy\t{0:X2}\t'{1}'\t{2}: {3}, {4}", b, char.IsControl((char)b) ? ' ' : (char)b, repIndex >= 0 ? "rep" + repIndex : "match", this.rep0, this.matchLength);
						repIndex = -2;
					}
					else
					{
						Console.WriteLine("copy\t{0:X2}\t'{1}'", b, char.IsControl((char)b) ? ' ' : (char)b);
					}
#else
					this.window.WriteHistory(this.window.ReadHistory(this.rep0));
#endif

					processed++;
					if (--this.matchLength == 0)
						break;
				}

				// did we fill the buffer?
				if (processed == count)
					break;
			}

			return processed;
		}

		/// <summary>
		/// Restarts the range decoder. Must be called when the encoder was aligned.
		/// </summary>
		public void Align()
		{
			if (!this.rangeDecoder.Finished)
				throw new InvalidOperationException("Range decoder is not finished.");
			this.initRangeDecoder = true;
		}

		#endregion
	}
}
