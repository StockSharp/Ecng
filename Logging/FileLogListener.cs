namespace Ecng.Logging;

using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

using Ecng.Localization;

/// <summary>
/// Modes of log files splitting by date.
/// </summary>
public enum SeparateByDateModes
{
	/// <summary>
	/// Do not split. The splitting is off.
	/// </summary>
	None,

	/// <summary>
	/// To split by adding to the file name.
	/// </summary>
	FileName,

	/// <summary>
	/// To split via subdirectories.
	/// </summary>
	SubDirectories,
}

/// <summary>
/// Policies of action for out of date logs.
/// </summary>
public enum FileLogHistoryPolicies
{
	/// <summary>
	/// Do nothing.
	/// </summary>
	None,

	/// <summary>
	/// Delete after <see cref="FileLogListener.HistoryAfter"/>.
	/// </summary>
	Delete,

	/// <summary>
	/// Compression after <see cref="FileLogListener.HistoryAfter"/>.
	/// </summary>
	Compression,

	/// <summary>
	/// Move to <see cref="FileLogListener.HistoryMove"/> after <see cref="FileLogListener.HistoryAfter"/>.
	/// </summary>
	Move,
}

/// <summary>
/// The logger recording the data to a text file.
/// </summary>
public class FileLogListener : LogListener
{
	private const int _maxHistoryDaysToScan = 365;
	private static readonly int _maxDateChars = "yyyy/MM/dd".Length;	// 10
	private static readonly int _maxTimeChars = "HH:mm:ss.fff".Length;	// 12

	private static readonly char[] _digitChars;

	static FileLogListener()
	{
		_digitChars = new char[10];

		for (var i = 0; i < 10; i++)
			_digitChars[i] = (char)(i + '0');
	}

	private class StreamWriterEx(string path, bool append, Encoding encoding) : StreamWriter(path, append, encoding)
	{
		public string Path { get; } = path;
	}

	private readonly SynchronizedPairSet<(string, DateTime), StreamWriterEx> _writers = [];

	/// <summary>
	/// To create <see cref="FileLogListener"/>. For each <see cref="ILogSource"/> a separate file with a name equal to <see cref="ILogSource.Name"/> will be created.
	/// </summary>
	public FileLogListener()
	{
	}

	/// <summary>
	/// To create <see cref="FileLogListener"/>. All messages from the <see cref="ILogSource.Log"/> will be recorded to the file <paramref name="fileName" />.
	/// </summary>
	/// <param name="fileName">The name of a text file to which messages from the event <see cref="ILogSource.Log"/> will be recorded.</param>
	public FileLogListener(string fileName)
	{
		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		var info = new FileInfo(fileName);

		if (info.Name.IsEmpty())
			throw new ArgumentException(fileName, nameof(fileName));

		FileName = Path.GetFileNameWithoutExtension(info.Name);

		if (!info.Extension.IsEmpty())
			Extension = info.Extension;

		if (!info.DirectoryName.IsEmpty())
			LogDirectory = info.DirectoryName;
	}

	private string _fileName;

	/// <summary>
	/// The name of a text file (without filename extension) to which messages from the event <see cref="ILogSource.Log"/> will be recorded.
	/// </summary>
	public string FileName
	{
		get => _fileName;
		set => _fileName = value.IsEmpty() ? null : value;
	}

	private Encoding _encoding = Encoding.UTF8;

	/// <summary>
	/// File encoding. The default is UTF-8 encoding.
	/// </summary>
	public Encoding Encoding
	{
		get => _encoding;
		set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
	}

	private long _maxLength;

