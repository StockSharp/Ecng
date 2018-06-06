namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;

	public class AllocationMemory
	{
		private readonly List<byte[]> _allocationBuffers = new List<byte[]>();
		private readonly int _bufferSize;

		public AllocationMemory(int bufferSize)
		{
			if (bufferSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(bufferSize));

			_bufferSize = bufferSize;
		}

		public byte[] Allocate()
		{
			if (_allocationBuffers.Count == 0)
			{
				for (var i = 0; i < 10; i++)
				{
					_allocationBuffers.Add(new byte[_bufferSize]);
				}
			}

			var last = _allocationBuffers.Count - 1;

			var buffer = _allocationBuffers[last];
			_allocationBuffers.RemoveAt(last);

			return buffer;
		}

		public void Free(byte[] buffer)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			_allocationBuffers.Add(buffer);
		}

		public byte[][] Allocate(int count)
		{
			if (_allocationBuffers.Count < count)
			{
				var need = count - _allocationBuffers.Count;

				for (var i = 0; i < need; i++)
				{
					_allocationBuffers.Add(new byte[_bufferSize]);
				}
			}

			var buffers = new byte[count][];

			_allocationBuffers.CopyTo(_allocationBuffers.Count - count, buffers, 0, count);
			_allocationBuffers.RemoveRange(_allocationBuffers.Count - count, count);

			return buffers;
		}

		public void Free(byte[][] restoreBodies)
		{
			_allocationBuffers.AddRange(restoreBodies);
		}
	}
}