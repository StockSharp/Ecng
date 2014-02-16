namespace Ecng.Logic.BusinessEntities
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Web;

	public static class WebHelper
	{
		private readonly static SynchronizedDictionary<Type, string> _identifiers = new SynchronizedDictionary<Type, string>();

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

		public static string GetIdentity<T>()
		{
			return GetIdentity(typeof(T));
		}

		public static bool Contains<T>()
		{
			return Url.Current.QueryString.Contains(GetIdentity(typeof(T)));
		}

		public static void Append<TEntity, TUser, TRole>(this QueryString queryString, TEntity entity)
			where TEntity : BaseEntity<TUser, TRole>
			where TUser : BaseUser<TUser, TRole>
			where TRole : BaseRole<TUser, TRole>
		{
			if (queryString == null)
				throw new ArgumentNullException("queryString");

			if (entity == null)
				throw new ArgumentNullException("entity");

			queryString.Append(GetIdentity(entity.GetType()), entity.Id);
		}
	}
}