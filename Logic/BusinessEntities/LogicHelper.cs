namespace Ecng.Logic.BusinessEntities
{
	using System;
	using System.Reflection;
	using System.Threading;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Data;
	using Ecng.Serialization;
	using Ecng.Reflection;
	using Ecng.Web;

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class QueryStringIdAttribute : Attribute
	{
		public QueryStringIdAttribute(string idField)
		{
			IdField = idField;
		}

		public string IdField { get; private set; }
	}

	public static class LogicHelper<TUser, TRole>
		where TUser : BaseEntity<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		#region GetRootObject

		public static LogicRootObject<TUser, TRole> GetRootObject()
		{
			return ConfigManager.GetService<LogicRootObject<TUser, TRole>>();
		}

		public static TRoot GetRootObject<TRoot>()
			where TRoot : LogicRootObject<TUser, TRole>
		{
			return (TRoot)GetRootObject();
		}

		#endregion

		#region CurrentUser

		public static TUser CurrentUser
		{
			get
			{
				var identity = Thread.CurrentPrincipal.Identity;
				return identity.IsAuthenticated ? GetRootObject().GetUsers().ReadByName(identity.Name) : null;
			}
		}

		#endregion

		public static TValue SecureGet<TEntity, TValue>(BaseEntityList<TEntity, TUser, TRole> list, Func<BaseEntityList<TEntity, TUser, TRole>, TValue> func)
			where TEntity : BaseEntity<TUser, TRole>
		{
			using (CreateScope(SchemaManager.GetSchema<TEntity>(), true))
				return func(list);
		}

		public static Scope<HierarchicalDatabaseContext> CreateScope(Schema schema, bool restrict)
		{
			string morph;
			var source = new SerializationItemCollection();

			if (restrict)
			{
				morph = "Secure";
				source.Add(new SerializationItem(new VoidField<long>("CurrentUser"), CurrentUser != null ? (object)CurrentUser.Id : null));
			}
			else
				morph = string.Empty;

			return new Scope<HierarchicalDatabaseContext>(new HierarchicalDatabaseContext(morph, schema, source));
		}

		#region GetEntity

		public static TEntity GetEntity<TEntity>()
			where TEntity : BaseEntity<TUser, TRole>
		{
			return GetEntity<TEntity>(WebHelper.GetIdentity<TEntity>());
		}

		public static TEntity GetEntity<TEntity>(string id)
			where TEntity : BaseEntity<TUser, TRole>
		{
			var entity = GetRootObject().Database.Read<TEntity>(Url.Current.QueryString.GetValue<long>(id));

			if (entity == null)
				throw new ArgumentException("id");
			else
				return entity;
		}

		public static BaseEntity<TUser, TRole> GetEntity(Type entityType, string id)
		{
			return typeof(LogicHelper<TUser, TRole>)
					.GetMember<MethodInfo>("GetEntity", typeof(string))
					.Make(entityType)
					.GetValue<string, BaseEntity<TUser, TRole>>(id);
		}

		#endregion

		public static T GetOrCreateEntity<T>()
			where T : BaseEntity<TUser, TRole>, new()
		{
			return WebHelper.Contains<T>() ? GetEntity<T>() : new T();
		}
	}
}