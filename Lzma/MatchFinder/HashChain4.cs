namespace Lzma
{
	/// <summary>
	/// Represents a hash-chain match finder with 4 input bytes.
	/// </summary>
	internal sealed class HashChain4 : IMatchFinder
	{
		#region Constants

		private const uint hashInitialValue = 0;
		private const uint hashTableIdentity = 0xFFFFFFFFu;
		private	const int maxIterations = 1 << 12;

		#endregion

		#region Fields

		private readonly SlidingWindow window;
		private readonly uint maxDistance;
		private readonly uint minLength;

		private uint myPosition;
		private uint currentHash;
		
		private readonly uint[] hashTable;
		private readonly uint[] chain;

		#endregion

		#region Properties

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new HashChain4.
		/// </summary>
		/// <param name="window">The sliding window.</param>
		/// <param name="maxDistance">The maxmimum distance of a match.</param>
		/// <param name="minLength">The minimum allowed length of a match.</param>
		public HashChain4(SlidingWindow window, uint maxDistance, uint minLength)
		{
			this.window = window;
			this.maxDistance = maxDistance;
			this.minLength = minLength;
		
			this.myPosition = 0;
			this.currentHash = hashInitialValue;
			
			this.hashTable = new uint[1 << 24]; // 24 bit hash table.
			this.chain = new uint[this.window.buffer.Length];
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the hash chain.
		/// </summary>
		public void Initialize()
		{
			for (uint i = 0; i < this.hashTable.Length; i++)
				this.hashTable[i] = hashTableIdentity;
			for (uint i = 0; i < this.chain.Length; i++)
				this.chain[i] = hashInitialValue;
		}

		/// <summary>
		/// Moves the pointer. Hashes and inserts all bytes in between into the hash chain.
		/// </summary>
		public void Insert(uint numBytes)
		{
			while (numBytes-- > 0)
			{
				// update hash with new byte.
				this.currentHash = ((this.currentHash << 6) ^ this.window.buffer[this.myPosition]) & 0xFFFFFFu;

				// update position links.
				uint newPosition = this.myPosition >= 3 ? this.myPosition - 3u : this.window.bufferSize + this.myPosition - 3u;
				this.chain[newPosition] = this.hashTable[this.currentHash];
				this.hashTable[this.currentHash] = newPosition;

				// increment our pointer and wrap it around if we reach the end of the buffer.
				this.myPosition++;
				if (this.myPosition >= this.window.bufferSize)
					this.myPosition = 0;
			}
		}
		
		public void FindMatches(uint position, uint maxLength, MatchCallback callback)
		{
			// calculate hash of the bytes at that position.
			// we have to check whether the position wraps around at the end of the buffer though.
			int hash;
			if (position < this.window.bufferSize - 3)
				hash = (this.window.buffer[position] << 18) ^ (this.window.buffer[position + 1] << 12) ^ (this.window.buffer[position + 2] << 6) ^ (this.window.buffer[position + 3]);
			else if (position == this.window.bufferSize - 3)
				hash = (this.window.buffer[position] << 18) ^ (this.window.buffer[position + 1] << 12) ^ (this.window.buffer[position + 2] << 6) ^ (this.window.buffer[0]);
			else if (position == this.window.bufferSize - 2)
				hash = (this.window.buffer[position] << 18) ^ (this.window.buffer[position + 1] << 12) ^ (this.window.buffer[0] << 6) ^ (this.window.buffer[1]);
			else
				hash = (this.window.buffer[position] << 18) ^ (this.window.buffer[0] << 12) ^ (this.window.buffer[1] << 6) ^ (this.window.buffer[2]);

			// get "entry point".
			uint matchPos = this.hashTable[hash & 0xFFFFFFu];
			if (matchPos == hashTableIdentity) // short cut.
				return;

			uint distance = matchPos >= position ? this.window.bufferSize - (matchPos - position) - 1 : position - 1 - matchPos;

			// sometimes there's a match next to the current byte, but I think LZMA isn't capable of encoding "0" as distance, so we just go to the next match here.
			if (distance == 0)
			{
				matchPos = this.chain[matchPos];			
				distance = matchPos >= position ? this.window.bufferSize - (matchPos - position) - 1 : position - 1 - matchPos;
			}

			int iterations = 0;
			uint max = System.Math.Min(this.maxDistance, this.window.totalPosition);

			while (distance < max && iterations++ < maxIterations)
			{	
				// get the length of that match.
				uint length = this.window.GetMatchLength(position, matchPos, maxLength);
				
				// note that we can't just bail if the length is too small because hash collisions exist.
				if (length >= this.minLength)
				{
					if (!callback(distance, length))
						break;
				}
			
				// go to next position.
				uint newPos = this.chain[matchPos];
				if (newPos == hashTableIdentity)
					break;

				if (newPos >= matchPos)
					distance += this.window.bufferSize - (newPos - matchPos);
				else
					distance += matchPos - newPos;
				
				matchPos = newPos;
			}
		}

		#endregion
	}
}
