namespace Ecng.Common
{
	#region Using Directives

	using System;

	#endregion

#if SILVERLIGHT
	[CLSCompliant(false)]
#endif
	public interface ICloneable<T> : ICloneable
	{
		new T Clone();
	}
}