namespace Lzma
{
	/// <summary>
	/// Represents a hash-chain match finder with 2 input bytes.
	/// </summary>
	internal sealed class HashChain2 : IMatchFinder
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
		/// Creates a new HashChain2.
		/// </summary>
		/// <param name="window">The sliding window.</param>
		/// <param name="maxDistance">The maxmimum distance of a match.</param>
		/// <param name="minLength">The minimum allowed length of a match.</param>
		public HashChain2(SlidingWindow window, uint maxDistance, uint minLength)
		{
			this.window = window;
			this.maxDistance = maxDistance;
			this.minLength = minLength;

			this.myPosition = 0;
			this.currentHash = hashInitialValue;
			
			this.hashTable = new uint[1 << 16]; // 16 bit hash table.
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
				this.currentHash = ((this.currentHash << 8) ^ this.window.buffer[this.myPosition]) & 0xFFFFu;

				// update position links.
				uint newPosition = this.myPosition == 0 ? this.window.bufferSize - 1u : this.myPosition - 1u;
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
			if (position < this.window.bufferSize - 1)
				hash = (this.window.buffer[position] << 8) ^ (this.window.buffer[position + 1]);
			else
				hash = (this.window.buffer[position] << 8) ^ (this.window.buffer[0]);

			// get "entry point".
			uint matchPos = this.hashTable[hash];
			if (matchPos == hashTableIdentity) // short cut.
				return;

			int iterations = 0;

			uint length = this.window.GetMatchLength(position, matchPos, maxLength);
			if (length < 2)
				return;
			
			uint distance = matchPos >= position ? this.window.bufferSize - (matchPos - position) - 1 : position - 1 - matchPos;
			uint maxDistance = System.Math.Min(this.maxDistance, this.window.totalPosition);
	
			while (distance < maxDistance && iterations++ < maxIterations)
			{
				if (length >= this.minLength && distance > 0)
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

				// get the length of the new match.
				length = this.window.GetMatchLength(position, newPos, maxLength);

				// matches must be at least 2 bytes long because the hash is unique.
				if (length < 2)
				{
					// if it isn't, it's an outdated entry that we should remove.
					this.chain[matchPos] = hashTableIdentity;
					break;
				}

				matchPos = newPos;
			}
		}

		#endregion
	}
}
