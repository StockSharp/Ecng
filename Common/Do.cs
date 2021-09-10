namespace Ecng.Common
{
	using System;
	using System.Globalization;

	public static class Do
	{
		public static T Invariant<T>(Func<T> func)
			=> CultureInfo.InvariantCulture.DoInCulture(func);

		public static void Invariant(Action action)
			=> CultureInfo.InvariantCulture.DoInCulture(action);
	}
}