namespace Ecng.Common
{
	#region Using Directives

	using System;

	#endregion

	public interface ICloneable<T> : ICloneable
	{
		new T Clone();
	}
}