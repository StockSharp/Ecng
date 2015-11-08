namespace Ecng.Security
{
	using System;
	using System.Globalization;
	using System.Security;

	using Ecng.Common;

	public abstract class CodeAccessPermission<T> : CodeAccessPermission
		where T : IPermission
	{
		public override IPermission Copy()
		{
			return OnCopy();
		}

		public override void FromXml(SecurityElement elem)
		{
			if (elem == null)
				throw new ArgumentNullException(nameof(elem));

			if (!elem.Tag.Equals("IPermission"))
				throw new ArgumentException("elem");

			string className = elem.Attribute("class");
			if (className.IsEmpty())
				throw new ArgumentException("elem");

			if (className.IndexOf(GetType().FullName, StringComparison.InvariantCultureIgnoreCase) < 0)
				throw new ArgumentException("elem");

			string unrestricted = elem.Attribute("Unrestricted");
			if (!unrestricted.IsEmpty() && (string.Compare(unrestricted, "true", true, CultureInfo.InvariantCulture) == 0))
				OnFromXml(elem, true);
			else
				OnFromXml(elem, false);
		}

		public override IPermission Intersect(IPermission target)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			return OnIntersect((T)target);
		}

		public override bool IsSubsetOf(IPermission target)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			return OnIsSubsetOf((T)target);
		}

		public override SecurityElement ToXml()
		{
			var elem = new SecurityElement("IPermission");

			elem.AddAttribute("class", GetType() + ", " + GetType().Module.Assembly.FullName.Replace('"', '\''));
			elem.AddAttribute("version", "1");

			if (OnToXml(elem))
				elem.AddAttribute("Unrestricted", "true");

			return elem;
		}

		public override IPermission Union(IPermission other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return OnUnion((T)other);
		}

		protected abstract void OnFromXml(SecurityElement elem, bool unrestricted);
		protected abstract bool OnToXml(SecurityElement elem);
		protected abstract T OnCopy();
		protected abstract T OnIntersect(T target);
		protected abstract bool OnIsSubsetOf(T target);
		protected abstract T OnUnion(T other);
	}
}