	/// <summary>
	/// The maximum length of the log file. The default is 0, which means that the file will have unlimited size.
	/// </summary>
	public long MaxLength
	{
		get => _maxLength;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

			_maxLength = value;
		}
	}

	private int _maxCount;

	/// <summary>
	/// The maximum number of rolling files. The default is 0, which means that the files will be rolled without limitation.
	/// </summary>
	public int MaxCount
	{
		get => _maxCount;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

			_maxCount = value;
		}
	}

	/// <summary>
	/// Whether to add the data to a file, if it already exists. The default is off.
	/// </summary>
	public bool Append { get; set; }

	private string _logDirectory = Directory.GetCurrentDirectory();

	/// <summary>
	/// The directory where the log file will be created. By default, it is the directory where the executable file is located.
	/// </summary>
	/// <remarks>
	/// If the directory does not exist, it will be created.
	/// </remarks>
	public string LogDirectory
	{
		get => _logDirectory;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			Directory.CreateDirectory(value);

			_logDirectory = value;
		}
	}

	/// <summary>
	/// To record the subsidiary sources data to the parent file. The default mode is enabled.
	/// </summary>
	public bool WriteChildDataToRootFile { get; set; } = true;

	private string _extension = ".txt";

	/// <summary>
	/// Extension of log files. The default value is '.txt'.
	/// </summary>
	public string Extension
	{
		get => _extension;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_extension = value;
		}
	}

	/// <summary>
	/// To output the source identifier <see cref="ILogSource.Id"/> to a file. The default is off.
	/// </summary>
	public bool WriteSourceId { get; set; }

	private string _directoryDateFormat = "yyyy_MM_dd";

	/// <summary>
	/// The directory name format that represents a date. By default is 'yyyy_MM_dd'.
	/// </summary>
	public string DirectoryDateFormat
	{
		get => _directoryDateFormat;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_directoryDateFormat = value;
		}
	}

	/// <summary>
	/// The mode of log files splitting by date. The default mode is <see cref="SeparateByDateModes.None"/>.
	/// </summary>
	public SeparateByDateModes SeparateByDates { get; set; }

	/// <summary>
	/// <see cref="FileLogHistoryPolicies"/>. By default is <see cref="FileLogHistoryPolicies.None"/>.
	/// </summary>
	public FileLogHistoryPolicies HistoryPolicy { get; set; } = FileLogHistoryPolicies.None;

	private TimeSpan _historyAfter = TimeSpan.FromDays(7);

	/// <summary>
	/// Offset from present day indicates are logs are out of date.
	/// </summary>
	public TimeSpan HistoryAfter
	{
		get => _historyAfter;
		set
		{
			if (value < TimeSpan.FromDays(1))
				throw new ArgumentOutOfRangeException(nameof(value));

			_historyAfter = value;
		}
	}

	/// <summary>
	/// Uses in case of <see cref="FileLogHistoryPolicies.Move"/>. Default is <see langword="null"/>.
	/// </summary>
	public string HistoryMove { get; set; }

	/// <summary>
	/// Uses in case of <see cref="FileLogHistoryPolicies.Compression"/>. Default is <see cref="CompressionLevel.Optimal"/>.
	/// </summary>
	public CompressionLevel HistoryCompressionLevel { get; set; } = CompressionLevel.Optimal;

	private string GetFileName(string sourceName, DateTime date)
	{
		var invalidChars = sourceName.Intersect(Path.GetInvalidFileNameChars()).ToArray();

		if (invalidChars.Any())
		{
			var sb = new StringBuilder(sourceName);

			foreach (var invalidChar in invalidChars)
				sb.Replace(invalidChar, '_');

			sourceName = sb.ToString();
		}

		var fileName = sourceName + Extension;
		var dirName = LogDirectory;

		switch (SeparateByDates)
		{
			case SeparateByDateModes.None:
				break;
			case SeparateByDateModes.FileName:
				fileName = date.ToString(DirectoryDateFormat) + "_" + fileName;
				break;
			case SeparateByDateModes.SubDirectories:
				dirName = Path.Combine(dirName, date.ToString(DirectoryDateFormat));
				Directory.CreateDirectory(dirName);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(SeparateByDates), SeparateByDates, "Invalid value.".Localize());
		}

		fileName = Path.Combine(dirName, fileName);
		return fileName;
	}

	/// <summary>
	/// To create a text writer.
	/// </summary>
	/// <param name="fileName">The name of the text file to which messages from the event <see cref="ILogSource.Log"/> will be recorded.</param>
	/// <returns>A text writer.</returns>
	private StreamWriterEx OnCreateWriter(string fileName)
	{
		return new StreamWriterEx(fileName, Append, Encoding);
	}

	private bool _triedHistoryPolicy;

	private void TryDoHistoryPolicy()
	{
		bool isDir;

		switch (SeparateByDates)
		{
			case SeparateByDateModes.None:
				return;
			case SeparateByDateModes.FileName:
				isDir = false;
				break;
			case SeparateByDateModes.SubDirectories:
				isDir = true;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(SeparateByDates), SeparateByDates, "Invalid value.".Localize());
		}

		var policy = HistoryPolicy;

		switch (policy)
		{
			case FileLogHistoryPolicies.None:
				return;
			case FileLogHistoryPolicies.Delete:
			case FileLogHistoryPolicies.Compression:
				break;
			case FileLogHistoryPolicies.Move:
			{
				if (HistoryMove.IsEmpty())
					throw new InvalidOperationException("HistoryMove is null.");

				Directory.CreateDirectory(HistoryMove);

				break;
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(HistoryPolicy), policy, "Invalid value.".Localize());
		}

		var files = CollectHistoryFiles(isDir);

		if (files.Count == 0)
			return;

		ApplyHistoryPolicy(policy, files, isDir);
	}

	private List<string> CollectHistoryFiles(bool isDir)
	{
		var files = new List<string>();

		var start = DateTime.Today - HistoryAfter;

		for (var i = 0; i < _maxHistoryDaysToScan; i++)
		{
			var dateStr = (start - TimeSpan.FromDays(i)).ToString(DirectoryDateFormat);

			if (isDir)
			{
				var dirName = Path.Combine(LogDirectory, dateStr);

				if (Directory.Exists(dirName))
					files.Add(dirName);
			}
			else
				files.AddRange(Directory.GetFiles(LogDirectory, $"{dateStr}_*{Extension}"));
		}

		return files;
	}

	private void ApplyHistoryPolicy(FileLogHistoryPolicies policy, List<string> files, bool isDir)
	{
		switch (policy)
		{
			case FileLogHistoryPolicies.Delete:
				DeleteHistoryFiles(files, isDir);
				break;
			case FileLogHistoryPolicies.Compression:
				CompressHistoryFiles(files, isDir);
				break;
			case FileLogHistoryPolicies.Move:
				MoveHistoryFiles(files, isDir);
				break;
		}
	}

	private static void DeleteHistoryFiles(List<string> files, bool isDir)
	{
		foreach (var file in files)
		{
			if (isDir)
				Directory.Delete(file, true);
			else
				File.Delete(file);
		}
	}

	private void CompressHistoryFiles(List<string> files, bool isDir)
	{
		foreach (var file in files)
		{
			if (isDir)
			{
				ZipFile.CreateFromDirectory(file, Path.Combine(LogDirectory, $"{Path.GetFileName(file)}.zip"), HistoryCompressionLevel, false);
				Directory.Delete(file, true);
			}
			else
			{
				using (var zipFile = new ZipArchive(File.Open(Path.Combine(LogDirectory, $"{Path.GetFileNameWithoutExtension(file)}.zip"), FileMode.Create), ZipArchiveMode.Create))
					zipFile.CreateEntryFromFile(file, Path.GetFileName(file), HistoryCompressionLevel);

				File.Delete(file);
			}
		}
	}

	private void MoveHistoryFiles(List<string> files, bool isDir)
	{
		try
		{
			Directory.CreateDirectory(HistoryMove);

			foreach (var file in files)
			{
				var destPath = Path.Combine(HistoryMove, Path.GetFileName(file));

				if (isDir)
					Directory.Move(file, destPath);
				else
					File.Move(file, destPath);
			}
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);
		}
	}

	/// <inheritdoc />
	protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
	{
		if (IsDisposed)
			return;

		if (!_triedHistoryPolicy)
		{
			TryDoHistoryPolicy();
			_triedHistoryPolicy = true;
		}

		var date = SeparateByDates != SeparateByDateModes.None
			? DateTime.Today /*message.Time.Date*/ // pyh: эмуляция года данных происходит за 5 секунд. На выходе 365 файлов лога? Бред.
			: default;

		string prevFileName = null;
		StreamWriterEx prevWriter = null;

		var isDisposing = false;

		foreach (var group in messages.GroupBy(m =>
		{
			if (m.IsDispose)
			{
				isDisposing = true;
				return null;
			}

			var fileName = FileName ?? GetSourceName(m.Source);

			if (prevFileName == fileName)
				return prevWriter;

			var key = (fileName, date);

			if (!_writers.TryGetValue(key, out var writer))
			{
				if (_writers.Count > 0 && date != default)
				{
					var outOfDate = _writers.Where(p => p.Key.Item2 < date).ToArray();

					if (outOfDate.Length > 0)
					{
						foreach (var pair in outOfDate)
							_writers.GetAndRemove(pair.Key).Dispose();

						TryDoHistoryPolicy();
					}
				}

				writer = OnCreateWriter(GetFileName(fileName, date));
				_writers.Add(key, writer);
			}

			prevFileName = fileName;
			prevWriter = writer;
			return writer;
		}))
		{
			var writer = group.Key;

			// dispose message
			if (writer is null)
				continue;

			try
			{
				foreach (var message in group)
				{
					WriteMessage(writer, message);

					if (MaxLength <= 0 || writer.BaseStream.Position < MaxLength)
						continue;

					var fileName = writer.Path;

					var key = _writers[writer];
					writer.Dispose();

					var maxIndex = 0;

					while (File.Exists(GetRollingFileName(fileName, maxIndex + 1)))
					{
						maxIndex++;
					}

					for (var i = maxIndex; i > 0; i--)
					{
						File.Move(GetRollingFileName(fileName, i), GetRollingFileName(fileName, i + 1));
					}

					File.Move(fileName, GetRollingFileName(fileName, 1));

					if (MaxCount > 0)
					{
						maxIndex++;

						for (var i = MaxCount; i <= maxIndex; i++)
						{
							File.Delete(GetRollingFileName(fileName, i));
						}
					}

					writer = OnCreateWriter(fileName);
					_writers[key] = writer;
				}
			}
			finally
			{
				writer.Flush();
			}
		}

		if (isDisposing)
			Dispose();
	}

	private static string GetRollingFileName(string fileName, int index)
	{
		if (index <= 0)
			throw new ArgumentOutOfRangeException(nameof(index), index, "Must be positive.");

		return Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "." + index + Path.GetExtension(fileName));
	}

	private string GetSourceName(ILogSource source)
	{
		if (source == null)
			throw new ArgumentNullException(nameof(source));

		var name = source.Name;

		if (!source.IsRoot && WriteChildDataToRootFile && source.Parent != null)
			name = GetSourceName(source.Parent);

		return name;
	}

