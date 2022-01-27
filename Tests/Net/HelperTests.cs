namespace Ecng.Tests.Net
{
	using System;
	using System.Net.Http;

	using Ecng.Common;
	using Ecng.Net;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

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
			cache.Set(method, correctUrl, new { });

			cache.TryGet<object>(method, correctUrl, out _).AssertTrue();
			cache.TryGet<object>(method, wrongUrl, out _).AssertFalse();

			cache.Remove(method, wrongUrl).AssertFalse();
			cache.Remove(method, correctUrl).AssertTrue();

			cache.TryGet<object>(method, correctUrl, out _).AssertFalse();
			cache.TryGet<object>(method, wrongUrl, out _).AssertFalse();

			cache.Set<object>(method, correctUrl, null);
			cache.TryGet<object>(method, correctUrl, out _).AssertFalse();

			cache.Set(method, correctUrl, 0);
			cache.TryGet<object>(method, correctUrl, out _).AssertTrue();

			cache.Clear();
			cache.TryGet<object>(method, correctUrl, out _).AssertFalse();

			cache.Set(method, correctUrl, 0);
			cache.RemoveLike(method, "https://stocksharp.com/api/products");
			cache.TryGet<object>(method, correctUrl, out _).AssertFalse();
		}
	}
}
