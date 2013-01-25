namespace Ecng.Forum.Components
{
	#region Using Directives

	using System.Collections;
	using System.ComponentModel;

	using Ecng.Web;

	#endregion

	public class ForumObjectModelSearchSiteMapProvider : ObjectModelSearchSiteMapProvider
	{
		public const bool DefaultRestrictValue = true;

		#region Restrict

		[DefaultValue(DefaultRestrictValue)]
		private bool _restrict = DefaultRestrictValue;

		public bool Restrict
		{
			get { return _restrict; }
			set { _restrict = value; }
		}

		#endregion

		public override IEnumerable DataQuery()
		{
			using (ForumHelper.CreateScope(this.Restrict))
				return base.DataQuery();
		}
	}
}