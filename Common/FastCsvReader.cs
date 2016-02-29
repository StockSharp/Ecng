namespace Ecng.Common
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;

	public class FastCsvReader
	{
		private readonly TextReader _reader;
		private const int _buffSize = 1024 * 1024;
		private readonly char[] _buffer = new char[_buffSize];
		private int _bufferLen;
		private int _bufferPos;
		private char[] _line = new char[_buffSize];
		private int _lineLen;
		private readonly RefPair<int, int>[] _columnPos = new RefPair<int, int>[_buffSize];
		private int _columnCount;
		private int _columnCurr;

		public FastCsvReader(Stream stream, Encoding encoding)
			: this(new StreamReader(stream, encoding))
		{
		}

		public FastCsvReader(string content)
			: this(new StringReader(content))
		{
		}

		public FastCsvReader(TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			_reader = reader;
			LineSeparator = Environment.NewLine;

			for (var i = 0; i < _columnPos.Length; i++)
				_columnPos[i] = new RefPair<int, int>();
		}

		private int _lineSeparatorCharPos;
		private char[] _lineSeparatorChars;
		private string _lineSeparator;

		public string LineSeparator
		{
			get { return _lineSeparator; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_lineSeparator = value;
				_lineSeparatorChars = value.ToArray();
			}
		}

		public char ColumnSeparator { get; set; } = ';';

		public string CurrentLine
		{
			get
			{
				if (_lineLen == 0)
					return null;

				return new string(_line, 0, _lineLen);
			}
		}

		public bool NextLine()
		{
			_lineLen = 0;
			_columnCount = 0;
			_columnCurr = -1;

			var inQuote = false;
			var columnStart = 0;

			while (true)
			{
				if (_bufferPos >= _bufferLen)
				{
					_bufferPos = 0;
					_bufferLen = 0;

					var left = _buffer.Length;

					while (left > 0)
					{
						var read = _reader.ReadBlock(_buffer, 0, _buffer.Length);

						if (read == 0)
							break;

						left -= read;
						_bufferLen += read;
					}

					if (_bufferLen == 0)
						break;
				}

				var c = _buffer[_bufferPos++];

				if (!inQuote)
				{
					if (c == _lineSeparatorChars[_lineSeparatorCharPos])
					{
						_lineSeparatorCharPos++;

						if (_lineSeparatorCharPos == _lineSeparatorChars.Length)
						{
							_lineSeparatorCharPos = 0;
							break;
						}

						continue;
					}
					else if (_lineSeparatorCharPos > 0)
					{
						if ((_lineLen + _lineSeparatorCharPos) >= _line.Length)
							Array.Resize(ref _line, _line.Length + _buffSize);

						Array.Copy(_lineSeparatorChars, 0, _line, _lineLen, _lineSeparatorCharPos);

						_lineLen += _lineSeparatorCharPos;
						_lineSeparatorCharPos = 0;
					}

					if (c == ColumnSeparator)
					{
						if (columnStart > _lineLen)
							throw new InvalidOperationException();

						var pair = _columnPos[_columnCount];

						pair.First = columnStart;
						pair.Second = _lineLen;

						columnStart = _lineLen + 1;

						_columnCount++;
					}
				}

				if (c == '"')
				{
					inQuote = !inQuote;

					//if (inQuote)
					continue;
					//else
					//	break;
				}

				if (_lineLen >= _line.Length)
					Array.Resize(ref _line, _line.Length + _buffSize);

				_line[_lineLen++] = c;
			}

			if (_columnCount > 0)
			{
				var pair = _columnPos[_columnCount];

				pair.First = columnStart;
				pair.Second = _lineLen;

				_columnCount++;
			}

			return _lineLen > 0;
		}

		public void Skip(int count = 1)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if ((_columnCurr + count) >= _columnCount)
				throw new ArgumentException(nameof(count));

			_columnCurr += count;
		}

		public bool ReadBool()
		{
			return ReadNullableBool().Value;
		}

		public bool? ReadNullableBool()
		{
			var str = ReadString();
			return str.To<bool?>();
		}

		public T ReadEnum<T>()
			where T : struct
		{
			return ReadNullableEnum<T>().Value;
		}

		public T? ReadNullableEnum<T>()
			where T : struct
		{
			var str = ReadString();
			return str.To<T?>();
		}

		public double ReadDouble()
		{
			return ReadNullableDouble().Value;
		}

		public double? ReadNullableDouble()
		{
			var str = ReadString();
			return str.To<double?>();
		}

		public decimal ReadDecimal()
		{
			return ReadNullableDecimal().Value;
		}

		public decimal? ReadNullableDecimal()
		{
			var pair = GetNextColumnPos();

			if (pair.First == pair.Second)
				return null;

			long? intPart = null;
			long? fractalPart = null;

			int i;

			for (i = pair.First; i < pair.Second; i++)
			{
				var c = _line[i];

				if (c == '.')
					break;

				intPart = (intPart ?? 0) * 10 + (c - '0');
			}

			var canSkipZero = true;
			var fractalPartSize = 0;

			for (var j = pair.Second - 1; j > i; j--)
			{
				var c = _line[j];

				if (c == '.')
					throw new InvalidOperationException();

				if (c == '0' && canSkipZero)
					continue;

				canSkipZero = false;
				fractalPartSize++;

				fractalPart = (c - '0') * 10.Pow(fractalPartSize - 1) + (fractalPart ?? 0);
			}

			if (fractalPart == null)
				return intPart;

			intPart = intPart ?? 0;

			return intPart.Value + (decimal)fractalPart.Value / (long)Math.Pow(10, fractalPartSize);
		}

		public int ReadInt()
		{
			return ReadNullableInt().Value;
		}

		public int? ReadNullableInt()
		{
			var pair = GetNextColumnPos();

			if (pair.First == pair.Second)
				return null;

			var retVal = 0;

			for (var i = pair.First; i < pair.Second; i++)
			{
				retVal = retVal * 10 + (_line[i] - '0');
			}

			return retVal;
		}

		public long ReadLong()
		{
			return ReadNullableLong().Value;
		}

		public long? ReadNullableLong()
		{
			var pair = GetNextColumnPos();

			if (pair.First == pair.Second)
				return null;

			var retVal = 0L;

			for (var i = pair.First; i < pair.Second; i++)
			{
				retVal = retVal * 10 + (_line[i] - '0');
			}

			return retVal;
		}

		public string ReadString()
		{
			var pair = GetNextColumnPos();

			var len = pair.Second - pair.First;

			if (len == 0)
				return null;

			return new string(_line, pair.First, len);
		}

		public DateTime ReadDateTime(string format)
		{
			return ReadNullableDateTime(format).Value;
		}

		public DateTime? ReadNullableDateTime(string format)
		{
			var str = ReadString();

			return str?.ToDateTime(format);
		}

		public TimeSpan ReadTimeSpan(string format)
		{
			return ReadNullableTimeSpan(format).Value;
		}

		public TimeSpan? ReadNullableTimeSpan(string format)
		{
			var str = ReadString();

			return str?.ToTimeSpan(format);
		}

		private RefPair<int, int> GetNextColumnPos()
		{
			if (_columnCurr >= _columnCount)
				throw new InvalidOperationException();

			return _columnPos[++_columnCurr];
		}
	}
}