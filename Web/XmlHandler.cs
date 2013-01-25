namespace Ecng.Web
{
	#region Using Directives

	using System.Web;
    using System.Xml;

	#endregion

	public abstract class XmlHandler : IHttpHandler
	{
		#region IHttpHandler Members

		bool IHttpHandler.IsReusable
		{
			get { return false; }
		}

		void IHttpHandler.ProcessRequest(HttpContext context)
		{
            context.Response.Buffer = false;
            context.Response.Clear();
            context.Response.ContentType = "application/xml";

			using (var writer = XmlWriter.Create(context.Response.OutputStream, new XmlWriterSettings { Indent = true }))
            {
                OnProcessRequest(writer);
                writer.Flush();
            }

            context.Response.End();
		}

		#endregion

		protected abstract void OnProcessRequest(XmlWriter writer);
	}
}