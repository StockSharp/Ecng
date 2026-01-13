namespace Ecng.IO;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Provides fast CSV reading capabilities from various input sources.
/// </summary>
public class FastCsvReader : Disposable
{
	private static readonly Func<string, bool> _toBool = Converter.GetTypedConverter<string, bool>();
	private static readonly Func<string, double> _toDouble = Converter.GetTypedConverter<string, double>();

	private const int _buffSize = FileSizes.MB;
	private readonly char[] _buffer = new char[_buffSize];
	private int _bufferLen;
	private int _bufferPos;
	private char[] _line = new char[_buffSize];
	private int _lineLen;
	private RefPair<int, int>[] _columnPos = new RefPair<int, int>[_buffSize];
	private readonly char[] _lineSeparatorChars;

	private int _lineSeparatorCharPos;
	private readonly bool _disposeReader;

	/// <summary>
	/// Initializes a new instance of the <see cref="FastCsvReader"/> class using a stream, encoding, and line separator.
	/// </summary>
	/// <param name="stream">The input stream to read CSV data from.</param>
	/// <param name="encoding">The character encoding to use.</param>
	/// <param name="lineSeparator">The string that separates lines.</param>
	public FastCsvReader(Stream stream, Encoding encoding, string lineSeparator)
		: this(stream, encoding, lineSeparator, leaveOpen: true)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FastCsvReader"/> class using a stream, encoding, and line separator.
	/// </summary>
	/// <param name="stream">The input stream to read CSV data from.</param>
	/// <param name="encoding">The character encoding to use.</param>
	/// <param name="lineSeparator">The string that separates lines.</param>
	/// <param name="leaveOpen">Specifies whether the stream should remain open after the <see cref="FastCsvReader"/> is disposed.</param>
	public FastCsvReader(Stream stream, Encoding encoding, string lineSeparator, bool leaveOpen)
		: this(new StreamReader(stream ?? throw new ArgumentNullException(nameof(stream)), encoding, true, _buffSize, leaveOpen), lineSeparator, leaveOpen: false)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FastCsvReader"/> class using a string content and line separator.
	/// </summary>
	/// <param name="content">The string content to read CSV data from.</param>
	/// <param name="lineSeparator">The string that separates lines.</param>
	public FastCsvReader(string content, string lineSeparator)
		: this(new StringReader(content), lineSeparator, leaveOpen: false)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FastCsvReader"/> class using a TextReader and line separator.
	/// </summary>
	/// <param name="reader">The TextReader to read CSV data from.</param>
	/// <param name="lineSeparator">The string that separates lines.</param>
	public FastCsvReader(TextReader reader, string lineSeparator)
		: this(reader, lineSeparator, leaveOpen: true)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FastCsvReader"/> class using a TextReader and line separator.
	/// </summary>
	/// <param name="reader">The TextReader to read CSV data from.</param>
	/// <param name="lineSeparator">The string that separates lines.</param>
	/// <param name="leaveOpen">Specifies whether the reader should remain open after the <see cref="FastCsvReader"/> is disposed.</param>
	public FastCsvReader(TextReader reader, string lineSeparator, bool leaveOpen)
	{
		if (lineSeparator.IsEmpty())
			throw new ArgumentNullException(nameof(lineSeparator));

		Reader = reader ?? throw new ArgumentNullException(nameof(reader));
		_disposeReader = !leaveOpen;
		_lineSeparatorChars = [.. lineSeparator];

		for (var i = 0; i < _columnPos.Length; i++)
			_columnPos[i] = new RefPair<int, int>();
	}

	/// <summary>
	/// Gets the underlying text reader.
	/// </summary>
	public TextReader Reader { get; }

	/// <summary>
	/// Gets or sets the character used to separate columns.
	/// </summary>
	public char ColumnSeparator { get; set; } = ';';

	/// <summary>
	/// Gets the current CSV line as a string.
	/// </summary>
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

	/// <summary>
	/// Gets the number of columns in the current CSV line.
	/// </summary>
	public int ColumnCount => _columnCount;

	private int _columnCurr;

	/// <summary>
	/// Gets the current column index being processed.
	/// </summary>
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