#if NET6_0_OR_GREATER
	private void WriteMessage(TextWriter writer, LogMessage message)
	{
		var includeDateInLog = SeparateByDates == SeparateByDateModes.None;
		var bufferSize = (includeDateInLog ? (_maxDateChars + 1) : 0) + _maxTimeChars;

		Span<char> timeChars = stackalloc char[bufferSize];
		var length = FormatDateTime(timeChars, ConvertToLocalTime(message.TimeUtc), includeDateInLog);

		writer.Write(timeChars[..length]);
		writer.Write('|');

		var level = message.Level;
		if (level != LogLevels.Info)
		{
			Span<char> levelBuffer = stackalloc char[7];
			var levelStr = level.ToString();

			levelStr.AsSpan().CopyTo(levelBuffer);

			for (var i = levelStr.Length; i < 7; i++)
				levelBuffer[i] = ' ';

			writer.Write(levelBuffer);
		}
		else
		{
			writer.Write("       ");
		}

		writer.Write('|');
		WritePadded(writer, message.Source.Name, 10);
		writer.Write('|');

		if (WriteSourceId)
		{
			WritePadded(writer, message.Source.Id.ToString(), 20);
			writer.Write('|');
		}

		writer.WriteLine(message.Message);
	}

	private static void WritePadded(TextWriter writer, string value, int width)
	{
		writer.Write(value);
		var padding = width - value.Length;
		for (var i = 0; i < padding; i++)
			writer.Write(' ');
	}

	private static int FormatDateTime(Span<char> buffer, DateTime time, bool includeDate)
	{
		var offset = 0;

		if (includeDate)
		{
			var year = time.Year;
			var month = time.Month;
			var day = time.Day;

			buffer[0] = _digitChars[year / 1000];
			buffer[1] = _digitChars[year % 1000 / 100];
			buffer[2] = _digitChars[year % 100 / 10];
			buffer[3] = _digitChars[year % 10];
			buffer[4] = '/';
			buffer[5] = _digitChars[month / 10];
			buffer[6] = _digitChars[month % 10];
			buffer[7] = '/';
			buffer[8] = _digitChars[day / 10];
			buffer[9] = _digitChars[day % 10];
			buffer[10] = ' ';

			offset = _maxDateChars + 1;
		}

		var hour = time.Hour;
		var minute = time.Minute;
		var second = time.Second;
		var millisecond = time.Millisecond;

		buffer[offset + 0] = _digitChars[hour / 10];
		buffer[offset + 1] = _digitChars[hour % 10];
		buffer[offset + 2] = ':';
		buffer[offset + 3] = _digitChars[minute / 10];
		buffer[offset + 4] = _digitChars[minute % 10];
		buffer[offset + 5] = ':';
		buffer[offset + 6] = _digitChars[second / 10];
		buffer[offset + 7] = _digitChars[second % 10];
		buffer[offset + 8] = '.';
		buffer[offset + 9] = _digitChars[millisecond / 100];
		buffer[offset + 10] = _digitChars[millisecond % 100 / 10];
		buffer[offset + 11] = _digitChars[millisecond % 10];

		return offset + _maxTimeChars;
	}
