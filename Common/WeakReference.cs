namespace Ecng.Common
{
	public sealed class WeakReference<T> : System.WeakReference
	{
		#region WeakReference.ctor()

		public WeakReference(T target)
			: base(target)
		{
		}

		public WeakReference(T target, bool trackResurrection)
			: base(target, trackResurrection)
		{
		}

		#endregion

		#region Target

		public new T Target
		{
			get => (T)base.Target;
			set => base.Target = value;
		}

		#endregion
	}
}