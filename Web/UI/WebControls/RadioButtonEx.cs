namespace Ecng.Web.UI.WebControls
{
	#region Using Directives

	using System.Collections.Specialized;
	using System.Reflection;
	using System.Web.UI;
	using System.Web.UI.WebControls;

	using Ecng.Reflection;

	#endregion

	public class RadioButtonEx : RadioButton, IPostBackDataHandler
	{
		private static readonly FastInvoker<RadioButton, string, VoidType> _invoker = FastInvoker<RadioButton, string, VoidType>.Create(typeof(RadioButton).GetMember<FieldInfo>("_uniqueGroupName"), false);

		protected override void Render(HtmlTextWriter writer)
		{
			InitUniqueGroupNameAndValue();
			base.Render(writer);
		}
        
		#region IPostBackDataHandler Members

		void IPostBackDataHandler.RaisePostDataChangedEvent()
		{
			InitUniqueGroupNameAndValue();
			base.RaisePostDataChangedEvent();
		}

		bool IPostBackDataHandler.LoadPostData(string postDataKey, NameValueCollection postCollection)
		{
			InitUniqueGroupNameAndValue();
			return base.LoadPostData(postDataKey, postCollection);
		}

		#endregion

		private void InitUniqueGroupNameAndValue()
		{
			_invoker.SetValue(this, GroupName);
			Attributes["value"] = UniqueID;
		}
	}
}