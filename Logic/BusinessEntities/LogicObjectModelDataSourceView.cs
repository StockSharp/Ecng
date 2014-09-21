namespace Ecng.Logic.BusinessEntities
{
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Reflection;
	using System.Web;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Reflection.Path;
	using Ecng.Serialization;
	using Ecng.Web.UI;

	public abstract class LogicObjectModelDataSource : ObjectModelDataSource
	{
		public bool Restrict { get; set; }

		//protected abstract ObjectModelDataSourceView CreateView(ObjectModelDataSource owner, string viewName, MemberProxy proxy, object root, HttpContext context);
	}

	public class LogicObjectModelDataSourceView<TUser, TRole> : ObjectModelDataSourceView
		where TUser : BaseUser<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		#region Private Fields

		private readonly MethodInfo _getRangeMethod;
		private readonly Schema _schema;

		#endregion

		#region LogicObjectModelDataSource.ctor()

		public LogicObjectModelDataSourceView(ObjectModelDataSource owner, string name, MemberProxy proxy, object root, HttpContext context, bool restrict)
			: base(owner, name, proxy, root, context)
		{
			_restrict = restrict;
			_getRangeMethod = typeof(LogicObjectModelDataSourceView<TUser, TRole>).GetMember<MethodInfo>("nGetRange").Make(ItemType);
			_schema = ItemType.GetSchema();
		}

		#endregion

		#region Restrict

		private readonly bool _restrict;

		public bool Restrict
		{
			get { return _restrict; }
		}

		#endregion

		#region ObjectModelDataSourceView Members

		protected override int GetCount(IListEx list)
		{
			using (LogicHelper<TUser, TRole>.CreateScope(_schema, Restrict))
				return base.GetCount(list);
		}

		protected override IEnumerable GetRange(IListEx list, int startIndex, int count, string sortExpression, ListSortDirection direction)
		{
			using (LogicHelper<TUser, TRole>.CreateScope(_schema, Restrict))
			{
				if (!sortExpression.IsEmpty() && !_schema.Fields.Contains(sortExpression))
					return _getRangeMethod.GetValue<object[], IEnumerable>(new object[] { list, startIndex, count, new VoidField<ListSortDirection>(sortExpression), direction });
				else
					return base.GetRange(list, startIndex, count, sortExpression, direction);
			}
		}

		#endregion

		private static IEnumerable<E> nGetRange<E>(RelationManyList<E> list, int startIndex, int count, VoidField field, ListSortDirection direction)
		{
			return list.ReadAll(startIndex, count, field, direction);
		}
	}
}