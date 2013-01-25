namespace Ecng.Forum.Components
{
	using Ecng.Forum.BusinessEntities;

	public class PollLink : BaseEntityLink<Poll>
	{
		protected override ForumBaseNamedEntity GetNamedEntity(Poll entity)
		{
			return entity.Topic;
		}
	}
}