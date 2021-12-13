namespace Ecng.Tests.Net
{
	using System;

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
			"info@stocksharp.com".GetGravatarUrl(100).AssertEqual("https://www.gravatar.com/avatar/cf4c4e682b9869e05c4cc4536b734828?size=100");
		}

		[TestMethod]
		public void Cache()
		{
			var correctUrl = "https://stocksharp.com/api/products?id=12".To<Uri>();
			var wrongUrl = "https://stocksharp.com/api/products?id=13".To<Uri>();

			IRestApiClientCache cache = new InMemoryRestApiClientCache(TimeSpan.FromHours(1));
			cache.Set(correctUrl, new { });

			cache.TryGet<object>(correctUrl, out _).AssertTrue();
			cache.TryGet<object>(wrongUrl, out _).AssertFalse();

			cache.Remove(wrongUrl).AssertFalse();
			cache.Remove(correctUrl).AssertTrue();

			cache.TryGet<object>(correctUrl, out _).AssertFalse();
			cache.TryGet<object>(wrongUrl, out _).AssertFalse();

			cache.Set<object>(correctUrl, null);
			cache.TryGet<object>(correctUrl, out _).AssertFalse();

			cache.Set(correctUrl, 0);
			cache.TryGet<object>(correctUrl, out _).AssertTrue();

			cache.Clear();
			cache.TryGet<object>(correctUrl, out _).AssertFalse();
		}
	}
}
