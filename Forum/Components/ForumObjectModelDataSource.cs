namespace Ecng.Forum.Components
{
	#region Using Directives

	using System.Web;

	using Ecng.Forum.BusinessEntities;
	using Ecng.Reflection.Path;
	using Ecng.Web.UI;
	using Ecng.Logic.BusinessEntities;

	#endregion

	public class ForumObjectModelDataSource : LogicObjectModelDataSource
	{
		#region ForumObjectModelDataSource.ctor()

		public ForumObjectModelDataSource()
		{
			RootType = typeof(ForumRootObject).AssemblyQualifiedName;
		}

		#endregion

		protected override ObjectModelDataSourceView CreateView(ObjectModelDataSource owner, string viewName, MemberProxy proxy, object root, HttpContext context)
		{
			return new ForumObjectModelDataSourceView(owner, viewName, proxy, root, context, Restrict);
		}
	}
}