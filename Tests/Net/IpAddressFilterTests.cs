namespace Ecng.Tests.Net;

using System.Net;

using Ecng.Net;

[TestClass]
public class IpAddressFilterTests : BaseTestClass
{
	[TestMethod]
	public void ExactIpMatch()
	{
		var filter = new IpAddressFilter("192.168.1.1, 10.0.0.5");

		filter.IsAllowed(IPAddress.Parse("192.168.1.1")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("10.0.0.5")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("192.168.1.2")).AssertFalse();
		filter.IsAllowed(IPAddress.Parse("10.0.0.6")).AssertFalse();
	}

	[TestMethod]
	public void CidrMaskMatch()
	{
		var filter = new IpAddressFilter("10.0.0.0/8");

		filter.IsAllowed(IPAddress.Parse("10.0.0.1")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("10.255.255.255")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("10.50.100.200")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("11.0.0.1")).AssertFalse();
		filter.IsAllowed(IPAddress.Parse("192.168.1.1")).AssertFalse();
	}

	[TestMethod]
	public void MixedExactAndCidr()
	{
		var filter = new IpAddressFilter("192.168.1.100, 10.0.0.0/8, 172.16.0.0/12");

		filter.IsAllowed(IPAddress.Parse("192.168.1.100")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("10.1.2.3")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("172.20.0.1")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("192.168.1.101")).AssertFalse();
		filter.IsAllowed(IPAddress.Parse("8.8.8.8")).AssertFalse();
	}

	[TestMethod]
	public void EmptyFilterAllowsAll()
	{
		var filter = new IpAddressFilter(string.Empty);
		filter.IsEmpty.AssertTrue();

		var filter2 = new IpAddressFilter((string)null);
		filter2.IsEmpty.AssertTrue();

		var filter3 = new IpAddressFilter(Array.Empty<string>());
		filter3.IsEmpty.AssertTrue();
	}

	[TestMethod]
	public void IPv6Support()
	{
		var filter = new IpAddressFilter("2001:db8::1, 2001:db8::/32");

		filter.IsAllowed(IPAddress.Parse("2001:db8::1")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("2001:db8::abcd")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("2001:db9::1")).AssertFalse();
	}

	[TestMethod]
	public void LoopbackEquivalence()
	{
		var filterV4 = new IpAddressFilter("127.0.0.1");
		filterV4.IsAllowed(IPAddress.Parse("::1")).AssertTrue();

		var filterV6 = new IpAddressFilter("::1");
		filterV6.IsAllowed(IPAddress.Parse("127.0.0.1")).AssertTrue();
	}

	[TestMethod]
	public void WhitespaceHandling()
	{
		var filter = new IpAddressFilter("  192.168.1.1 ,  10.0.0.0/8  , 172.16.0.1  ");

		filter.IsAllowed(IPAddress.Parse("192.168.1.1")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("10.5.5.5")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("172.16.0.1")).AssertTrue();
	}

	[TestMethod]
	public void ToIpFilterExtension()
	{
		var filter = "192.168.0.0/16".ToIpFilter();
		filter.IsAllowed(IPAddress.Parse("192.168.1.1")).AssertTrue();
		filter.IsAllowed(IPAddress.Parse("10.0.0.1")).AssertFalse();

		var empty = ((string)null).ToIpFilter();
		empty.IsEmpty.AssertTrue();
	}

	[TestMethod]
	public void NonEmptyFilter()
	{
		var filter = new IpAddressFilter("192.168.1.1");
		filter.IsEmpty.AssertFalse();

		var filter2 = new IpAddressFilter("10.0.0.0/8");
		filter2.IsEmpty.AssertFalse();
	}
}
