namespace Ecng.Common
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Numerics;
	using System.Text;

	public class FastCsvReader
	{
		private static readonly Func<string, bool> _toBool = Converter.GetTypedConverter<string, bool>();
		private static readonly Func<string, double> _toDouble = Converter.GetTypedConverter<string, double>();

		private const int _buffSize = 1024 * 1024;
		private readonly char[] _buffer = new char[_buffSize];
		private int _bufferLen;
		private int _bufferPos;
		private char[] _line = new char[_buffSize];
		private int _lineLen;
		private RefPair<int, int>[] _columnPos = new RefPair<int, int>[_buffSize];

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

			Reader = reader;
			LineSeparator = Environment.NewLine;

			for (var i = 0; i < _columnPos.Length; i++)
				_columnPos[i] = new RefPair<int, int>();
		}

		public TextReader Reader { get; }

		private int _lineSeparatorCharPos;
		private char[] _lineSeparatorChars;
		private string _lineSeparator;

		public string LineSeparator
		{
			get => _lineSeparator;
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

		private int _columnCount;

		public int ColumnCount => _columnCount;

		private int _columnCurr;

		public int ColumnCurr => _columnCurr;

		private RefPair<int, int> GetColumnPos()
		{
			var prevLen = _columnPos.Length;

			if (prevLen <= _columnCount)
			{
				Array.Resize(ref _columnPos, prevLen + _buffSize);

				for (var i = prevLen; i < prevLen + _buffSize; i++)
				{
					_columnPos[i] = new RefPair<int, int>();
				}
			}

			return _columnPos[_columnCount];
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
						var read = Reader.ReadBlock(_buffer, 0, _buffer.Length);

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

						var pair = GetColumnPos();

						pair.First = columnStart;
						pair.Second = _lineLen;

						columnStart = _lineLen + 1;

						_columnCount++;
					}
				}

				if (c == '"')
				{
					inQuote = !inQuote;

					if (inQuote && _bufferPos > 1 && _buffer[_bufferPos - 2] == '"')
					{
						if (_lineLen >= _line.Length)
							Array.Resize(ref _line, _line.Length + _buffSize);

						_line[_lineLen++] = c;
					}

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
				var pair = GetColumnPos();

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

			if (str == null)
				return null;

			return _toBool(str);
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

			if (str.IsEmpty())
				return null;

			if (int.TryParse(str, out var num))
				return num.To<T>();

			return str.To<T>();
		}

		public double ReadDouble()
		{
			return ReadNullableDouble().Value;
		}

		public double? ReadNullableDouble()
		{
			var str = ReadString();

			if (str == null)
				return null;

			return _toDouble(str);
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
			BigInteger? intPart2 = null;
			long? fractalPart = null;

			var isNegative = false;

			int i;

			for (i = pair.First; i < pair.Second; i++)
			{
				var c = _line[i];

				if (c == '.')
					break;

				if (c == '+')
				{
					if (i != pair.First)
						throw new InvalidOperationException("+");

					continue;
				}

				if (c == '-')
				{
					if (i != pair.First)
						throw new InvalidOperationException("-");

					isNegative = true;
					continue;
				}

				if (intPart2 is null)
				{
					intPart = (intPart ?? 0) * 10 + (c - '0');

					if (intPart >= long.MaxValue / 10)
					{
						intPart2 = intPart;
						intPart = null;
					}
				}
				else
					intPart2 = intPart2.Value * 10 + (c - '0');
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

			decimal? retVal = intPart ?? (decimal?)intPart2;

			if (fractalPart is object)
			{
				if (intPart2 is object)
					retVal = (decimal)intPart2.Value + (decimal)fractalPart.Value / (long)Math.Pow(10, fractalPartSize);
				else
					retVal = (intPart ?? 0L) + (decimal)fractalPart.Value / (long)Math.Pow(10, fractalPartSize);
			}

			if (retVal > 0 && isNegative)
				retVal = -retVal;

			return retVal;
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

			var isNegative = false;

			var retVal = 0;

			for (var i = pair.First; i < pair.Second; i++)
			{
				var c = _line[i];

				if (c == '+')
				{
					if (i != pair.First)
						throw new InvalidOperationException("+");

					continue;
				}

				if (c == '-')
				{
					if (i != pair.First)
						throw new InvalidOperationException("-");

					isNegative = true;
					continue;
				}

				retVal = retVal * 10 + (c - '0');
			}

			if (retVal > 0 && isNegative)
				retVal = -retVal;

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

			var isNegative = false;

			var retVal = 0L;

			for (var i = pair.First; i < pair.Second; i++)
			{
				var c = _line[i];

				if (c == '+')
				{
					if (i != pair.First)
						throw new InvalidOperationException("+");

					continue;
				}

				if (c == '-')
				{
					if (i != pair.First)
						throw new InvalidOperationException("-");

					isNegative = true;
					continue;
				}

				retVal = retVal * 10 + (c - '0');
			}

			if (retVal > 0 && isNegative)
				retVal = -retVal;

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