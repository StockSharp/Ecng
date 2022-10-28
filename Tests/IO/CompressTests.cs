namespace Ecng.Tests.IO
{
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.IO;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	}
}
