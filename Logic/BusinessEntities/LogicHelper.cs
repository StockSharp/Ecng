namespace Ecng.Logic.BusinessEntities
{
	using System;
	using System.Linq;
	using System.Reflection;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Data;
	using Ecng.Serialization;
	using Ecng.Reflection;
	using Ecng.Web;

	public static class WebHelper
	{
		public static void Append<TEntity, TUser, TRole>(this QueryString queryString, TEntity entity)
			where TEntity : BaseEntity<TUser, TRole>
			where TUser : BaseUser<TUser, TRole>
			where TRole : BaseRole<TUser, TRole>
		{
			if (queryString == null)
				throw new ArgumentNullException("queryString");

			if (entity == null)
				throw new ArgumentNullException("entity");

			queryString.Append(LogicHelper<TUser, TRole>.GetIdentity(entity.GetType()), entity.Id);
		}
	}

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
		where TUser : BaseUser<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		private readonly static SynchronizedDictionary<Type, string> _identifiers = new SynchronizedDictionary<Type, string>();

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

		public static string GetIdentity<T>()
			where T : BaseEntity<TUser, TRole>
		{
			return GetIdentity(typeof(T));
		}

		public static string GetIdentity(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return _identifiers.SafeAdd(type, delegate
			{
				var attr = type.GetAttribute<QueryStringIdAttribute>();

				if (attr != null)
					return attr.IdField;
				else
				{
					var upperChars = type.Name.ToCharArray().Where(char.IsUpper).ToArray();
					return new string(upperChars).ToLowerInvariant() + "id";
				}
			});
		}

		#region GetEntity

		public static TEntity GetEntity<TEntity>()
			where TEntity : BaseEntity<TUser, TRole>
		{
			return GetEntity<TEntity>(GetIdentity(typeof(TEntity)));
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

		public static bool Contains<T>()
			where T : BaseEntity<TUser, TRole>
		{
			return Url.Current.QueryString.Contains(GetIdentity(typeof(T)));
		}

		public static T GetOrCreateEntity<T>()
			where T : BaseEntity<TUser, TRole>, new()
		{
			return Contains<T>() ? GetEntity<T>() : new T();
		}
	}
}