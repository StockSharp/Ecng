namespace Ecng.Forum.Components
{
	using Ecng.Forum.BusinessEntities;

	public class MessageLink : BaseEntityLink<Message>
	{
		#region BaseEntityLink<Message> Members

		protected override ForumBaseNamedEntity GetNamedEntity(Message entity)
		{
			return entity.Topic;
		}

		//protected override Url GetUrl(Message entity)
		//{
		//    int index = entity.Topic.Messages.IndexOf(entity);

		//    var url = base.GetUrl(entity);

		//    if (index != 0)
		//    {
		//        int page = index / ForumHelper.PageSize;

		//        if (page > 0)
		//            url.QueryString.Append("page", page);

		//        url.Fragment = "#n" + entity.Id;
		//    }

		//    return url;
		//}

		#endregion
	}
}