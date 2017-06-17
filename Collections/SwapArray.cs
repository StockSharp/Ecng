namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.InteropServices;

	using Ecng.Common;

	using Wintellect.PowerCollections;

	public class SwapArray<T> : CollectionBase<T>, IList<T>
		where T : struct
	{
		#region Private Fields

		//private static int _number = 0;
		private static readonly int _valueSize;

		private static long _fileOffset;
		private static readonly FileStream _swapFile;
		private static readonly BinaryWriter _writer;

		private readonly long _offset;

		#endregion

		#region SwapArray.cctor()

		static SwapArray()
		{
			_valueSize = Marshal.SizeOf(typeof(T));

			_swapFile = new FileStream("__swap", FileMode.Create);
			_writer = new BinaryWriter(_swapFile);
		}

		#endregion

		#region SwapArray.ctor()

		public SwapArray(long size)
		{
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), "Size has incorrect value '{0}'.".Put(size));

			//_swapFile = new FileStream(@"swap\__swap" + _number++, FileMode.Create);
			//_writer = new BinaryWriter(_swapFile);

			const long step = 50000000;

			var buffer = new byte[step];

			for (long i = 0; i < (size * _valueSize - step); i += step)
				_writer.Write(buffer);

			if (_swapFile.Length < (size * _valueSize + _fileOffset))
				_writer.Write(new byte[size * _valueSize + _fileOffset - _swapFile.Length]);

			_writer.Flush();

			_offset = _fileOffset;
			_fileOffset += size * _valueSize;

			if (_fileOffset != _swapFile.Length)
				throw new ArgumentException("size");

			_count = size;
		}

		#endregion

		#region CollectionBase<T> Members

		public override void Add(T item)
		{
			throw new NotSupportedException();
		}

		public override void Clear()
		{
			throw new NotSupportedException();
		}

		private readonly long _count;

		public override int Count => (int)_count;

		public override IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public override bool Remove(T item)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region IList<T> Members

		public int IndexOf(T item)
		{
			throw new NotSupportedException();
		}

		public void Insert(int index, T item)
		{
			throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		public T this[int index]
		{
			get => ReadArray(index, 1)[0];
			set => WriteArray(index, new [] { value });
		}

		#endregion

		public T[] ReadArray(long startIndex, long count)
		{
			var array = new T[count];
			var buffer = new byte[array.Length * _valueSize];

			Seek(startIndex);
			_swapFile.Read(buffer, 0, buffer.Length);

			Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
			return array;
		}

		public void WriteArray(long startIndex, T[] array)
		{
			var buffer = new byte[array.Length * _valueSize];
			Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);

			Seek(startIndex);
			_writer.Write(buffer);
		}

		private void Seek(long index)
		{
			if (index < 0 || index >= _count)
				throw new ArgumentOutOfRangeException(nameof(index), "Index has incorrect value '{0}'.".Put(index));

			var newSeekPos = index * _valueSize + _offset;

			if (newSeekPos != _swapFile.Position)
			{
				var offset = newSeekPos - _swapFile.Position;

				if ((offset + _swapFile.Position) < 0 || (offset + _swapFile.Position) > _swapFile.Length)
					throw new ArgumentOutOfRangeException(nameof(index), "Offset is incorrect for position '{0}' and index '{1}'.".Put(_swapFile.Position, index));

				_swapFile.Seek(offset, SeekOrigin.Current);

				if (_swapFile.Position < _offset || _swapFile.Position >= (_offset + _count * _valueSize))
					throw new ArgumentOutOfRangeException(nameof(index));
			}
		}
	}
}