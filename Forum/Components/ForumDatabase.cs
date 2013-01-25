namespace Ecng.Forum.Components
{
	using Ecng.Forum.BusinessEntities;
	using Ecng.Logic.BusinessEntities;

	public class ForumDatabase : LogicDatabase<ForumUser, ForumRole>
	{
		public ForumDatabase(string name, string connectionString)
			: base(name, connectionString)
		{
		}
	}
}