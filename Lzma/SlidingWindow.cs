using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace Lzma
{
	/// <summary>
	/// Represents an LZ sliding window containing a "backlog" of processed bytes (a.k.a the dictionary) 
	/// as well as a working buffer (ahead of the processing position) for encoding.
	/// </summary>
	/// <remarks>
	/// The buffer is organized like this:
	/// 
	///    history     working buffer
	/// [...........x..................y]
	/// 
	/// x: history position.
	/// y: working position.
	/// 
	/// The working buffer is used by the encoder to write data to the window that is about to be encoded.
	/// This data is not yet part of the history (i.e. dictionary).
	/// The working position is always equal to or greater than the history position (in terms of a cyclic buffer that is).
	/// The history position will be moved once the data is processed.
	/// </remarks>
	public sealed class SlidingWindow
	{
		#region Fields

		internal readonly byte[] buffer;
		internal readonly uint bufferSize;
		
		internal uint workingPosition;
		internal uint historyPosition;
		internal uint currentWorkingSize;
		private bool isFull;
		internal uint totalPosition;

		private byte[] outputBuffer;
		private long outputPosition;
		
		#endregion

		#region Properties
		
		/// <summary>
		/// Gets the underlying buffer.
		/// </summary>
		public byte[] Buffer => this.buffer;

		/// <summary>
		/// Gets a value indicating whether the window is empty, i.e. no bytes have been output using PutByte.
		/// </summary>
		public bool IsEmpty => this.historyPosition == 0 && !this.isFull;

		/// <summary>
		/// Gets a value indicating whether the dictionary buffer is full.
		/// </summary>
		public bool IsFull => this.isFull;

		/// <summary>
		/// Gets the current position in the buffer (i.e. where the next byte is written).
		/// </summary>
		public uint WorkingPosition => this.workingPosition;

		/// <summary>
		/// Gets the position at which the history part of the buffer ends.
		/// </summary>
		public uint HistoryPosition => this.historyPosition;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new sliding window with the specified size.
		/// </summary>
		/// <param name="size">The size of the cyclic buffer.</param>
		public SlidingWindow(uint size)
		{
			this.buffer = new byte[size];
			this.bufferSize = size;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the window.
		/// </summary>
		public void Initialize()
		{
			this.workingPosition = 0;
			this.historyPosition = 0;
			this.currentWorkingSize = 0;
			this.isFull = false;

			this.outputBuffer = null;
			this.outputPosition = 0;

			this.totalPosition = 0;
		}

		/// <summary>
		/// Sets the output buffer and offset where new bytes are stored.
		/// </summary>
		/// <param name="outBuffer"></param>
		/// <param name="offset"></param>
		public void SetOutput(byte[] outBuffer, long offset)
		{
			this.outputBuffer = outBuffer;
			this.outputPosition = offset;
		}

		#region History Buffer
		
		/// <summary>
		/// Gets a byte from the history buffer.
		/// </summary>
		/// <param name="index">The distance of the byte. 0 is the last byte written to the buffer.</param>
		/// <returns></returns>
		public byte ReadHistory(uint index)
		{
#if DEBUG
			if (this.IsEmpty)
				throw new System.InvalidOperationException("No data has been written to the history buffer.");

			if (this.isFull ? index >= this.bufferSize : index >= this.historyPosition)
				throw new System.ArgumentOutOfRangeException(nameof(index));
#endif
			++index;
			if (index <= this.historyPosition)
				return this.buffer[this.historyPosition - index];

			return this.buffer[this.bufferSize - index + this.historyPosition];
		}

		/// <summary>
		/// Writes one byte to the history buffer and the output buffer.
		/// </summary>
		/// <param name="b">The byte to write.</param>
		public void WriteHistory(byte b)
		{
			if (this.currentWorkingSize > 0)
				--this.currentWorkingSize;
			else
				++this.workingPosition;

			// write to cyclic buffer.
			this.buffer[this.historyPosition] = b;
			if (++this.historyPosition == this.buffer.Length)
			{
				this.historyPosition = 0;
				this.isFull = true;
			}

			if (this.outputBuffer != null)
			{
				// write to output.
				this.outputBuffer[this.outputPosition] = b;
				++this.outputPosition;
			}

			++this.totalPosition;
		}

		/// <summary>
		/// Writes the specified byte array to the history buffer and the output buffer.
		/// </summary>
		/// <param name="bytes">The byte array.</param>
		/// <param name="offset">An offset in the byte array.</param>
		/// <param name="count">The number of bytes to write.</param>
		public void WriteHistory(byte[] bytes, uint offset, uint count)
		{
			for (uint i = 0; i < count; i++)
				this.WriteHistory(bytes[offset + i]);
		}

		/// <summary>
		/// Copies a match.
		/// </summary>
		/// <param name="dist">The distance to the first byte.</param>
		/// <param name="len">The length of the match.</param>
		public void CopyMatch(uint dist, uint len)
		{
			while (len-- > 0)
				this.WriteHistory(this.ReadHistory(dist));
		}

		/// <summary>
		/// Checks if the specified distance is valid.
		/// </summary>
		/// <param name="dist">The distance to check.</param>
		/// <returns>True, if it is a valid distance, false otherwise.</returns>
		public bool CheckDistance(uint dist)
		{
			return (dist <= this.historyPosition || this.isFull) && !(dist >= this.bufferSize);
		}

		#endregion

		#region Working Buffer

		/// <summary>
		/// Gets a byte from the working buffer.
		/// </summary>
		/// <param name="index">The index of the byte. 0 is the first byte that was written to the working buffer.</param>
		/// <returns></returns>
		public byte ReadWorking(uint index)
		{
#if DEBUG
			if (index >= this.currentWorkingSize)
				throw new System.ArgumentOutOfRangeException(nameof(index));
#endif

			long pos = (this.historyPosition + index);
			return this.buffer[pos > this.bufferSize ? pos - this.bufferSize : pos];
		}

		/// <summary>
		/// Writes the specified byte array only to the working buffer (not the output buffer), incrementing the working buffer size.
		/// </summary>
		/// <param name="bytes">The byte array.</param>
		/// <param name="offset">An offset in the byte array.</param>
		/// <param name="count">The number of bytes to write.</param>
		/// <remarks>Use the <see cref="ProcessWorking">ProcessWorking</see> method to write bytes from the working buffer to the output buffer and decrementing the working buffer size.</remarks>
		public void WriteWorking(byte[] bytes, uint offset, uint count)
		{
			if (this.workingPosition + count >= this.buffer.Length)
			{
				// split up because of wrap-around.
				long splitCount1 = this.buffer.Length - 1 - this.workingPosition;
				long splitCount2 = count - splitCount1;

				Array.Copy(bytes, offset, this.buffer, this.workingPosition, splitCount1);
				Array.Copy(bytes, offset + splitCount1, this.buffer, 0, splitCount2);

				this.workingPosition = (uint)splitCount2;
			}
			else
			{
				Array.Copy(bytes, offset, this.buffer, this.workingPosition, count);
				this.workingPosition += count;
			}

			this.currentWorkingSize += count;
		}
		
		/// <summary>
		/// Processed the specified number of bytes in the working buffer. The bytes will be written to the history buffer.
		/// </summary>
		/// <param name="numBytes">The number of bytes to move.</param>
		public void ProcessWorking(uint numBytes)
		{
#if DEBUG
			if (numBytes > this.currentWorkingSize)
				throw new System.ArgumentException("numBytes is greater than working buffer size.");
#endif

			if (this.historyPosition + numBytes >= this.bufferSize)
			{
				numBytes -= this.bufferSize - this.historyPosition;
				this.WriteHistory(this.buffer, this.historyPosition, this.bufferSize - this.historyPosition);
				this.WriteHistory(this.buffer, 0, numBytes);
			}
			else
			{
				this.WriteHistory(this.buffer, this.historyPosition, numBytes);
			}
		}

		#endregion

		#region Misc
		
		/// <summary>
		/// Gets the length of a match.
		/// </summary>
		/// <param name="originalPos">The start position of the original string.</param>
		/// <param name="matchPos">The start position of the match.</param>
		/// <param name="maxLength">The maximum number of bytes.</param>
		/// <returns>The number of matching bytes.</returns>
		public uint GetMatchLength(uint originalPos, uint matchPos, uint maxLength)
		{
			uint len = maxLength;
			uint limit = this.bufferSize - maxLength;

			// do some testing whether the positions will wrap around.
			// since this is pretty unlikely for large buffers it improves performance quite a bit because we can leave out the out-of-bounds checks.
			if (originalPos < limit)
			{
				if (matchPos < limit)
				{
					while(len > 0 && this.buffer[originalPos] == this.buffer[matchPos])
					{
						++originalPos;
						++matchPos;
						--len;
					}
				}
				else
				{
					while(len > 0 && this.buffer[originalPos] == this.buffer[matchPos])
					{
						++originalPos;
						++matchPos;
						--len;

						if (matchPos >= this.bufferSize)
							matchPos = 0;
					}
				}
			}
			else
			{
				if (matchPos < limit)
				{
					while(len > 0 && this.buffer[originalPos] == this.buffer[matchPos])
					{
						++originalPos;
						++matchPos;
						--len;

						if (originalPos >= this.bufferSize)
							originalPos = 0;
					}
				}
				else
				{
					while(len > 0 && this.buffer[originalPos] == this.buffer[matchPos])
					{
						++originalPos;
						++matchPos;
						--len;

						if (originalPos >= this.bufferSize)
							originalPos = 0;

						if (matchPos >= this.bufferSize)
							matchPos = 0;
					}
				}
			}

			return maxLength - len;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetDistance(uint originalPos, uint matchPos)
		{
			return matchPos >= originalPos ? this.bufferSize - (matchPos - originalPos) - 1 : originalPos - 1 - matchPos;
		}

		#endregion
		
		#endregion
	}
}
