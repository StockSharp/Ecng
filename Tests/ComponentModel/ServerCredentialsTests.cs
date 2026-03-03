namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class ServerCredentialsTests
{
	[TestMethod]
	public void IsValidLogin_Static()
	{
		((string)null).IsValidLogin(true).AssertFalse();
		"".IsValidLogin(true).AssertFalse();
		"   ".IsValidLogin(true).AssertFalse();
		"not-email".IsValidLogin(true).AssertFalse();
		"user@domain".IsValidLogin(true).AssertFalse();
		"user@domain.com".IsValidLogin(true).AssertTrue();
		"first.last+tag@sub.domain.co".IsValidLogin(true).AssertFalse();
	}

	[TestMethod]
	public void IsValidLogin_Username()
	{
		((string)null).IsValidLogin().AssertFalse();
		"".IsValidLogin().AssertFalse();
		"ab".IsValidLogin().AssertFalse(); // too short
		"a".IsValidLogin().AssertFalse();
		"a*b".IsValidLogin().AssertFalse(); // invalid char
		"-abc".IsValidLogin().AssertFalse(); // must start letter/digit
		"abc-".IsValidLogin().AssertFalse(); // must end letter/digit
		"a_b.c-1".IsValidLogin().AssertTrue();
		new string('a', 64).IsValidLogin().AssertTrue();
		new string('a', 65).IsValidLogin().AssertFalse();
	}

	[TestMethod]
	public void IsLoginValid_Instance()
	{
		var creds = new ServerCredentials();
		creds.Email = null;
		creds.IsLoginValid(true).AssertFalse();
		creds.Email = "bad";
		creds.IsLoginValid(true).AssertFalse();
		creds.Email = "user@example.com";
		creds.IsLoginValid(true).AssertTrue();
	}
}
