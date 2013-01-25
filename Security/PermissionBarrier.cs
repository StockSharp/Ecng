namespace Ecng.Security
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Threading;

	using Ecng.Common;

	#endregion

	public static class PermissionBarrier
	{
		public static void Check(string userName)
		{
			if (userName.IsEmpty())
				throw new ArgumentNullException("userName");

			if (string.Compare(Thread.CurrentPrincipal.Identity.Name, userName, StringComparison.OrdinalIgnoreCase) != 0)
				ThrowAccessException();
		}

		public static void Check(IEnumerable<string> roles)
		{
			if (roles == null)
				throw new ArgumentNullException("roles");

			foreach (var role in roles)
			{
				if (Thread.CurrentPrincipal.IsInRole(role))
					break;

				ThrowAccessException();
			}
		}

		public static void Check()
		{
			if (!Thread.CurrentPrincipal.Identity.IsAuthenticated)
				ThrowAccessException();
		}

		private static void ThrowAccessException()
		{
			throw new UnauthorizedAccessException("Current principal '{0}' hasn't required permissions.".Put(Thread.CurrentPrincipal.Identity.Name));
		}
	}
}
