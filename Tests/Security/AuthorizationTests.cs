namespace Ecng.Tests.Security;

using System.Net;
using System.Security;

using Ecng.Common;
using Ecng.Security;

[TestClass]
public class AuthorizationTests : BaseTestClass
{
	private static SecureString ToSecure(string s) => s?.Secure();

	#region AnonymousAuthorization

	[TestMethod]
	public async Task Anonymous_AlwaysReturnsSessionId()
	{
		var auth = new AnonymousAuthorization();

		var session = await auth.ValidateCredentials("anyuser", ToSecure("anypass"), IPAddress.Loopback, CancellationToken);

		session.AssertNotNull();
		session.IsEmpty().AssertFalse();
	}

	[TestMethod]
	public async Task Anonymous_NullCredentials_StillWorks()
	{
		var auth = new AnonymousAuthorization();

		var session = await auth.ValidateCredentials(null, null, null, CancellationToken);

		session.AssertNotNull();
	}

	[TestMethod]
	public async Task Anonymous_ReturnsUniqueSessionIds()
	{
		var auth = new AnonymousAuthorization();

		var session1 = await auth.ValidateCredentials("user", ToSecure("pass"), IPAddress.Loopback, CancellationToken);
		var session2 = await auth.ValidateCredentials("user", ToSecure("pass"), IPAddress.Loopback, CancellationToken);

		session1.AssertNotEqual(session2);
	}

	#endregion

	#region SimpleAuthorization

	[TestMethod]
	public async Task Simple_EmptyLogin_FallsBackToAnonymous()
	{
		var auth = new SimpleAuthorization { Login = null, Password = null };

		var session = await auth.ValidateCredentials("anyuser", ToSecure("anypass"), IPAddress.Loopback, CancellationToken);

		session.AssertNotNull();
	}

	[TestMethod]
	public async Task Simple_CorrectCredentials_ReturnsSessionId()
	{
		var auth = new SimpleAuthorization { Login = "admin", Password = ToSecure("secret") };

		var session = await auth.ValidateCredentials("admin", ToSecure("secret"), IPAddress.Loopback, CancellationToken);

		session.AssertNotNull();
		session.IsEmpty().AssertFalse();
	}

	[TestMethod]
	public async Task Simple_CorrectCredentials_CaseInsensitiveLogin()
	{
		var auth = new SimpleAuthorization { Login = "Admin", Password = ToSecure("secret") };

		var session = await auth.ValidateCredentials("ADMIN", ToSecure("secret"), IPAddress.Loopback, CancellationToken);

		session.AssertNotNull();
	}

	[TestMethod]
	public async Task Simple_WrongPassword_Throws()
	{
		var auth = new SimpleAuthorization { Login = "admin", Password = ToSecure("secret") };

		await ThrowsExactlyAsync<UnauthorizedAccessException>(
			() => auth.ValidateCredentials("admin", ToSecure("wrong"), IPAddress.Loopback, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Simple_WrongLogin_Throws()
	{
		var auth = new SimpleAuthorization { Login = "admin", Password = ToSecure("secret") };

		await ThrowsExactlyAsync<UnauthorizedAccessException>(
			() => auth.ValidateCredentials("wronguser", ToSecure("secret"), IPAddress.Loopback, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Simple_NullPassword_Throws()
	{
		var auth = new SimpleAuthorization { Login = "admin", Password = ToSecure("secret") };

		await ThrowsExactlyAsync<UnauthorizedAccessException>(
			() => auth.ValidateCredentials("admin", null, IPAddress.Loopback, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Simple_NullExpectedPassword_Throws()
	{
		var auth = new SimpleAuthorization { Login = "admin", Password = null };

		await ThrowsExactlyAsync<UnauthorizedAccessException>(
			() => auth.ValidateCredentials("admin", ToSecure("anypass"), IPAddress.Loopback, CancellationToken).AsTask());
	}

	#endregion

	#region UnauthorizedAuthorization

	[TestMethod]
	public async Task Unauthorized_AlwaysThrows()
	{
		var auth = new UnauthorizedAuthorization();

		await ThrowsExactlyAsync<UnauthorizedAccessException>(
			() => ((IAuthorization)auth).ValidateCredentials("user", ToSecure("pass"), IPAddress.Loopback, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Unauthorized_NullCredentials_StillThrows()
	{
		var auth = new UnauthorizedAuthorization();

		await ThrowsExactlyAsync<UnauthorizedAccessException>(
			() => ((IAuthorization)auth).ValidateCredentials(null, null, null, CancellationToken).AsTask());
	}

	#endregion
}