#else
	private void WriteMessage(TextWriter writer, LogMessage message)
	{
		writer.Write(ToFastDateCharArray(ConvertToLocalTime(message.TimeUtc)));
		writer.Write("|");
		writer.Write("{0, -7}".Put(message.Level == LogLevels.Info ? string.Empty : message.Level.ToString()));
		writer.Write("|");
		writer.Write("{0, -10}".Put(message.Source.Name));
		writer.Write("|");

		if (WriteSourceId)
		{
			writer.Write("{0, -20}".Put(message.Source.Id));
			writer.Write("|");
		}

		writer.WriteLine(message.Message);
	}

	// http://ramblings.markstarmer.co.uk/2011/07/efficiency-datetime-tostringstring/
	private char[] ToFastDateCharArray(DateTime time)
	{
		var includeDateInLog = SeparateByDates == SeparateByDateModes.None;

		var timeChars = new char[_maxTimeChars + (includeDateInLog ? (_maxDateChars + 1) : 0)];

		var offset = 0;

		if (includeDateInLog)
		{
			var year = time.Year;
			var month = time.Month;
			var day = time.Day;

			timeChars[0] = _digitChars[year / 1000];
			timeChars[1] = _digitChars[year % 1000 / 100];
			timeChars[2] = _digitChars[year % 100 / 10];
			timeChars[3] = _digitChars[year % 10];
			timeChars[4] = '/';
			timeChars[5] = _digitChars[month / 10];
			timeChars[6] = _digitChars[month % 10];
			timeChars[7] = '/';
			timeChars[8] = _digitChars[day / 10];
			timeChars[9] = _digitChars[day % 10];
			timeChars[10] = ' ';

			offset = _maxDateChars + 1;
		}

		var hour = time.Hour;
		var minute = time.Minute;
		var second = time.Second;
		var millisecond = time.Millisecond;

		timeChars[offset + 0] = _digitChars[hour / 10];
		timeChars[offset + 1] = _digitChars[hour % 10];
		timeChars[offset + 2] = ':';
		timeChars[offset + 3] = _digitChars[minute / 10];
		timeChars[offset + 4] = _digitChars[minute % 10];
		timeChars[offset + 5] = ':';
		timeChars[offset + 6] = _digitChars[second / 10];
		timeChars[offset + 7] = _digitChars[second % 10];
		timeChars[offset + 8] = '.';
		timeChars[offset + 9] = _digitChars[millisecond % 1000 / 100];
		timeChars[offset + 10] = _digitChars[millisecond % 100 / 10];
		timeChars[offset + 11] = _digitChars[millisecond % 10];

		return timeChars;
	}
