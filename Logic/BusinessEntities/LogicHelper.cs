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

	public static class LogicHelper
	{
		#region GetRootObject

		public static LogicRootObject GetRootObject()
		{
			return ConfigManager.GetService<LogicRootObject>();
		}

		public static TRoot GetRootObject<TRoot>()
			where TRoot : LogicRootObject
		{
			return (TRoot)GetRootObject();
		}

		#endregion

		#region CurrentUser

		public static IWebUser CurrentUser
		{
			get
			{
				var identity = Thread.CurrentPrincipal.Identity;
				return identity.IsAuthenticated ? GetRootObject().GetUsers().ReadByName(identity.Name) : default(IWebUser);
			}
		}

		#endregion

		public static TValue SecureGet<TEntity, TValue>(BaseEntityList<TEntity> list, Func<BaseEntityList<TEntity>, TValue> func)
			where TEntity : BaseEntity
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
				source.Add(new SerializationItem(new VoidField<long>("CurrentUser"), CurrentUser.Key));
			}
			else
				morph = string.Empty;

			return new Scope<HierarchicalDatabaseContext>(new HierarchicalDatabaseContext(morph, schema, source));
		}

		#region GetEntity

		public static TEntity GetEntity<TEntity>()
			where TEntity : BaseEntity
		{
			return GetEntity<TEntity>(GetIdentity<TEntity>());
		}

		public static TEntity GetEntity<TEntity>(string id)
			where TEntity : BaseEntity
		{
			var entity = GetRootObject().Database.Read<TEntity>(Url.Current.QueryString.GetValue<long>(id));

			if (entity == null)
				throw new ArgumentException("id");
			else
				return entity;
		}

		public static BaseEntity GetEntity(Type entityType, string id)
		{
			return typeof(LogicHelper)
					.GetMember<MethodInfo>("GetEntity", typeof(string))
					.Make(entityType)
					.GetValue<string, BaseEntity>(id);
		}

		#endregion

		public static T GetOrCreateEntity<T>()
			where T : BaseEntity, new()
		{
			return Contains<T>() ? GetEntity<T>() : new T();
		}

		private static readonly SynchronizedDictionary<Type, string> _identifiers = new SynchronizedDictionary<Type, string>();

		public static string GetIdentity(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

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

		public static string GetIdentity<T>()
		{
			return GetIdentity(typeof(T));
		}

		public static bool Contains<T>()
		{
			return Url.Current.QueryString.Contains(GetIdentity(typeof(T)));
		}

		public static void Append<TEntity>(this QueryString queryString, TEntity entity)
			where TEntity : BaseEntity
			//where TUser : IWebUser
			//where TRole : IWebRole
		{
			if (queryString == null)
				throw new ArgumentNullException(nameof(queryString));

			if (entity == null)
				throw new ArgumentNullException(nameof(entity));

			queryString.Append(GetIdentity(entity.GetType()), entity.Id);
		}
	}
}