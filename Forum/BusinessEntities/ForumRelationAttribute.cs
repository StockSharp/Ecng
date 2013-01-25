namespace Ecng.Forum.BusinessEntities
{
	using Ecng.Data;

	public class ForumRelationAttribute : DataRelationAttribute
	{
		protected override Database GetDatabase()
		{
			return ForumRootObject.Instance.Database;
		}
	}
}