	/// <summary>
	/// Reads the next CSV line from the underlying stream or reader.
	/// </summary>
	/// <returns><c>true</c> if a new line was successfully read; otherwise, <c>false</c>.</returns>
	[Obsolete("Use NextLineAsync(CancellationToken) instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool NextLine()
	{
		ThrowIfDisposed();

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

				while (_bufferLen < _buffer.Length)
				{
					var read = Reader.ReadBlock(_buffer, _bufferLen, _buffer.Length - _bufferLen);

					if (read == 0)
						break;

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
		else if (_lineLen > 0)
		{
			// If there are no column separators, treat the whole line as a single column
			var pair = GetColumnPos();
			pair.First = 0;
			pair.Second = _lineLen;
			_columnCount = 1;
		}

		return _lineLen > 0;
	}

	/// <summary>
	/// Asynchronously reads the next CSV line from the underlying stream or reader.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><c>true</c> if a new line was successfully read; otherwise, <c>false</c>.</returns>
	public async ValueTask<bool> NextLineAsync(CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();

		_lineLen = 0;
		_columnCount = 0;
		_columnCurr = -1;

		var inQuote = false;
		var columnStart = 0;

		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (_bufferPos >= _bufferLen)
			{
				_bufferPos = 0;
				_bufferLen = 0;

				while (_bufferLen < _buffer.Length)
				{
					cancellationToken.ThrowIfCancellationRequested();

#if NETSTANDARD2_0
					var read = await Reader.ReadBlockAsync(_buffer, _bufferLen, _buffer.Length - _bufferLen).NoWait();
#else
					var read = await Reader.ReadBlockAsync(_buffer.AsMemory(_bufferLen, _buffer.Length - _bufferLen), cancellationToken).NoWait();
#endif

					if (read == 0)
						break;

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

			if (c == '\"')
			{
				inQuote = !inQuote;

				if (inQuote && _bufferPos > 1 && _buffer[_bufferPos - 2] == '\"')
				{
					if (_lineLen >= _line.Length)
						Array.Resize(ref _line, _line.Length + _buffSize);

					_line[_lineLen++] = c;
				}

				continue;
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
		else if (_lineLen > 0)
		{
			var pair = GetColumnPos();
			pair.First = 0;
			pair.Second = _lineLen;
			_columnCount = 1;
		}

		return _lineLen > 0;
	}

	/// <summary>
	/// Skips the specified number of columns.
	/// </summary>
	/// <param name="count">The number of columns to skip. Defaults to 1.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the count is less than or equal to 0.</exception>
	/// <exception cref="ArgumentException">Thrown if skipping the specified columns would exceed the number of columns available in the current line.</exception>
	public void Skip(int count = 1)
	{
		if (count <= 0)
			throw new ArgumentOutOfRangeException(nameof(count));

		if ((_columnCurr + count) >= _columnCount)
			throw new ArgumentOutOfRangeException(nameof(count), count, "Invalid value.");

		_columnCurr += count;
	}

	/// <summary>
	/// Reads the next column as a boolean value.
	/// </summary>
	/// <returns>The boolean value read from the column.</returns>
	public bool ReadBool()
	{
		return ReadNullableBool().Value;
	}

	/// <summary>
	/// Reads the next column as a nullable boolean value.
	/// </summary>
	/// <returns>A nullable boolean value read from the column, or null if the column is empty.</returns>
	public bool? ReadNullableBool()
	{
		var str = ReadString();

		if (str is null)
			return null;

		return _toBool(str);
	}

	/// <summary>
	/// Reads the next column as an enum value of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The enum type to convert to.</typeparam>
	/// <returns>The enum value read from the column.</returns>
	public T ReadEnum<T>()
		where T : struct
	{
		return ReadNullableEnum<T>().Value;
	}

	/// <summary>
	/// Reads the next column as a nullable enum value of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The enum type to convert to.</typeparam>
	/// <returns>A nullable enum value read from the column, or null if the column is empty.</returns>
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

	/// <summary>
	/// Reads the next column as a double value.
	/// </summary>
	/// <returns>The double value read from the column.</returns>
	public double ReadDouble()
	{
		return ReadNullableDouble().Value;
	}

	/// <summary>
	/// Reads the next column as a nullable double value.
	/// </summary>
	/// <returns>A nullable double value read from the column, or null if the column is empty.</returns>
	public double? ReadNullableDouble()
	{
		var str = ReadString();

		if (str is null)
			return null;

		return _toDouble(str);
	}

	/// <summary>
	/// Reads the next column as a decimal value.
	/// </summary>
	/// <returns>The decimal value read from the column.</returns>
	public decimal ReadDecimal()
	{
		return ReadNullableDecimal().Value;
	}

	/// <summary>
	/// Reads the next column as a nullable decimal value.
	/// </summary>
	/// <returns>A nullable decimal value read from the column, or null if the column is empty.</returns>
	public decimal? ReadNullableDecimal()
	{
		var pair = GetNextColumnPos();

		if (pair.First == pair.Second)
			return null;

		long? intPart = null;
		BigInteger? intPart2 = null;
		decimal? fractalPart = null;

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

			fractalPart = (decimal)((c - '0') * 10.0.Pow(fractalPartSize - 1)) + (fractalPart ?? 0);
		}

		decimal? retVal = intPart ?? (decimal?)intPart2;

		if (fractalPart is decimal f)
		{
			if (intPart2 is BigInteger i2)
				retVal = (decimal)i2 + f / (decimal)Math.Pow(10, fractalPartSize);
			else
				retVal = (intPart ?? 0L) + f / (decimal)Math.Pow(10, fractalPartSize);
		}

		if (retVal > 0 && isNegative)
			retVal = -retVal;

		return retVal;
	}

	/// <summary>
	/// Reads the next column as an integer value.
	/// </summary>
	/// <returns>The integer value read from the column.</returns>
	public int ReadInt()
	{
		return ReadNullableInt().Value;
	}

	/// <summary>
	/// Reads the next column as a nullable integer value.
	/// </summary>
	/// <returns>A nullable integer value read from the column, or null if the column is empty.</returns>
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

	/// <summary>
	/// Reads the next column as a long value.
	/// </summary>
	/// <returns>The long value read from the column.</returns>
	public long ReadLong()
	{
		return ReadNullableLong().Value;
	}

	/// <summary>
	/// Reads the next column as a nullable long value.
	/// </summary>
	/// <returns>A nullable long value read from the column, or null if the column is empty.</returns>
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

	/// <summary>
	/// Reads the next column as a string.
	/// </summary>
	/// <returns>The string read from the column, or null if the column is empty.</returns>
	public string ReadString()
	{
		var pair = GetNextColumnPos();

		var len = pair.Second - pair.First;

		if (len == 0)
			return null;

		return new string(_line, pair.First, len);
	}

	/// <summary>
	/// Reads the next column as a DateTime value using the specified format.
	/// </summary>
	/// <param name="format">The date and time format string.</param>
	/// <returns>The DateTime value read from the column.</returns>
	public DateTime ReadDateTime([StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format)
	{
		return ReadNullableDateTime(format).Value;
	}

	/// <summary>
	/// Reads the next column as a nullable DateTime value using the specified format.
	/// </summary>
	/// <param name="format">The date and time format string.</param>
	/// <returns>A nullable DateTime value read from the column, or null if the column is empty.</returns>
	public DateTime? ReadNullableDateTime([StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format)
	{
		var str = ReadString();

		return str?.ToDateTime(format);
	}

	/// <summary>
	/// Reads the next column as a TimeSpan value using the specified format.
	/// </summary>
	/// <param name="format">The time span format string.</param>
	/// <returns>The TimeSpan value read from the column.</returns>
	public TimeSpan ReadTimeSpan([StringSyntax(StringSyntaxAttribute.TimeSpanFormat)] string format)
	{
		return ReadNullableTimeSpan(format).Value;
	}

	/// <summary>
	/// Reads the next column as a nullable TimeSpan value using the specified format.
	/// </summary>
	/// <param name="format">The time span format string.</param>
	/// <returns>A nullable TimeSpan value read from the column, or null if the column is empty.</returns>
	public TimeSpan? ReadNullableTimeSpan([StringSyntax(StringSyntaxAttribute.TimeSpanFormat)] string format)
	{
		var str = ReadString();

		return str?.ToTimeSpan(format);
	}

	private RefPair<int, int> GetNextColumnPos()
	{
		ThrowIfDisposed();

		if (_columnCurr >= _columnCount)
			throw new InvalidOperationException();

		return _columnPos[++_columnCurr];
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (_disposeReader)
			Reader.Dispose();

		base.DisposeManaged();
	}
}
