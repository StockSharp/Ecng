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
	using Ecng.Web;
	using Ecng.Web.UI;

	public abstract class LogicObjectModelDataSource : ObjectModelDataSource
	{
		public bool Restrict { get; set; }

		//protected abstract ObjectModelDataSourceView CreateView(ObjectModelDataSource owner, string viewName, MemberProxy proxy, object root, HttpContext context);
	}

	public class LogicObjectModelDataSourceView : ObjectModelDataSourceView
	{
		private readonly MethodInfo _getRangeMethod;
		private readonly Schema _schema;

		public LogicObjectModelDataSourceView(ObjectModelDataSource owner, string name, MemberProxy proxy, object root, HttpContext context, bool restrict)
			: base(owner, name, proxy, root, context)
		{
			Restrict = restrict;
			_getRangeMethod = typeof(LogicObjectModelDataSourceView).GetMember<MethodInfo>("nGetRange").Make(ItemType);
			_schema = ItemType.GetSchema();
		}

		public bool Restrict { get; }

		protected override int GetCount(IRangeCollection collection)
		{
			using (LogicHelper.CreateScope(_schema, Restrict))
				return base.GetCount(collection);
		}

		protected override IEnumerable GetRange(IRangeCollection collection, int startIndex, int count, string sortExpression, ListSortDirection direction)
		{
			using (LogicHelper.CreateScope(_schema, Restrict))
			{
				if (!sortExpression.IsEmpty() && !_schema.Fields.Contains(sortExpression))
					return _getRangeMethod.GetValue<object[], IEnumerable>(new object[] { collection, startIndex, count, new VoidField<ListSortDirection>(sortExpression), direction });
				else
					return base.GetRange(collection, startIndex, count, sortExpression, direction);
			}
		}

		private static IEnumerable<E> nGetRange<E>(RelationManyList<E> list, int startIndex, int count, VoidField field, ListSortDirection direction)
		{
			return list.ReadAll(startIndex, count, field, direction);
		}
	}
}