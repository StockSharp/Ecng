using System;
using System.IO;

namespace Lzma
{
	/// <summary>
	/// Represents a fast LZMA encoder.
	/// </summary>
	public class FastEncoder : Encoder
	{
		#region Fields

		private readonly HashChain3 matchFinder;
		private Match mainMatch;
		private Match nextMatch;

		#endregion

		#region Properties

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new FastEncoder.
		/// </summary>
		public FastEncoder(Stream stream, EncoderProperties properties)
			: base(stream, properties)
		{
			this.matchFinder = new HashChain3(this.Window, this.Properties.DictionarySize - 1, Constants.MinMatchLength);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the fast encoder.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();
			this.matchFinder.Initialize();
		}

		/// <summary>
		/// Processes the specified number of bytes.
		/// </summary>
		/// <param name="numBytes"></param>
		protected override void Process(uint numBytes)
		{
			base.Process(numBytes);
			this.matchFinder.Insert(numBytes);

			if(numBytes == 1)
				this.mainMatch = this.nextMatch; // if only one byte has been processed, we can use the next match as new main match.
			else
				this.findMatchFast(0, out this.mainMatch); // we need to find a new match.

			// if we have bytes left in the window, search for a new match.
			if (this.Window.currentWorkingSize > 0)
				this.findMatchFast(1, out this.nextMatch);
			else
				this.nextMatch = default(Match);

			if (this.mainMatch.Length > 0)
			{
				// if there's a better match after the current match, skip our main match and use that instead.
				if ((this.mainMatch.Length > 3 && this.nextMatch.Length >= this.mainMatch.Length - 1 && this.nextMatch.Distance < this.mainMatch.Distance >> 7) ||
				    (this.nextMatch.Length >= this.mainMatch.Length && this.nextMatch.Distance < this.mainMatch.Distance) ||
				    (this.nextMatch.Length >= this.mainMatch.Length + 1 && this.nextMatch.Distance >> 7 <= this.mainMatch.Distance) ||
				    (this.nextMatch.Length > this.mainMatch.Length + 1))
					this.mainMatch.Length = 0;
			}
		}

		private void findMatchFast(uint offset, out Match match)
		{
			uint position = this.Window.historyPosition + offset;
			if (position == this.Window.bufferSize)
				position = 0; 
			
			uint maxLength = Math.Min(Constants.LengthMax + Constants.MinMatchLength - 1, this.Window.currentWorkingSize - offset);

			uint currentDistance = 0;
			uint currentLength = 0;
			uint numMatches = 0;

			// find the longest match.
			this.matchFinder.FindMatches(position, maxLength, ((distance, length) =>
			{
				if (length > currentLength)
				{
					currentDistance = distance;
					currentLength = length;
				}
					
				// simple heuristic: if the match is relatively long and the distance is a repeated distance, just use that match.
				if (length > 64 && (distance == this.Rep0 || distance == this.Rep1 || distance == this.Rep2 || distance == this.Rep3))
				{
					currentDistance = distance;
					currentLength = length;
					return false;
				}

				return numMatches++ < 256;
			}));

			match.Length = currentLength;
			match.Distance = currentDistance;
		}

		/// <summary>
		/// Finds a match for the current position.
		/// </summary>
		/// <param name="match"></param>
		/// <returns></returns>
		protected override void FindMatch(out Match match)
		{
			match = this.mainMatch;
		}
		
		#endregion
	}
}
