namespace Ecng.Tests.IO;

using System.IO.Compression;
using System.Text;

using Ecng.IO;

[TestClass]
public class CompressTests
{
	[TestMethod]
	public void Deflate()
	{
		var bytes = RandomGen.GetBytes(FileSizes.MB);

		bytes.DeflateTo().DeflateFrom().SequenceEqual(bytes).AssertTrue();
		bytes.Compress<DeflateStream>().DeflateFrom().SequenceEqual(bytes).AssertTrue();
		bytes.Compress<DeflateStream>().Uncompress<DeflateStream>().SequenceEqual(bytes).AssertTrue();
	}

	[TestMethod]
	public void GZip()
	{
		var bytes = RandomGen.GetBytes(FileSizes.MB);

		bytes.Compress<GZipStream>().Uncompress<GZipStream>().SequenceEqual(bytes).AssertTrue();
	}

	[TestMethod]
	public void GZipStr()
	{
		var str = "Hello world";
		str.UTF8().Compress<GZipStream>().UnGZip().AssertEqual(str);
	}

	[TestMethod]
	public void GZipToBuffer()
	{
		var bytes = RandomGen.GetBytes(FileSizes.KB);
		var rangeCount = 100;
		var destination = new byte[rangeCount * 2];
		var range = bytes.Compress<GZipStream>(count: rangeCount);
		var count = range.UnGZip(0, range.Length, destination);
		count.AssertEqual(rangeCount);
		bytes.Take(count).SequenceEqual(destination.Take(rangeCount)).AssertTrue();
	}

	[TestMethod]
	public void UnDeflateToBuffer()
	{
		var bytes = RandomGen.GetBytes(FileSizes.KB);
		var rangeCount = 100;
		var destination = new byte[rangeCount * 2];
		var range = bytes.Compress<DeflateStream>(count: rangeCount);
		var count = range.UnDeflate(0, range.Length, destination);
		count.AssertEqual(rangeCount);
		bytes.Take(count).SequenceEqual(destination.Take(rangeCount)).AssertTrue();
	}

	[TestMethod]
	public void Zip7()
	{
		var bytes = RandomGen.GetBytes(FileSizes.MB);

		// TODO 7Zip compression not implemented
		//bytes.Do7Zip().Un7Zip().SequenceEqual(bytes).AssertTrue();
		//bytes.Compress<Lzma.LzmaStream>().Uncompress<Lzma.LzmaStream>().SequenceEqual(bytes).AssertTrue();
	}

	[TestMethod]
	public async Task Async()
	{
		var bytes = RandomGen.GetBytes(FileSizes.MB);

		async Task Do<TCompress>()
			where TCompress : Stream
			=> (await (await bytes.CompressAsync<TCompress>()).UncompressAsync<TCompress>()).SequenceEqual(bytes).AssertTrue();

		await Do<DeflateStream>();
		await Do<GZipStream>();
		//await Do<Lzma.LzmaStream>();
	}

	[TestMethod]
	public void UsableEntry()
	{
		// Build a simple in-memory zip with two files
		var ms = new MemoryStream();

		using (var a = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
		{
			void Add(string name, string text)
			{
				var entry = a.CreateEntry(name);
				using var es = entry.Open();
				var data = text.UTF8();
				es.Write(data, 0, data.Length);
			}

			Add("a.txt", "hello");
			Add("b.txt", "world");
		}

		ms.Position = 0;

		string ReadAll(Stream s)
		{
			using var sr = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
			return sr.ReadToEnd();
		}

		using var zip = ms.Unzip(leaveOpen: true);

		// Materialize results first (typical pattern), then consume streams later
		var entries = zip.ToArray();

		entries.Length.AssertEqual(2);

		entries[0].name.EndsWith("a.txt").AssertTrue();
		ReadAll(entries[0].body).AssertEqual("hello");
		ReadAll(entries[0].body).AssertEqual(string.Empty);

		entries[1].name.EndsWith("b.txt").AssertTrue();
		ReadAll(entries[1].body).AssertEqual("world");
		ReadAll(entries[1].body).AssertEqual(string.Empty);
	}
}
