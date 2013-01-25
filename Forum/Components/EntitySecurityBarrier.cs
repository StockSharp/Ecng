namespace Ecng.Forum.Components
{
	#region Using Directives

	using System;
	using System.Linq;

	using Ecng.Forum.BusinessEntities;
	using Ecng.Common;
	using Ecng.Security;

	#endregion

	public class EntitySecurityBarrier
	{
		#region Private Fields

		private readonly ForumRoleList _roles;

		#endregion

		#region EntitySecurityBarrier.ctor()

		public EntitySecurityBarrier()
		{
			_roles = ForumHelper.GetRootObject<ForumRootObject>().Roles;
		}

		#endregion

		public void Check(Forum forum)
		{
			if (forum == null)
				throw new ArgumentNullException("forum");

			if (forum.IsLocked)
				Check(_roles.Moderators);
		}

		public void Check(Topic topic)
		{
			if (topic == null)
				throw new ArgumentNullException("topic");

			if (topic.Type == TopicTypes.Locked || topic.Type == TopicTypes.Announce)
				Check(_roles.Moderators);
		}

		public void Check(params ForumRole[] requiredRoles)
		{
			PermissionBarrier.Check(requiredRoles.Select(arg => arg.Name));
		}

		public File Check(File file)
		{
			if (file == null)
				throw new ArgumentNullException("file");

			if (ForumHelper.CurrentUser != file.User)
				Check(_roles.Administrators);

			return file;
		}

		public Poll Check(Poll poll, PermissionTypes permissions)
		{
			return CheckRetVal(poll, permissions, TryCheck(poll, permissions));
		}

		public bool TryCheck(Poll poll, PermissionTypes permissions)
		{
			if (poll == null)
				throw new ArgumentNullException("poll");

			return TryCheck(poll.Topic, permissions);
		}

		public Message Check(Message message, PermissionTypes permissions)
		{
			return CheckRetVal(message, permissions, TryCheck(message, permissions));
		}

		public bool TryCheck(Message message, PermissionTypes permissions)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return TryCheck(message.Topic, permissions);
		}

		public Topic Check(Topic topic, PermissionTypes permissions)
		{
			return CheckRetVal(topic, permissions, TryCheck(topic, permissions));
		}

		public bool TryCheck(Topic topic, PermissionTypes permissions)
		{
			if (topic == null)
				throw new ArgumentNullException("topic");

			var retVal = TryCheck(topic.Forum, permissions);

			if (!retVal && topic.Forum == ForumHelper.GetRootObject<ForumRootObject>().Forums.PrivateMessages && (permissions == PermissionTypes.Create || permissions == PermissionTypes.Read))
			{
				var user = ForumHelper.CurrentUser;

				if (user != null)
					return user.PrivateTopics.Contains(topic);
			}

			return retVal;
		}

		public Forum Check(Forum forum, PermissionTypes permissions)
		{
			return CheckRetVal(forum, permissions, TryCheck(forum, permissions));
		}

		public bool TryCheck(Forum forum, PermissionTypes permissions)
		{
			if (forum == null)
				throw new ArgumentNullException("forum");

			return TryCheck(forum.Folder, permissions);
		}

		public ForumFolder Check(ForumFolder forumFolder, PermissionTypes permissions)
		{
			return CheckRetVal(forumFolder, permissions, TryCheck(forumFolder, permissions));
		}

		public bool TryCheck(ForumFolder forumFolder, PermissionTypes permissions)
		{
			var roles = ForumHelper.CurrentUser != null ? ForumHelper.CurrentUser.Roles.ToArray<ForumRole>() : new[] { _roles.Users };

			return TryCheck(forumFolder, permissions, roles);
		}

		public bool TryCheck(ForumFolder forumFolder, PermissionTypes permissions, ForumRole[] roles)
		{
			if (forumFolder == null)
				throw new ArgumentNullException("forumFolder");

			//if (forumFolder.Parent != null && !TryCheck(forumFolder.Parent, permissions, roles))
			//	return false;

			return roles.Select(role => forumFolder.Entries.ReadByRole(role)).Any(entry => entry != null && entry.Permissions.Contains(permissions));
		}

		private static E CheckRetVal<E>(E entity, PermissionTypes permissions, bool retVal)
			where E : ForumBaseEntity
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			if (!retVal)
				throw new UnauthorizedAccessException("Entity of type '{0}' with id '{1}' can't be '{2}'.".Put(entity.GetType(), entity.Id, permissions));
			else
				return entity;
		}
	}
}