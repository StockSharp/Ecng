namespace Ecng.Tests.Net;

using Ecng.Net;

[TestClass]
public class UrlTests
{
    [TestMethod]
    public void Url_Construct_FromString()
    {
        // Test Url construction from string
        var url = new Url("https://example.com/path?foo=bar");
		"https://example.com/path?foo=bar".AssertEqual(url.ToString());
    }

    [TestMethod]
    public void Url_Construct_FromUri()
    {
        // Test Url construction from Uri
        var uri = new Uri("https://example.com/path?foo=bar");
        var url = new Url(uri);
		uri.ToString().AssertEqual(url.ToString());
    }

    [TestMethod]
    public void Url_Clone_CreatesCopy()
    {
        // Test Url cloning
        var url = new Url("https://example.com/path?foo=bar");
        var clone = url.Clone();
		url.ToString().AssertEqual(clone.ToString());
		url.AssertNotSame(clone);
    }

    [TestMethod]
    public void QueryString_Append_And_Get()
    {
        // Test appending and retrieving query parameters
        var url = new Url("https://example.com");
        url.QueryString.Append("foo", "bar");
		url.QueryString.Contains("foo").AssertTrue();
		"bar".AssertEqual(url.QueryString["foo"]);
    }

    [TestMethod]
    public void QueryString_Remove_And_Clear()
    {
        // Test removing and clearing query parameters
        var url = new Url("https://example.com?foo=bar&baz=qux");
        url.QueryString.Remove("foo");
		url.QueryString.Contains("foo").AssertFalse();
        url.QueryString.Clear();
        0.AssertEqual(url.QueryString.Count);
    }

    [TestMethod]
    public void QueryString_Clone_CreatesCopy()
    {
        // Test QueryString cloning
        var url = new Url("https://example.com?foo=bar");
        var clone = url.QueryString.Clone();
        url.QueryString.ToString().AssertEqual(clone.ToString());
		url.QueryString.AssertNotSame(clone);
    }

    [TestMethod]
    public void QueryString_TryGetValue_Works()
    {
        // Test TryGetValue for existing and non-existing keys
        var url = new Url("https://example.com?foo=123");
        123.AssertEqual(url.QueryString.TryGetValue<int>("foo"));
        0.AssertEqual(url.QueryString.TryGetValue<int>("bar"));
        42.AssertEqual(url.QueryString.TryGetValue("bar", 42));
		url.QueryString.TryGetValue("foo", out int value).AssertTrue();
		123.AssertEqual(value);
		url.QueryString.TryGetValue("bar", out value).AssertFalse();
    }

    [TestMethod]
    public void QueryString_Encoding_Works()
    {
		// Test encoding options for query string
		var url = new Url("https://example.com")
		{
			Encode = UrlEncodes.Lower
		};
		url.QueryString.Append("sp ce", "v@lue");
		url.QueryString.ToString().Contains("sp+ce").AssertTrue();
        url.Encode = UrlEncodes.Upper;
        url.QueryString.Append("another", "тест");
		url.QueryString.ToString().Contains("%D1%82%D0%B5%D1%81%D1%82").AssertTrue();
    }

    [TestMethod]
    public void QueryString_Enumerator_Works()
    {
        // Test enumerator for query string
        var url = new Url("https://example.com?foo=bar&baz=qux");
        var dict = url.QueryString.ToDictionary(kv => kv.Key, kv => kv.Value);
        "bar".AssertEqual(dict["foo"]);
        "qux".AssertEqual(dict["baz"]);
    }

    [TestMethod]
    public void QueryString_Equality_Works_WithDifferentOrder()
    {
        // Test QueryString equality with parameters added in different order
        var url1 = new Url("https://example.com");
        url1.QueryString.Append("a", 1);
        url1.QueryString.Append("b", 2);

        var url2 = new Url("https://example.com");
        url2.QueryString.Append("b", 2);
        url2.QueryString.Append("a", 1);

        // Should be equal regardless of order
        url1.QueryString.Equals(url2.QueryString).AssertTrue();
        (url1.QueryString == url2.QueryString).AssertTrue();
    }

    [TestMethod]
    public void QueryString_Equality_Works_WithDifferentValues()
    {
        // Test QueryString equality with different values
        var url1 = new Url("https://example.com");
        url1.QueryString.Append("a", 1);
        url1.QueryString.Append("b", 2);

        var url2 = new Url("https://example.com");
        url2.QueryString.Append("a", 1);
        url2.QueryString.Append("b", 3);

        // Should not be equal if values differ
        url1.QueryString.Equals(url2.QueryString).AssertFalse();
        (url1.QueryString == url2.QueryString).AssertFalse();
    }

    [TestMethod]
    public void QueryString_Equality_Works_WithDifferentCaseKeys()
    {
        // Test QueryString equality with different case in keys
        var url1 = new Url("https://example.com");
        url1.QueryString.Append("foo", "bar");
        var url2 = new Url("https://example.com");
        url2.QueryString.Append("FOO", "bar");
        // Should be equal because implementation uses case-insensitive keys
        url1.QueryString.Equals(url2.QueryString).AssertTrue();
        (url1.QueryString == url2.QueryString).AssertTrue();
    }

    [TestMethod]
    public void QueryString_Equality_Works_WithDifferentEncodes()
    {
        // Test QueryString equality with different UrlEncodes
        var url1 = new Url("https://example.com") { Encode = UrlEncodes.Lower };
        url1.QueryString.Append("foo", "тест");
        var url2 = new Url("https://example.com") { Encode = UrlEncodes.Upper };
        url2.QueryString.Append("foo", "тест");
        // Should be equal because equality is based on key-value pairs, not encoding
        url1.QueryString.Equals(url2.QueryString).AssertTrue();
        (url1.QueryString == url2.QueryString).AssertTrue();
    }

	/// <summary>
	/// Verifies that QueryString handles duplicate keys in URLs.
	/// </summary>
	[TestMethod]
	public void QueryString_ShouldHandleDuplicateKeys()
	{
		// URLs can have duplicate query parameters like ?a=1&a=2
		// This should not throw ArgumentException
		var url = new Url("https://example.com?foo=1&foo=2");
		url.ToString().AssertEqual("https://example.com/?foo=1&foo=2");
	}
}
