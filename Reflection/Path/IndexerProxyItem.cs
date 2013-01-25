namespace Ecng.Reflection.Path
{
	using System.Collections.Generic;
	using System.Reflection;

	class IndexerProxyItem : MethodProxyItem
	{
		public IndexerProxyItem(MethodInfo method, IEnumerable<Param> parameters)
			: base(method, parameters)
		{
		}
	}
}