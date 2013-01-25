namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	public class MessageTaglineList : ForumBaseEntityList<MessageTagline>
	{
		#region MessageTaglineList.ctor()

		public MessageTaglineList(IStorage storage)
			: base(storage)
		{
		}

		public MessageTaglineList(IStorage storage, Message message)
			: this(storage)
		{
			AddFilter(message);
		}

		#endregion
	}
}