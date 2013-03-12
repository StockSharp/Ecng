namespace Ecng.Web
{
	using System;
	using System.Web;
	using System.IO;

	public class WebApplication : HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			OnAppStart(e);
		}

		protected void Application_End(object sender, EventArgs e)
		{
			OnAppEnd(e);
		}

		protected void Application_Init(object sender, EventArgs e)
		{
			OnAppInit(e);
		}

		protected void Application_Disposed(object sender, EventArgs e)
		{
			OnAppDisposed(e);
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{
			OnAppBeginRequest(e);
		}

		protected void Application_EndRequest(object sender, EventArgs e)
		{
			OnAppEndRequest(e);
		}

		protected void Application_AuthenticateRequest(object sender, EventArgs e)
		{
			OnAppAuthenticateRequest(e);
		}

		protected void Application_AuthorizeRequest(object sender, EventArgs e)
		{
			OnAppAuthorizeRequest(e);
		}

		protected void Application_Error(object sender, EventArgs e)
		{
			OnAppError(new ErrorEventArgs(Context.Error));
		}

		protected void Application_AcquireRequestState(object sender, EventArgs e)
		{
			OnAppAcquireRequestState(e);
		}

		protected void Application_PostAcquireRequestState(object sender, EventArgs e)
		{
			OnAppPostAcquireRequestState(e);
		}

		protected void Session_Start(object sender, EventArgs e)
		{
			OnSessionStart(e);
		}

		protected void Session_End(object sender, EventArgs e)
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

		protected virtual void OnAppAcquireRequestState(EventArgs e)
		{
		}

		protected virtual void OnAppPostAcquireRequestState(EventArgs e)
		{
		}

		protected virtual void OnAppError(ErrorEventArgs e)
		{
		}

		protected virtual void OnSessionStart(EventArgs e)
		{
		}

		protected virtual void OnSessionEnd(EventArgs e)
		{
		}
	}
}