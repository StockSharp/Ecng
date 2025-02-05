namespace Ecng.Tests.Net
{
	using System.Net;
	using System.Net.Http;
	using System.Text;

	using Ecng.Net;

	[TestClass]
	public class HelperTests
	{
		[TestMethod]
		public void Gravatar()
		{
			"info@stocksharp.com".GetGravatarToken().GetGravatarUrl(100).AssertEqual("https://www.gravatar.com/avatar/cf4c4e682b9869e05c4cc4536b734828?size=100");
		}

		[TestMethod]
		public void Cache()
		{
			var correctUrl = "https://stocksharp.com/api/products?id=12".To<Uri>();
			var wrongUrl = "https://stocksharp.com/api/products?id=13".To<Uri>();

			var method = HttpMethod.Get;

			IRestApiClientCache cache = new InMemoryRestApiClientCache(TimeSpan.FromHours(1));
			cache.Set(method, correctUrl, default, new { });

			cache.TryGet<object>(method, correctUrl, default, out _).AssertTrue();
			cache.TryGet<object>(method, wrongUrl, default, out _).AssertFalse();

			cache.Remove(method, wrongUrl.To<string>());
			cache.Remove(method, correctUrl.To<string>());

			cache.TryGet<object>(method, correctUrl, default, out _).AssertFalse();
			cache.TryGet<object>(method, wrongUrl, default, out _).AssertFalse();

			cache.Set<object>(method, correctUrl, default, null);
			cache.TryGet<object>(method, correctUrl, default, out _).AssertFalse();

			cache.Set(method, correctUrl, default, 0);
			cache.TryGet<object>(method, correctUrl, default, out _).AssertTrue();

			cache.Remove();
			cache.TryGet<object>(method, correctUrl, default, out _).AssertFalse();

			cache.Set(method, correctUrl, default, 0);
			cache.Remove(method, "https://stocksharp.com/api/products");
			cache.TryGet<object>(method, correctUrl, default, out _).AssertFalse();
		}

		[TestMethod]
		public void Image()
		{
			"1.png".IsImage().AssertTrue();
			"C:\\1.png".IsImage().AssertTrue();

			"1.svg".IsImage().AssertTrue();
			"C:\\1.svg".IsImage().AssertTrue();

			"1.doc".IsImage().AssertFalse();
			"C:\\1.doc".IsImage().AssertFalse();

			"1.doc".IsImageVector().AssertFalse();
			"C:\\1.doc".IsImageVector().AssertFalse();

			"1.svg".IsImageVector().AssertTrue();
			"C:\\1.svg".IsImageVector().AssertTrue();

			".png".IsImage().AssertTrue();
			"C:\\.png".IsImage().AssertTrue();

			".svg".IsImageVector().AssertTrue();
			"C:\\.svg".IsImageVector().AssertTrue();
		}

		[TestMethod]
		public void IsInSubnet()
		{
			static bool IsInSubnet(string addr)
				=> addr.To<IPAddress>().IsInSubnet("95.31.0.0/16");

			IsInSubnet("95.31.174.147").AssertTrue();
			IsInSubnet("95.31.174.134").AssertTrue();
			IsInSubnet("95.31.174.112").AssertTrue();
			IsInSubnet("95.32.161.158").AssertFalse();
		}

		[TestMethod]
		public void ContentType2Encoding()
		{
			var result = ((string)null).TryExtractEncoding();
			result.AssertNull();

			string.Empty.TryExtractEncoding().AssertNull();
			"text/html".TryExtractEncoding().AssertNull();

			result = "text/html; charset=utf-8".TryExtractEncoding();
			result.AssertNotNull();
			result.WebName.AreEqual(Encoding.UTF8.WebName);

			result = "application/json; charset=\"ISO-8859-1\"".TryExtractEncoding();
			result.AssertNotNull();
			result.WebName.AreEqual(Encoding.GetEncoding("iso-8859-1").WebName);

			result = "text/plain; charset=windows-1252; format=flowed".TryExtractEncoding();
			result.AssertNotNull();
			result.WebName.AreEqual(Encoding.GetEncoding("windows-1252").WebName);

			result = "text/html; charset=invalid-charset".TryExtractEncoding();
			result.AssertNull();

			result = "charset=UTF-16; text/html".TryExtractEncoding();
			result.AssertNotNull();
			result.WebName.AreEqual(Encoding.Unicode.WebName);
		}
	}
}
