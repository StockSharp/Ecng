namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
    public class PollChoiceList : ForumBaseNamedEntityList<PollChoice>
    {
		public PollChoiceList(IStorage storage, Poll poll)
			: base(storage)
		{
			AddFilter(poll);
		}
	}
}