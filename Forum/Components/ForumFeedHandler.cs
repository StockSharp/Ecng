namespace Ecng.Forum.Components
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel.Syndication;
	using System.Linq;

	using Ecng.Forum.BusinessEntities;
	using Ecng.Web;
	using Ecng.Web.Feeds;

	public abstract class ForumFeedHandler<TFeed, TFormatter> : FeedHandler<TFeed, TFormatter>
		where TFeed : SyndicationFeed, new()
		where TFormatter : SyndicationFeedFormatter
	{
		private const int _messageMaxCount = 100;

		private enum ForumFeedTypes
		{
			ForumFolder,
			Forum,
			Topic,
			User,
			TopicTag,
			//ForumUser
		}

		//private class UserSyndicationPerson : SyndicationPerson
		//{
		//    public UserSyndicationPerson(User user)
		//        : base(user.Email, user.Name, ForumHelper.GetIdentityUrl(user).ToString())
		//    {
		//    }
		//}

		private static ForumFeedTypes FeedType
		{
			get
			{
				if (ForumHelper.Contains<ForumFolder>())
					return ForumFeedTypes.ForumFolder;
				else if (ForumHelper.Contains<Forum>())
					return ForumFeedTypes.Forum;
				else if (ForumHelper.Contains<Topic>())
					return ForumFeedTypes.Topic;
				else if (ForumHelper.Contains<User>())
					return ForumFeedTypes.User;
				else
					throw new InvalidOperationException();
			}
		}

		#region FeedHandler<TFeed, TFormatter> Members

		protected override void PopulateHeader(SyndicationFeed feed)
		{
			feed.Language = "en-US";
			feed.Generator = "Ecng Feed Generator";
			feed.Copyright = new TextSyndicationContent("Copyright @ Ecng 2004");

			BaseNamedEntity entity;
			//User author = null;

			switch (FeedType)
			{
				case ForumFeedTypes.ForumFolder:
					entity = ForumHelper.GetEntity<ForumFolder>();
					ForumHelper.SecurityBarrier.Check((ForumFolder)entity, PermissionTypes.Read);
					break;
				case ForumFeedTypes.Forum:
					entity = ForumHelper.GetEntity<Forum>();
					ForumHelper.SecurityBarrier.Check((Forum)entity, PermissionTypes.Read);
					break;
				case ForumFeedTypes.Topic:
					var topic = ForumHelper.GetEntity<Topic>();
					ForumHelper.SecurityBarrier.Check(topic, PermissionTypes.Read);
					entity = topic;
					break;
				//case ForumFeedTypes.User:
				//    entity = author = ForumHelper.GetEntity<User>();
				//    break;
				case ForumFeedTypes.TopicTag:
					entity = ForumHelper.GetEntity<TopicTag>();
					break;
				default:
					throw new InvalidOperationException();
			}

			feed.Id = entity.Id.ToString();
			feed.Title = new TextSyndicationContent(entity.Name + " from ChroBreak.Com");
			feed.Description = new TextSyndicationContent(entity.Description);
			feed.LastUpdatedTime = entity.ModificationDate;
			feed.Links.Add(SyndicationLink.CreateAlternateLink(ForumHelper.GetIdentityUrl(entity)));

			//if (author == null)
				feed.Authors.Add(new SyndicationPerson(AspNetPath.MakeEmail("webMaster")));
			//else
			//	feed.Authors.Add(new UserSyndicationPerson(author));
				
			//feed.AttributeExtensions.Add(new XmlQualifiedName("slash"), "http://purl.org/rss/1.0/modules/slash/");
		}

		protected override void PopulateItems(List<SyndicationItem> items)
		{
			foreach (var message in GetMessages())
			{
				var item = new SyndicationItem
				{
					Id = message.Id.ToString(),
					Title = new TextSyndicationContent(message.Topic.Name),
					Summary = new TextSyndicationContent(message.Topic.Description),
					PublishDate = message.CreationDate,
					LastUpdatedTime = message.ModificationDate,
					Content = SyndicationContent.CreateXhtmlContent(message.Body)
				};

				//item.Authors.Add(new UserSyndicationPerson(message.User));
				item.Authors.Add(new SyndicationPerson(AspNetPath.MakeEmail("webMaster")));
				item.Links.Add(SyndicationLink.CreateAlternateLink(ForumHelper.GetIdentityUrl(message)));
				//item.Links.Add(SyndicationLink.CreateSelfLink(ForumHelper.GetIdentityUrl(message)));
				item.Categories.Add(new SyndicationCategory(message.Topic.Name));
				//item.ElementExtensions.Add(new SyndicationElementExtension("comments", "slash", 1));
				
				items.Add(item);
			}
		}

		#endregion

		private static IEnumerable<Message> GetMessages()
		{
			if (FeedType == ForumFeedTypes.Topic)
				return ForumHelper.GetEntity<Topic>().Messages.ReadLastsModified(_messageMaxCount);
			else
			{
				Func<TopicList, IEnumerable<Message>> getMesages = topics => topics.ReadLastsModified(_messageMaxCount).SelectMany(t => new[] { t.Messages.FirstCreated });

				switch (FeedType)
				{
					case ForumFeedTypes.ForumFolder:
						using (ForumHelper.CreateScope(true))
							return ForumHelper.GetEntity<ForumFolder>().Messages.ReadLastsModified(_messageMaxCount);
					case ForumFeedTypes.Forum:
						return getMesages(ForumHelper.GetEntity<Forum>().Topics);
					case ForumFeedTypes.User:
						return getMesages(ForumHelper.GetEntity<User>().Topics);
					case ForumFeedTypes.Topic:
						return ForumHelper.GetEntity<Topic>().Messages.ReadLastsModified(_messageMaxCount);
					case ForumFeedTypes.TopicTag:
						return getMesages(ForumHelper.GetEntity<TopicTag>().Topics);
					default:
						throw new InvalidOperationException();
				}
			}
		}
	}
}