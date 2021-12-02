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
				throw new ArgumentNullException(nameof(userName));

			if (string.Compare(Thread.CurrentPrincipal.Identity.Name, userName, StringComparison.OrdinalIgnoreCase) != 0)
				ThrowAccessException();
		}

		public static void Check(IEnumerable<string> roles)
		{
			if (roles is null)
				throw new ArgumentNullException(nameof(roles));

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
			throw new UnauthorizedAccessException($"Current principal '{Thread.CurrentPrincipal?.Identity?.Name}' hasn't required permissions.");
		}
	}
}
