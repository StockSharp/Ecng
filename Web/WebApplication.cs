namespace Ecng.Web
{
	#region Using Directives
	
	using System;
	using System.Web;
	using System.IO;
	
	#endregion

	public class WebApplication : HttpApplication
	{
		protected virtual void Application_Start(object sender, EventArgs e)
		{
			OnAppStart(e);
		}

		protected virtual void Application_End(object sender, EventArgs e)
		{
			OnAppEnd(e);
		}

		protected virtual void Application_Init(object sender, EventArgs e)
		{
			OnAppInit(e);
		}

		protected virtual void Application_Disposed(object sender, EventArgs e)
		{
			OnAppDisposed(e);
		}

		protected virtual void Application_BeginRequest(object sender, EventArgs e)
		{
			OnAppBeginRequest(e);
		}

		protected virtual void Application_EndRequest(object sender, EventArgs e)
		{
			OnAppEndRequest(e);
		}

		protected virtual void Application_AuthenticateRequest(object sender, EventArgs e)
		{
			OnAppAuthenticateRequest(e);
		}

		protected virtual void Application_AuthorizeRequest(object sender, EventArgs e)
		{
			OnAppAuthorizeRequest(e);
		}

		protected virtual void Application_Error(object sender, EventArgs e)
		{
			OnError(new ErrorEventArgs(Context.Error));
		}

		protected virtual void Session_Start(object sender, EventArgs e)
		{
			OnSessionStart(e);
		}

		protected virtual void Session_End(object sender, EventArgs e)
		{
			OnSessionEnd(e);
		}

		protected virtual void OnAppStart(EventArgs e)
		{
		}

		protected virtual void OnAppEnd(EventArgs e)
		{
		}

		protected virtual void OnAppInit(EventArgs e)
		{
		}

		protected virtual void OnAppDisposed(EventArgs e)
		{
		}

		protected virtual void OnAppBeginRequest(EventArgs e)
		{
		}

		protected virtual void OnAppEndRequest(EventArgs e)
		{
		}

		protected virtual void OnAppAuthenticateRequest(EventArgs e)
		{
		}

		protected virtual void OnAppAuthorizeRequest(EventArgs e)
		{
		}

		protected virtual void OnSessionStart(EventArgs e)
		{
		}

		protected virtual void OnSessionEnd(EventArgs e)
		{
		}

		protected virtual void OnError(ErrorEventArgs e)
		{
		}
	}
}