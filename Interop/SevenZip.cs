namespace Ecng.Interop
{
	using System;
	using System.IO;

	using Ecng.Common;
	using Ecng.Serialization;

	using global::SevenZip;

	public static class SevenZip
	{
		private static bool _isInitialized;

		private static string _dllDir = Directory.GetCurrentDirectory();

		public static string DllDir
		{
			get { return _dllDir; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_dllDir = value;

				var fileName = Environment.Is64BitProcess ? "7z64.dll" : "7z.dll";
				(Environment.Is64BitProcess ? Properties.Resources._7z64 : Properties.Resources._7z).Save(Path.Combine(value, fileName));
				SevenZipBase.SetLibraryPath(fileName);

				_isInitialized = true;
			}
		}

		private static void Init()
		{
			if (_isInitialized)
				return;

			DllDir = Directory.GetCurrentDirectory();
		}

		public static byte[] Extract(byte[] from)
		{
			var to = new MemoryStream();
			Extract(from.To<Stream>(), to);
			return to.GetBuffer();
		}

		public static void Extract(Stream from, Stream to, string password = null)
		{
			if (from == null)
				throw new ArgumentNullException(nameof(@from));

			if (to == null)
				throw new ArgumentNullException(nameof(to));

			Init();

			using (var e = password.IsEmpty() ? new SevenZipExtractor(from) : new SevenZipExtractor(from, password))
				e.ExtractFile(0, to);
		}

		public static byte[] Compress(byte[] from)
		{
			var to = new MemoryStream();
			Compress(from.To<Stream>(), to);
			return to.GetBuffer();
		}

		public static void Compress(Stream from, Stream to, string password = null)
		{
			if (from == null)
				throw new ArgumentNullException(nameof(@from));

			if (to == null)
				throw new ArgumentNullException(nameof(to));

			Init();

			var e = new SevenZipCompressor();
			e.CompressStream(from, to, password);
		}
	}
}