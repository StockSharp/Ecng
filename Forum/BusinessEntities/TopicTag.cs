namespace Ecng.Forum.BusinessEntities
{
	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	[Audit((byte)AuditSchemas.TopicTag)]
	public class TopicTag : ForumBaseNamedEntity
	{
		[RelationMany(typeof(TagTopicList))]
		public TopicList Topics { get; protected set; }
	}
}