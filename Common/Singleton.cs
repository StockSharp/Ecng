namespace Ecng.Common
{
	using System;

	public abstract class Singleton<T> : Disposable
		where T : new()
	{
		private static readonly Lazy<T> _instance = new(() => new T());

		public static T Instance => _instance.Value;
	}
}