#endif

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		FileName = storage.GetValue<string>(nameof(FileName));
		MaxLength = storage.GetValue<long>(nameof(MaxLength));
		MaxCount = storage.GetValue<int>(nameof(MaxCount));
		Append = storage.GetValue<bool>(nameof(Append));
		LogDirectory = storage.GetValue<string>(nameof(LogDirectory));
		WriteChildDataToRootFile = storage.GetValue<bool>(nameof(WriteChildDataToRootFile));
		Extension = storage.GetValue<string>(nameof(Extension));
		WriteSourceId = storage.GetValue<bool>(nameof(WriteSourceId));
		DirectoryDateFormat = storage.GetValue<string>(nameof(DirectoryDateFormat));
		SeparateByDates = storage.GetValue<SeparateByDateModes>(nameof(SeparateByDates));

		HistoryPolicy = storage.GetValue(nameof(HistoryPolicy), HistoryPolicy);
		HistoryAfter = storage.GetValue(nameof(HistoryAfter), HistoryAfter);
		HistoryMove = storage.GetValue(nameof(HistoryMove), HistoryMove);
		HistoryCompressionLevel = storage.GetValue(nameof(HistoryCompressionLevel), HistoryCompressionLevel);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(FileName), FileName)
			.Set(nameof(MaxLength), MaxLength)
			.Set(nameof(MaxCount), MaxCount)
			.Set(nameof(Append), Append)
			.Set(nameof(LogDirectory), LogDirectory)
			.Set(nameof(WriteChildDataToRootFile), WriteChildDataToRootFile)
			.Set(nameof(Extension), Extension)
			.Set(nameof(WriteSourceId), WriteSourceId)
			.Set(nameof(DirectoryDateFormat), DirectoryDateFormat)
			.Set(nameof(SeparateByDates), SeparateByDates.To<string>())

			.Set(nameof(HistoryPolicy), HistoryPolicy)
			.Set(nameof(HistoryAfter), HistoryAfter)
			.Set(nameof(HistoryMove), HistoryMove)
			.Set(nameof(HistoryCompressionLevel), HistoryCompressionLevel)
		;
	}

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		_writers.Values.ForEach(w => w.Dispose());
		_writers.Clear();

		base.DisposeManaged();
	}
}