namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using Ecng.Serialization;

	#endregion

	public class TopicTagList : ForumBaseNamedEntityList<TopicTag>
	{
		public TopicTagList(IStorage storage)
			: base(storage)
		{
		}
	}
		

	class TopicTopicTagList : TopicTagList
	{
		public TopicTopicTagList(IStorage storage, Topic topic)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Topic"), topic);
		}
	}

	class ForumTopicTagList : TopicTagList
	{
		#region ForumTopicTagList.ctor()

		public ForumTopicTagList(IStorage storage, Forum forum)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Forum"), forum);
		}

		#endregion
	}
}