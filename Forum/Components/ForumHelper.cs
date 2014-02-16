namespace Ecng.Forum.Components
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Configuration;

	using Ecng.Forum.Components.Configuration;
	using Ecng.Forum.BusinessEntities;
	using Ecng.Logic.BusinessEntities;
	using Ecng.Common;
	using Ecng.Web;
	using Ecng.Configuration;

	#endregion

	public static class ForumHelper
	{
		#region ForumHelper.cctor()

		static ForumHelper()
		{
			PageTypes = new Dictionary<Type, Type>();

			var section = ConfigManager.GetSection<ForumSection>();

			if (section != null)
			{
				foreach (ProviderSettings type in section.PageTypes)
					PageTypes.Add(type.Name.To<Type>(), type.Type.To<Type>());
			}

			LogicHelper<ForumUser, ForumRole>.GetRootObject<ForumRootObject>();

			Logger = new Logger();
			SecurityBarrier = new EntitySecurityBarrier();
		}

		#endregion

		public static Dictionary<Type, Type> PageTypes { get; private set; }
		public static Logger Logger { get; private set; }
		public static EntitySecurityBarrier SecurityBarrier { get; private set; }

		#region RootObject

		public static TRoot GetRootObject<TRoot>()
			where TRoot : ForumRootObject
		{
			return LogicHelper<ForumUser, ForumRole>.GetRootObject<TRoot>();
		}

		#endregion

		#region CurrentUser

		public static ForumUser CurrentUser
		{
			get { return LogicHelper<ForumUser, ForumRole>.CurrentUser; }
		}

		#endregion

		#region PageSize

		public static int PageSize
		{
			get
			{
				var user = CurrentUser;
				return user != null ? user.PageSize : HttpHelper.DefaultPageSize;
			}
		}

		#endregion

		public static T GetEntity<T>()
			where T : BaseEntity<ForumUser, ForumRole>
		{
			return LogicHelper<ForumUser, ForumRole>.GetEntity<T>();
		}

		public static T GetEntity<T>(string id)
			where T : BaseEntity<ForumUser, ForumRole>
		{
			return LogicHelper<ForumUser, ForumRole>.GetEntity<T>(id);
		}

		public static ForumBaseEntity GetEntity(Type entityType, string id)
		{
			return (ForumBaseEntity)LogicHelper<ForumUser, ForumRole>.GetEntity(entityType, id);
		}

		public static void Redirect(BaseEntity<ForumUser, ForumRole> entity)
		{
			GetIdentityUrl(entity).Redirect();
		}

		public static T GetOrCreateEntity<T>()
			where T : BaseEntity<ForumUser, ForumRole>, new()
		{
			return LogicHelper<ForumUser, ForumRole>.GetOrCreateEntity<T>();
		}

		public static Url GetIdentityUrl<T>(T entity)
			where T : BaseEntity<ForumUser, ForumRole>
		{
			return GetIdentityUrl((BaseEntity<ForumUser, ForumRole>)entity);
		}

		public static Url GetIdentityUrl(BaseEntity<ForumUser, ForumRole> entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			var url = new Url(PageTypes[entity.GetType()]);
			url.QueryString.Append(WebHelper.GetIdentity(entity.GetType()), entity.Id);
			return url;
		}

		//public static Scope<HierarchicalDatabaseContext> CreateScope(bool restrict)
		//{
		//    return LogicHelper<ForumUser, ForumRole>.CreateScope(restrict);
		//}
	}
}