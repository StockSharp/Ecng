namespace Ecng.Forum.Components
{
	using System.Web;

	using Ecng.Forum.BusinessEntities;
	using Ecng.Reflection.Path;
	using Ecng.Web.UI;
	using Ecng.Logic.BusinessEntities;

	public class ForumObjectModelDataSourceView : LogicObjectModelDataSourceView<ForumUser, ForumRole>
	{
		public ForumObjectModelDataSourceView(ObjectModelDataSource owner, string name, MemberProxy proxy, object root, HttpContext context, bool restrict)
			: base(owner, name, proxy, root, context, restrict)
		{
		}
	}
}