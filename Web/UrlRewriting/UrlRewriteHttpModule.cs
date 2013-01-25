namespace Ecng.Web.UrlRewriting
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Web;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Web.Configuration;

	#endregion

	public class UrlRewritingHttpModule : IHttpModule
	{
		private sealed class FormActionFilter : Stream
		{
			#region Private Fields

			private readonly static Regex _regex = new Regex("<form name=\"aspnetForm\" method=\"post\" action=\"([^\"]*)\"([^>]*)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

			private readonly Stream _innerStream;
			
			#endregion

			#region FormActionFilter.ctor()

			public FormActionFilter(Stream stream)
			{
				_innerStream = stream;
			}

			#endregion

			#region Stream Members

			public override bool CanRead
			{
				get { return _innerStream.CanRead; }
			}

			public override bool CanSeek
			{
				get { return _innerStream.CanSeek; }
			}

			public override bool CanWrite
			{
				get { return _innerStream.CanWrite; }
			}

			public override long Length
			{
				get { return _innerStream.Length; }
			}

			public override long Position
			{
				get { return _innerStream.Position; }
				set { _innerStream.Position = value; }
			}

			public override void Close()
			{
				_innerStream.Close();
			}

			public override void Flush()
			{
				_innerStream.Flush();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return _innerStream.Read(buffer, offset, count);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return _innerStream.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				_innerStream.SetLength(value);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				string textBuffer = Encoding.UTF8.GetString(buffer, offset, count);
				//string url = HttpContext.Current.Request.Headers[RewriteUrlHeader];
				
				if (_regex.IsMatch(textBuffer))
				{
					textBuffer = _regex.Replace(textBuffer, "<form name=\"aspnetForm\" method=\"post\" action=\"" + RewritePath + "\"$2>");
					var response = Encoding.UTF8.GetBytes(textBuffer);
					_innerStream.Write(response, 0, response.Length);
				}
				else
				{
					_innerStream.Write(buffer, offset, count);
				}
			}

			#endregion
		}

		#region Private Fields

		private readonly static List<UrlRewritingRule> _rules = new List<UrlRewritingRule>();

		#endregion

		//public const string OriginalUrlHeader = "X-Original-Url";
		//public const string RewriteUrlHeader = "X-Rewrite-Url";

		[ThreadStatic]
		public static string OriginalPath;

		[ThreadStatic]
		public static string RewritePath;

		#region UrlRewritingHttpModule.cctor()

		static UrlRewritingHttpModule()
		{
			var webSection = ConfigManager.GetSection<WebSection>();

			foreach (ProviderSettings settings in webSection.UrlRewritingRules)
			{
				var rule = settings.Type.To<Type>().CreateInstance<UrlRewritingRule>();
				rule.Initialize(settings.Name, settings.Parameters);
				_rules.Add(rule);
			}
		}

		#endregion

		#region IHttpModule Members

		void IHttpModule.Init(HttpApplication context)
		{
			context.BeginRequest += delegate
			{
				foreach (var rule in _rules.Where(rule => rule.IsCompatible(context.Request.Path)))
				{
					OriginalPath = context.Request.RawUrl;
					context.Context.RewritePath(rule.TransformPath(context.Request.Path), false);
					RewritePath = context.Request.RawUrl;
					break;
				}
			};

			context.PostReleaseRequestState += delegate
			{
				if (context.Response.ContentType == "text/html" && !RewritePath.IsEmpty())
					context.Response.Filter = new FormActionFilter(context.Response.Filter);
			};
		}

		void IHttpModule.Dispose()
		{
		}

		#endregion
	}
}