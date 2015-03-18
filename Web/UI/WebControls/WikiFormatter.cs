namespace Ecng.Web.UI.WebControls
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Web;

	using Ecng.Common;

	/// <summary>
	/// Performs all the text formatting and parsing operations.
	/// </summary>
	public static class WikiFormatter
	{
		private static readonly Regex _noWiki = new Regex(@"\<nowiki\>(.|\n|\r)+?\<\/nowiki\>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex _link = new Regex(@"(\[\[.+?\]\])|(\[.+?\])", RegexOptions.Compiled);
		//private static readonly Regex _redirection = new Regex(@"^\ *\>\>\>\ *.+\ *$", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex _h1 = new Regex(@"^==.+?==\n?", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex _h2 = new Regex(@"^===.+?===\n?", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex _h3 = new Regex(@"^====.+?====\n?", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex _h4 = new Regex(@"^=====.+?=====\n?", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex _bold = new Regex(@"'''.+?'''", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _italic = new Regex(@"''.+?''", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _boldItalic = new Regex(@"'''''.+?'''''", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _underlined = new Regex(@"__.+?__", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _big = new Regex(@"\<bg\>.+?\<\/bg\>", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _small = new Regex(@"\<sm\>.+?\<\/sm\>", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _sup = new Regex(@"\^\^.+?\^\^", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _sub = new Regex(@"vv.+?vv", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _quote = new Regex(@"\<q\>.+?\<\/q\>", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _striked = new Regex(@"(?<!\<\!)(\-\-(?!\>).+?\-\-)(?!\>)", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _code = new Regex(@"\{\{.+?\}\}", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _pre = new Regex(@"\{\{\{\{.+?\}\}\}\}", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _box = new Regex(@"\(\(\(.+?\)\)\)", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _specialTag = new Regex(@"\{(wikititle|wikiversion|mainurl|up|rsspage|themepath|clear|br|top|searchbox|ftsearchbox|pagecount)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		//private static readonly Regex _cloud = new Regex(@"\{cloud\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex _list = new Regex(@"(?<=(\n|^))((\*|\#)+(\ )?.+?\n)+", RegexOptions.Compiled);
		//private static readonly Regex _toc = new Regex(@"\{toc\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex _transclusion = new Regex(@"\{T(\:|\|).+}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex _hr = new Regex(@"(?<=(\n|^))(\ )*----(\ )*\n", RegexOptions.Compiled);
		private static readonly Regex _snippet = new Regex(@"\{S(\:|\|)(.+?)(\|(.+?))*}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex _table = new Regex(@"\{\|(\ [^\n]*)?\n.+?\|\}", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _indent = new Regex(@"(?<=(\n|^))\:+(\ )?.+?\n", RegexOptions.Compiled);
		private static readonly Regex _esc = new Regex(@"\<esc\>(.|\n|\r)*?\<\/esc\>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		//private static readonly Regex _sign = new Regex(@"§§\(.+?\)§§", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex _fullCode = new Regex(@"@@.+?@@", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex _username = new Regex(@"\{username\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex _javascript = new Regex(@"\<script.*?\>.*?\<\/script\>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
		private static readonly Regex _comment = new Regex(@"(?<!(\<script.*?\>[\s\n]*))\<\!\-\-.*?\-\-\>(?!([\s\n]*\<\/script\>))", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

		private static readonly Dictionary<string, RefPair<Url, string>> _imageUrls = new Dictionary<string, RefPair<Url, string>>();

		public static void AddImageUrl(string code, Url url, string altText)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException("code");

			if (url == null)
				throw new ArgumentNullException("url");

			if (altText.IsEmpty())
				throw new ArgumentNullException("altText");

			_imageUrls.Add("[" + code + "]", Ref.Create(url, altText));
		}

		//private static string tocTitlePlaceHolder = "%%%%TocTitlePlaceHolder%%%%";
		//private static string editSectionPlaceHolder = "%%%%EditSectionPlaceHolder%%%%"; // This string is also used in History.aspx.cs
		//private static string _upReplacement = "GetFile.aspx?File=";

		/// <summary>
		/// Formats WikiMarkup, converting it into XHTML.
		/// </summary>
		/// <param name="raw">The raw WikiMarkup text.</param>
		/// <returns>The formatted text.</returns>
		public static string Format(string raw)
		{
			//linkedPages = new PageInfo[0];
			//List<PageInfo> lp = new List<PageInfo>();

			var sb = new StringBuilder(raw);
			string tmp, a, n, url, title, bigUrl;
			StringBuilder dummy; // Used for temporary string manipulation inside formatting cycles
			bool done;
			List<int> noWikiBegin = new List<int>(), noWikiEnd = new List<int>();
			int end;
			//List<HPosition> hPos = new List<HPosition>();

			sb.Replace("\r", "");
			bool addedNewLineAtEnd = false;
			if (!sb.ToString().EndsWith("\n"))
			{
				sb.Append("\n"); // Very important to make Regular Expressions work!
				addedNewLineAtEnd = true;
			}

			// Remove all double-LF in JavaScript tags
			Match match = _javascript.Match(sb.ToString());
			while (match.Success)
			{
				sb.Remove(match.Index, match.Length);
				sb.Insert(match.Index, match.Value.Replace("\n\n", "\n"));
				match = _javascript.Match(sb.ToString(), match.Index + 1);
			}

			// Strip out all comments
			match = _comment.Match(sb.ToString());
			while (match.Success)
			{
				sb.Remove(match.Index, match.Length);
				match = _comment.Match(sb.ToString(), match.Index + 1);
			}

			ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);

			//if (current != null)
			//{
			//    // Check redirection
			//    match = redirection.Match(sb.ToString());
			//    if (match.Success)
			//    {
			//        if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
			//        {
			//            sb.Remove(match.Index, match.Length);
			//            string destination = match.Value.Trim().Substring(4).Trim();
			//            while (destination.StartsWith("[") && destination.EndsWith("]"))
			//            {
			//                destination = destination.Substring(1, destination.Length - 2);
			//            }
			//            while (sb[match.Index] == '\n' && match.Index < sb.Length - 1) sb.Remove(match.Index, 1);
			//            PageInfo dest = Pages.Instance.FindPage(destination);
			//            if (dest != null)
			//            {
			//                Redirections.Instance.AddRedirection(current, dest);
			//            }
			//        }
			//        ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
			//    }
			//}

			// Before Producing HTML
			match = _fullCode.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					string content = match.Value.Substring(2, match.Length - 4);
					dummy = new StringBuilder();
					dummy.Append("<pre>");
					// IE needs \r\n for line breaks
					dummy.Append(EscapeWikiMarkup(content).Replace("\n", "\r\n"));
					dummy.Append("</pre>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _fullCode.Match(sb.ToString(), end);
			}

			// No more needed (Striked Regex modified)
			// Temporarily "escape" comments
			//sb.Replace("<!--", "($_^)");
			//sb.Replace("-->", "(^_$)");

			ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);

			// Before Producing HTML
			match = _esc.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, match.Value.Substring(5, match.Length - 11).Replace("<", "&lt;").Replace(">", "&gt;"));
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _esc.Match(sb.ToString(), end);
			}

			// Snippets were here
			// Moved to solve problems with lists and tables

			match = _table.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, BuildTable(match.Value));
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _table.Match(sb.ToString(), end);
			}

			match = _indent.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, BuildIndent(match.Value) + "\n");
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _indent.Match(sb.ToString(), end);
			}

			match = _specialTag.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					switch (match.Value.Substring(1, match.Value.Length - 2).ToUpperInvariant())
					{
						//case "WIKITITLE":
						//    sb.Insert(match.Index, Settings.WikiTitle);
						//    break;
						//case "WIKIVERSION":
						//    sb.Insert(match.Index, Settings.WikiVersion);
						//    break;
						//case "MAINURL":
						//    sb.Insert(match.Index, Settings.MainUrl);
						//	break;
						//case "UP":
						//    sb.Insert(match.Index, _upReplacement);
						//    break;
						//case "RSSPAGE":
						//    if (current != null)
						//    {
						//        sb.Insert(match.Index, @"<a href=""RSS.aspx?Page=" + Tools.UrlEncode(current.Name) + @""" title=""" + Exchanger.ResourceExchanger.GetResource("RssForThisPage") + @"""><img src=""" + Settings.ThemePath + @"Images/RSS.png"" alt=""RSS"" /></a>");
						//    }
						//    break;
						//case "THEMEPATH":
						//    sb.Insert(match.Index, Settings.ThemePath);
						//    break;
						case "CLEAR":
							sb.Insert(match.Index, @"<div style=""clear: both;""></div>");
							break;
						case "BR":
							//if(!AreSingleLineBreaksToBeProcessed()) sb.Insert(match.Index, "<br />");
							sb.Insert(match.Index, "<br />");
							break;
						//case "TOP":
						//    sb.Insert(match.Index, @"<a href=""#PageTop"">" + Exchanger.ResourceExchanger.GetResource("Top") + "</a>");
						//    break;
						//case "SEARCHBOX":
						//    sb.Insert(match.Index, @"<nowiki><input type=""text"" id=""TxtSearchBox"" onkeydown=""javascript:var keycode; if(window.event) keycode = event.keyCode; else keycode = event.which; if(keycode == 10 || keycode == 13) { document.location = 'Search.aspx?Query=' + encodeURI(document.getElementById('TxtSearchBox').value); return false; }"" /></nowiki>");
						//    break;
						//case "FTSEARCHBOX":
						//    sb.Insert(match.Index, @"<nowiki><input type=""text"" id=""TxtSearchBox"" onkeydown=""javascript:var keycode; if(window.event) keycode = event.keyCode; else keycode = event.which; if(keycode == 10 || keycode == 13) { document.location = 'Search.aspx?FullText=1&amp;Query=' + encodeURI(document.getElementById('TxtSearchBox').value); return false; }"" /></nowiki>");
						//    break;
						//case "PAGECOUNT":
						//    sb.Insert(match.Index, Pages.Instance.AllPages.Count.ToString());
						//    break;
					}
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _specialTag.Match(sb.ToString(), end);
			}

			match = _list.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					int d = 0;
					try
					{
						sb.Insert(match.Index, GenerateList(match.Value.Split(new [] { '\n' }, StringSplitOptions.RemoveEmptyEntries), 0, 0, ref d) + "\n");
					}
					catch
					{
						sb.Insert(match.Index, @"<b style=""color: #FF0000;"">FORMATTER ERROR (Malformed List)</b>");
					}
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _list.Match(sb.ToString(), end);
			}

			match = _hr.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, @"<h1 style=""border-bottom: solid 1px #999999;""> </h1>" + "\n");
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _hr.Match(sb.ToString(), end);
			}

			// Replace \n with BR was here

			ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);

			// Transclusion (intra-Wiki)
			match = _transclusion.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					//PageInfo info = Pages.Instance.FindPage(match.Value.Substring(3, match.Value.Length - 4));
					//if (info != null && info != current)
					//{ // Avoid circular transclusion!
					//    dummy = new StringBuilder();
					//    dummy.Append(@"<div class=""transcludedpage"">");
					//    // The current PageInfo is null to disable section editing and similar features
					//    dummy.Append(FormattingPipeline.FormatWithPhase1And2(Content.GetPageContent(info, true).Content, null));
					//    dummy.Append("</div>");
					//    sb.Insert(match.Index, dummy.ToString());
					//}
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _transclusion.Match(sb.ToString(), end);
			}

			//var attachments = new List<string>();

			// Links and images
			match = _link.Match(sb.ToString());
			while (match.Success)
			{
				if (IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					match = _link.Match(sb.ToString(), end);
					continue;
				}

				// [], [[]] and [[] can occur when empty links are processed
				if (match.Value.Equals("[]") || match.Value.Equals("[[]]") || match.Value.Equals("[[]"))
				{
					end += 2;
					match = _link.Match(sb.ToString(), end);
					continue; // Prevents formatting emtpy links
				}

				if (_imageUrls.ContainsKey(match.Value))
				{
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, "[image|{1}|{0}]".Put(_imageUrls[match.Value].First, _imageUrls[match.Value].Second));
					match = _link.Match(sb.ToString());
					//continue;
				}

				done = false;
				tmp = match.Value.StartsWith("[[") ? match.Value.Substring(2, match.Length - 4).Trim() : match.Value.Substring(1, match.Length - 2).Trim();
				sb.Remove(match.Index, match.Length);
				a = "";
				n = "";
				if (tmp.IndexOf("|") != -1)
				{
					// There are some fields
					string[] fields = tmp.Split('|');
					if (fields.Length == 2)
					{
						// Link with title
						a = fields[0];
						n = fields[1];
					}
					else
					{
						var img = new StringBuilder();
						// Image
						if (fields[0].ToLowerInvariant().Equals("imageleft") || fields[0].ToLowerInvariant().Equals("imageright") || fields[0].ToLowerInvariant().Equals("imageauto"))
						{
							string c;
							switch (fields[0].ToLowerInvariant())
							{
								case "imageleft":
									c = "imageleft";
									break;
								case "imageright":
									c = "imageright";
									break;
								case "imageauto":
									c = "imageauto";
									break;
								default:
									c = "image";
									break;
							}
							title = fields[1];
							url = fields[2];
							bigUrl = fields.Length == 4 ? fields[3] : "";
							url = EscapeUrl(url);
							// bigUrl = EscapeUrl(bigUrl); The url is already escaped by BuildUrl
							if (c.Equals("imageauto"))
							{
								img.Append(@"<table class=""imageauto"" cellpadding=""0"" cellspacing=""0"" align=""center""><tr><td>");
							}
							else
							{
								img.Append(@"<div class=""");
								img.Append(c);
								img.Append(@""">");
							}
							if (bigUrl.Length > 0)
							{
								dummy = new StringBuilder();
								dummy.Append(@"<img class=""image"" src=""");
								dummy.Append(url);
								dummy.Append(@""" alt=""");
								if (title.Length > 0) dummy.Append(StripWikiMarkup(StripHtml(title.TrimStart('#'))));
								//else dummy.Append(Exchanger.ResourceExchanger.GetResource("Image"));
								dummy.Append(@""" />");
								img.Append(BuildLink(bigUrl, dummy.ToString(), true, title));
							}
							else
							{
								img.Append(@"<img class=""image"" src=""");
								img.Append(url);
								img.Append(@""" alt=""");
								if (title.Length > 0) img.Append(StripWikiMarkup(StripHtml(title.TrimStart('#'))));
								//else img.Append(Exchanger.ResourceExchanger.GetResource("Image"));
								img.Append(@""" />");
							}
							if (title.Length > 0 && !title.StartsWith("#"))
							{
								img.Append(@"<p class=""imagedescription"">");
								img.Append(title);
								img.Append("</p>");
							}
							if (c.Equals("imageauto"))
							{
								img.Append("</td></tr></table>");
							}
							else
							{
								img.Append("</div>");
							}
							sb.Insert(match.Index, img);
						}
						else if (fields[0].ToLowerInvariant().Equals("image"))
						{
							title = fields[1];
							url = fields[2];
							bigUrl = fields.Length == 4 ? fields[3] : "";
							url = EscapeUrl(url);
							// bigUrl = EscapeUrl(bigUrl); The url is already escaped by BuildUrl
							if (bigUrl.Length > 0)
							{
								dummy = new StringBuilder();
								dummy.Append(@"<img src=""");
								dummy.Append(url);
								dummy.Append(@""" alt=""");
								if (title.Length > 0) dummy.Append(StripWikiMarkup(StripHtml(title.TrimStart('#'))));
								//else dummy.Append(Exchanger.ResourceExchanger.GetResource("Image"));
								dummy.Append(@""" />");
								img.Append(BuildLink(bigUrl, dummy.ToString(), true, title));
							}
							else
							{
								img.Append(@"<img src=""");
								img.Append(url);
								img.Append(@""" alt=""");
								if (title.Length > 0) img.Append(StripWikiMarkup(StripHtml(title.TrimStart('#'))));
								//else img.Append(Exchanger.ResourceExchanger.GetResource("Image"));
								img.Append(@""" />");
							}
							sb.Insert(match.Index, img.ToString());
						}
						else
						{
							sb.Insert(match.Index, @"<b style=""color: #FF0000;"">FORMATTER ERROR (Malformed Image Tag)</b>");
						}
						done = true;
					}
				}
				else if (tmp.ToLowerInvariant().StartsWith("attachment:"))
				{
					// This is an attachment
					done = true;
					//string f = tmp.Substring("attachment:".Length);
					//if (f.StartsWith("{up}"))
					//    f = f.Substring(4);
					//if (f.ToLowerInvariant().StartsWith(_upReplacement.ToLowerInvariant()))
					//    f = f.Substring(_upReplacement.Length);
					//attachments.Add(HttpContext.Current.Server.UrlDecode(f));
					// Remove all trailing \n, so that attachments have no effect on the output in any case
					while (sb[match.Index] == '\n' && match.Index < sb.Length - 1)
					{
						sb.Remove(match.Index, 1);
					}
				}
				else
				{
					a = tmp;
					n = "";
				}
				if (!done)
				{
					sb.Insert(match.Index, BuildLink(a, n, false, ""));
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _link.Match(sb.ToString(), end);
			}

			match = _boldItalic.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<b><i>");
					dummy.Append(match.Value.Substring(5, match.Value.Length - 10));
					dummy.Append("</i></b>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _boldItalic.Match(sb.ToString(), end);
			}

			match = _bold.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<b>");
					dummy.Append(match.Value.Substring(3, match.Value.Length - 6));
					dummy.Append("</b>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _bold.Match(sb.ToString(), end);
			}

			match = _italic.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<i>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4));
					dummy.Append("</i>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _italic.Match(sb.ToString(), end);
			}

			match = _underlined.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<u>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4));
					dummy.Append("</u>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _underlined.Match(sb.ToString(), end);
			}

			match = _big.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<big>");
					dummy.Append(match.Value.Substring(4, match.Value.Length - 9));
					dummy.Append("</big>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _big.Match(sb.ToString(), end);
			}

			match = _small.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<small>");
					dummy.Append(match.Value.Substring(4, match.Value.Length - 9));
					dummy.Append("</small>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _small.Match(sb.ToString(), end);
			}

			match = _sup.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<sup>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4));
					dummy.Append("</sup>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _sup.Match(sb.ToString(), end);
			}

			match = _sub.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<sub>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4));
					dummy.Append("</sub>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _sub.Match(sb.ToString(), end);
			}

			match = _quote.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<tt>");
					dummy.Append(match.Value.Substring(3, match.Value.Length - 7));
					dummy.Append("</tt>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _quote.Match(sb.ToString(), end);
			}

			match = _striked.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<strike>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4));
					dummy.Append("</strike>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _striked.Match(sb.ToString(), end);
			}

			match = _pre.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<pre>");
					// IE needs \r\n for line breaks
					dummy.Append(match.Value.Substring(4, match.Value.Length - 8).Replace("\n", "\r\n"));
					dummy.Append("</pre>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _pre.Match(sb.ToString(), end);
			}

			match = _code.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<code>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4).Replace("\n", "\r\n"));
					dummy.Append("</code>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _code.Match(sb.ToString(), end);
			}

			string h;

			// Hx: detection pass (used for the TOC generation and section editing)
			//hPos = DetectHeaders(sb.ToString());

			// Hx: formatting pass

			int count = 0;

			match = _h4.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					h = match.Value.Substring(5, match.Value.Length - 10 - (match.Value.EndsWith("\n") ? 1 : 0));
					dummy = new StringBuilder();
					dummy.Append(@"<a id=""");
					dummy.Append(BuildHAnchor(h, count.ToString()));
					dummy.Append(@"""></a><h4>");
					dummy.Append(h);
					dummy.Append("</h4>");
					sb.Insert(match.Index, dummy.ToString());
					count++;
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _h4.Match(sb.ToString(), end);
			}

			match = _h3.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					h = match.Value.Substring(4, match.Value.Length - 8 - (match.Value.EndsWith("\n") ? 1 : 0));
					dummy = new StringBuilder();
					//if (current != null) dummy.Append(BuildEditSectionLink(count, current.Name));
					dummy.Append(@"<a id=""");
					dummy.Append(BuildHAnchor(h, count.ToString()));
					dummy.Append(@"""></a><h3 class=""separator"">");
					dummy.Append(h);
					dummy.Append("</h3>");
					sb.Insert(match.Index, dummy.ToString());
					count++;
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _h3.Match(sb.ToString(), end);
			}

			match = _h2.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					h = match.Value.Substring(3, match.Value.Length - 6 - (match.Value.EndsWith("\n") ? 1 : 0));
					dummy = new StringBuilder();
					//if (current != null) dummy.Append(BuildEditSectionLink(count, current.Name));
					dummy.Append(@"<a id=""");
					dummy.Append(BuildHAnchor(h, count.ToString()));
					dummy.Append(@"""></a><h2 class=""separator"">");
					dummy.Append(h);
					dummy.Append("</h2>");
					sb.Insert(match.Index, dummy.ToString());
					count++;
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _h2.Match(sb.ToString(), end);
			}

			match = _h1.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					h = match.Value.Substring(2, match.Value.Length - 4 - (match.Value.EndsWith("\n") ? 1 : 0));
					dummy = new StringBuilder();
					//if (current != null) dummy.Append(BuildEditSectionLink(count, current.Name));
					dummy.Append(@"<a id=""");
					dummy.Append(BuildHAnchor(h, count.ToString()));
					dummy.Append(@"""></a><h1 class=""separator"">");
					dummy.Append(h);
					dummy.Append("</h1>");
					sb.Insert(match.Index, dummy.ToString());
					count++;
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _h1.Match(sb.ToString(), end);
			}

			match = _box.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder(@"<div class=""box"">");
					dummy.Append(match.Value.Substring(3, match.Value.Length - 6));
					dummy.Append("</div>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _box.Match(sb.ToString(), end);
			}

			// "Disable" NoWiki'ed {Username} tags
			match = _username.Match(sb.ToString());
			while (match.Success)
			{
				if (IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, match.Value.Replace("{", "&#0123;").Replace("}", "&#0125;"));
				}
				else end = match.Index + match.Length + 1;
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _username.Match(sb.ToString(), end);
			}

			//if (current != null)
			//{
			//    string tocString = BuildToc(hPos);
			//    match = toc.Match(sb.ToString());
			//    while (match.Success)
			//    {
			//        if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
			//        {
			//            sb.Remove(match.Index, match.Length);
			//            sb.Insert(match.Index, tocString);
			//        }
			//        ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
			//        match = toc.Match(sb.ToString(), end);
			//    }
			//}

			match = _snippet.Match(sb.ToString());
			while (match.Success)
			{
				if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
				{
					sb.Remove(match.Index, match.Length);
					//int secondPipe = match.Value.Substring(3).IndexOf("|");
					//string name = "";
					//if (secondPipe == -1) name = match.Value.Substring(3, match.Length - 4);
					//else name = match.Value.Substring(3, secondPipe);
					//Snippet s = Snippets.Instance.Find(name);
					//if (s != null)
					//{
					//    string[] parameters = match.Value.Substring(3 + secondPipe + 1, match.Length - secondPipe - 5).Split('|');
					//    string fs = Format(PrepareSnippet(parameters, s.Content), null);
					//    // Format appends a "<br />", which needs to be removed
					//    if (fs.ToLowerInvariant().EndsWith("<br />"))
					//    {
					//        fs = fs.Substring(0, fs.Length - 6);
					//    }
					//    sb.Insert(match.Index, fs.Trim('\n'));
					//}
					//else
					//{
					//	sb.Insert(match.Index, @"<b style=""color: #FF0000;"">FORMATTER ERROR (Snippet Not Found)</b>");
					//}
				}
				ComputeNoWiki(sb.ToString(), noWikiBegin, noWikiEnd);
				match = _snippet.Match(sb.ToString(), end);
			}

			// Remove <nowiki> tags
			sb.Replace("<nowiki>", "");
			sb.Replace("</nowiki>", "");

			ProcessLineBreaks(sb);

			if (addedNewLineAtEnd)
			{
				if (sb.ToString().EndsWith("<br />")) sb.Remove(sb.Length - 6, 6);
			}

			// Append Attachments
			//if (attachments.Count > 0)
			//{
			//    sb.Append(@"<div id=""AttachmentsDiv"">");
			//    for (int i = 0; i < attachments.Count; i++)
			//    {
			//        sb.Append(@"<a href=""");
			//        sb.Append(_upReplacement);
			//        sb.Append(attachments[i]);
			//        sb.Append(@""" class=""attachment"">");
			//        sb.Append(attachments[i]);
			//        sb.Append("</a>");
			//        if (i != attachments.Count - 1) sb.Append(" - ");
			//    }
			//    sb.Append("</div>");
			//}

			//linkedPages = lp.ToArray();

			return sb.ToString();
		}

		private static void ProcessLineBreaks(StringBuilder sb)
		{
			if (AreSingleLineBreaksToBeProcessed()) sb.Replace("\n", "<br />");
			else sb.Replace("\n\n", "<br /><br />");
			sb.Replace("<br>", "<br />");

			// BR Hacks
			sb.Replace("</ul><br /><br />", "</ul><br />");
			sb.Replace("</ol><br /><br />", "</ol><br />");
			sb.Replace("</table><br /><br />", "</table><br />");
			sb.Replace("</pre><br /><br />", "</pre><br />");
			if (AreSingleLineBreaksToBeProcessed())
			{
				sb.Replace("</h1><br />", "</h1>");
				sb.Replace("</h2><br />", "</h2>");
				sb.Replace("</h3><br />", "</h3>");
				sb.Replace("</h4><br />", "</h4>");
				sb.Replace("</h5><br />", "</h5>");
				sb.Replace("</h6><br />", "</h6>");
				sb.Replace("</div><br />", "</div>");
			}
			else
			{
				sb.Replace("</div><br /><br />", "</div><br />");
			}
		}

		/// <summary>
		/// Gets a value indicating whether or not to process single line breaks.
		/// </summary>
		/// <returns><b>True</b> if SLB are to be processed, <b>false</b> otherwise.</returns>
		private static bool AreSingleLineBreaksToBeProcessed()
		{
			//return Settings.ProcessSingleLineBreaks;
			return false;
		}

		///// <summary>
		///// Prepares the content of a snippet, properly managing parameters.
		///// </summary>
		///// <param name="parameters">The snippet parameters.</param>
		///// <param name="snippet">The snippet original text.</param>
		///// <returns>The prepared snippet text.</returns>
		//private static string PrepareSnippet(string[] parameters, string snippet)
		//{
		//    StringBuilder sb = new StringBuilder(snippet);

		//    for (int i = 0; i < parameters.Length; i++)
		//    {
		//        sb.Replace(string.Format("?{0}?", i + 1), parameters[i]);
		//    }

		//    // Remove all remaining parameters that have no value
		//    Regex regex = new Regex("\\?[0-9]+\\?");
		//    return regex.Replace(sb.ToString(), "");
		//}

		/// <summary>
		/// Escapes all the characters used by the WikiMarkup.
		/// </summary>
		/// <param name="content">The Content.</param>
		/// <returns>The escaped Content.</returns>
		private static string EscapeWikiMarkup(string content)
		{
			var sb = new StringBuilder(content);
			sb.Replace("&", "&amp;"); // Before all other escapes!
			sb.Replace("#", "&#0035;");
			sb.Replace("*", "&#0042;");
			sb.Replace("<", "&lt;");
			sb.Replace(">", "&gt;");
			sb.Replace("[", "&#0091;");
			sb.Replace("]", "&#0093;");
			sb.Replace("{", "&#0123;");
			sb.Replace("}", "&#0125;");
			sb.Replace("'''", "&#0039;&#0039;&#0039;");
			sb.Replace("''", "&#0039;&#0039;");
			sb.Replace("=====", "&#0061;&#0061;&#0061;&#0061;&#0061;");
			sb.Replace("====", "&#0061;&#0061;&#0061;&#0061;");
			sb.Replace("===", "&#0061;&#0061;&#0061;");
			sb.Replace("==", "&#0061;&#0061;");
			sb.Replace("§§", "&#0167;&#0167;");
			sb.Replace("__", "&#0095;&#0095;");
			sb.Replace("--", "&#0045;&#0045;");
			sb.Replace("@@", "&#0064;&#0064;");
			return sb.ToString();
		}

		/// <summary>
		/// Removes all the characters used by the WikiMarkup.
		/// </summary>
		/// <param name="content">The Content.</param>
		/// <returns>The stripped Content.</returns>
		private static string StripWikiMarkup(string content)
		{
			var sb = new StringBuilder(content);
			sb.Replace("*", "");
			sb.Replace("<", "");
			sb.Replace(">", "");
			sb.Replace("[", "");
			sb.Replace("]", "");
			sb.Replace("{", "");
			sb.Replace("}", "");
			sb.Replace("'''", "");
			sb.Replace("''", "");
			sb.Replace("=====", "");
			sb.Replace("====", "");
			sb.Replace("===", "");
			sb.Replace("==", "");
			sb.Replace("§§", "");
			sb.Replace("__", "");
			sb.Replace("--", "");
			sb.Replace("@@", "");
			return sb.ToString();
		}

		/// <summary>
		/// Removes all HTML markup from a string.
		/// </summary>
		/// <param name="content">The string.</param>
		/// <returns>The result.</returns>
		private static string StripHtml(string content)
		{
			var sb = new StringBuilder(Regex.Replace(content, "<[^>]*>", " "));
			sb.Replace("&nbsp;", "");
			sb.Replace("  ", " ");
			return sb.ToString();
		}

		/// <summary>
		/// Builds a Link.
		/// </summary>
		/// <param name="a">The (raw) HREF.</param>
		/// <param name="n">The name/title.</param>
		/// <param name="isImage">True if the link contains an Image as "visible content".</param>
		/// <param name="imageTitle">The title of the image.</param>
		/// <returns>The formatted Link.</returns>
		private static string BuildLink(string a, string n, bool isImage, string imageTitle)
		{
			// For an unknown reason, this method sometimes throws a NullReferenceException
			if (a == null) a = "";
			if (n == null) n = "";
			if (imageTitle == null) imageTitle = "";

			bool blank = false;
			if (a.StartsWith("^"))
			{
				blank = true;
				a = a.Substring(1);
			}
			a = EscapeUrl(a);
			string nstripped = StripWikiMarkup(StripHtml(n));
			string imageTitleStripped = StripWikiMarkup(StripHtml(imageTitle));

			var sb = new StringBuilder();

			if (a.ToLowerInvariant().Equals("anchor") && n.StartsWith("#"))
			{
				sb.Append(@"<a id=""");
				sb.Append(n.Substring(1));
				sb.Append(@"""></a>");
			}
			else if (a.StartsWith("#") || a.Contains("chrobreak.com") || a.Contains("localhost"))
			{
				sb.Append(@"<a");
				if (!isImage) sb.Append(@" class=""internallink""");
				if (blank) sb.Append(@" target=""_blank""");
				sb.Append(@" href=""");
				sb.Append(a);
				sb.Append(@""" title=""");
				if (!isImage && n.Length > 0) sb.Append(nstripped);
				else if (isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(a.Substring(1));
				sb.Append(@""">");
				if (n.Length > 0) sb.Append(n);
				else sb.Append(a.Substring(1));
				sb.Append("</a>");
			}
			else if (a.StartsWith("http://") || a.StartsWith("https://") || a.StartsWith("ftp://") || a.StartsWith("file://"))
			{
				// The link is complete
				sb.Append(@"<a");
				if (!isImage) sb.Append(@" class=""externallink""");
				sb.Append(@" href=""");
				sb.Append(a);
				sb.Append(@""" title=""");
				if (!isImage && n.Length > 0) sb.Append(nstripped);
				else if (isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(a);
				sb.Append(@""" target=""_blank"">");
				if (n.Length > 0) sb.Append(n);
				else sb.Append(a);
				sb.Append("</a>");
			}
			else if (a.StartsWith(@"\\") || a.StartsWith("//"))
			{
				// The link is a UNC path
				sb.Append(@"<a");
				if (!isImage) sb.Append(@" class=""externallink""");
				sb.Append(@" href=""file://///");
				sb.Append(a.Substring(2));
				sb.Append(@""" title=""");
				if (!isImage && n.Length > 0) sb.Append(nstripped);
				else if (isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(a);
				sb.Append(@""" target=""_blank"">");
				if (n.Length > 0) sb.Append(n);
				else sb.Append(a);
				sb.Append("</a>");
			}
			else if (a.IndexOf("@") != -1 && a.IndexOf(".") != -1)
			{
				// Email
				sb.Append(@"<a");
				if (!isImage) sb.Append(@" class=""emaillink""");
				if (blank) sb.Append(@" target=""_blank""");
				sb.Append(@" href=""mailto:");
				sb.Append(a.Replace("&amp;", "%26")); // Hack to let ampersands work in email addresses
				sb.Append(@""" title=""");
				if (!isImage && n.Length > 0) sb.Append(nstripped);
				else if (isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(a);
				sb.Append(@""">");
				if (n.Length > 0) sb.Append(n);
				else sb.Append(a);
				sb.Append("</a>");
			}
			else if (((a.IndexOf(".") != -1 && !a.ToLowerInvariant().EndsWith(".aspx")) || a.EndsWith("/")) && 
				!a.StartsWith("c:") && !a.StartsWith("C:"))
			{
				// Link to an internal file or subdirectory
				sb.Append(@"<a");
				if (!isImage) sb.Append(@" class=""internallink""");
				if (blank) sb.Append(@" target=""_blank""");
				sb.Append(@" href=""");
				sb.Append(a);
				sb.Append(@""" title=""");
				if (!isImage && n.Length > 0) sb.Append(nstripped);
				else if (isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(a);
				sb.Append(@""">");
				if (n.Length > 0) sb.Append(n);
				else sb.Append(a);
				sb.Append("</a>");
			}
			else
			{
				if (a.IndexOf(".aspx") != -1)
				{
					// The link points to a "system" page
					sb.Append(@"<a");
					if (!isImage) sb.Append(@" class=""systemlink""");
					if (blank) sb.Append(@" target=""_blank""");
					sb.Append(@" href=""");
					sb.Append(a);
					sb.Append(@""" title=""");
					if (!isImage && n.Length > 0) sb.Append(nstripped);
					else if (isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
					else sb.Append(a);
					sb.Append(@""">");
					if (n.Length > 0) sb.Append(n);
					else sb.Append(a);
					sb.Append("</a>");
				}
				else
				{
					if (a.StartsWith("c:") || a.StartsWith("C:"))
					{
						// Category link
						sb.Append(@"<a href=""AllPages.aspx?Cat=");
						sb.Append(HttpUtility.UrlEncode(a.Substring(2)));
						sb.Append(@""" title=""");
						if (!isImage && n.Length > 0) sb.Append(nstripped);
						else if (isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
						else sb.Append(a.Substring(2));
						sb.Append(@""">");
						if (n.Length > 0) sb.Append(n);
						else sb.Append(a.Substring(2));
						sb.Append("</a>");
					}
					else if (a.Contains(":") || a.ToLowerInvariant().Contains("%3a") || a.Contains("&") || a.Contains("%26"))
					{
						sb.Append(@"<b style=""color: #FF0000;"">FORMATTER ERROR ("":"" and ""&"" not supported in Page Names)</b>");
					}
					else
					{
						// The link points to a wiki page
						string tempLink = a;
						if (a.IndexOf("#") != -1)
						{
							tempLink = a.Substring(0, a.IndexOf("#"));
							a = HttpUtility.UrlEncode(a.Substring(0, a.IndexOf("#"))) + ".aspx" + a.Substring(a.IndexOf("#"));
						}
						else
						{
							a += ".aspx";
							a = HttpUtility.UrlEncode(a);
						}

						//PageInfo info = Pages.Instance.FindPage(tempLink);
						//if (info == null)
						//{
							sb.Append(@"<a");
							if (!isImage) sb.Append(@" class=""unknownlink""");
							if (blank) sb.Append(@" target=""_blank""");
							sb.Append(@" href=""");
							sb.Append(a);
							sb.Append(@""" title=""");
							if (!isImage && n.Length > 0) sb.Append(nstripped);
							else if (isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
							else sb.Append(tempLink);
							sb.Append(@""">");
							if (n.Length > 0) sb.Append(n);
							else sb.Append(tempLink);
							sb.Append("</a>");
						//}
						//else
						//{
						//    if (lp != null && !lp.Contains(info))
						//    {
						//        lp.Add(info);
						//    }
						//    sb.Append(@"<a");
						//    if (!isImage) sb.Append(@" class=""pagelink""");
						//    if (blank) sb.Append(@" target=""_blank""");
						//    sb.Append(@" href=""");
						//    sb.Append(a);
						//    sb.Append(@""" title=""");
						//    if (!isImage && n.Length > 0) sb.Append(nstripped);
						//    else if (isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
						//    else sb.Append(Content.GetPageContent(info, false).Title);
						//    sb.Append(@""">");
						//    if (n.Length > 0) sb.Append(n);
						//    else sb.Append(tempLink);
						//    sb.Append("</a>");
						//}
					}
				}
			}
			return sb.ToString();
		}

		///// <summary>
		///// Detects all the Headers in a block of text (H1, H2, H3, H4).
		///// </summary>
		///// <param name="text">The text.</param>
		///// <returns>The List of Header objects, in the same order as they are in the text.</returns>
		//public static List<HPosition> DetectHeaders(string text)
		//{
		//    Match match;
		//    string h;
		//    int end = 0;
		//    List<int> noWikiBegin = new List<int>(), noWikiEnd = new List<int>();
		//    List<HPosition> hPos = new List<HPosition>();
		//    StringBuilder sb = new StringBuilder(text);

		//    ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);

		//    int count = 0;

		//    match = _h4.Match(sb.ToString());
		//    while (match.Success)
		//    {
		//        if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
		//        {
		//            h = match.Value.Substring(5, match.Value.Length - 10 - (match.Value.EndsWith("\n") ? 1 : 0));
		//            hPos.Add(new HPosition(match.Index, h, 4, count));
		//            end = match.Index + match.Length;
		//            count++;
		//        }
		//        ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
		//        match = _h4.Match(sb.ToString(), end);
		//    }

		//    match = _h3.Match(sb.ToString());
		//    while (match.Success)
		//    {
		//        if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
		//        {
		//            h = match.Value.Substring(4, match.Value.Length - 8 - (match.Value.EndsWith("\n") ? 1 : 0));
		//            bool found = false;
		//            for (int i = 0; i < hPos.Count; i++)
		//            {
		//                if (match.Index == hPos[i].Index)
		//                {
		//                    found = true;
		//                    break;
		//                }
		//            }
		//            if (!found)
		//            {
		//                hPos.Add(new HPosition(match.Index, h, 3, count));
		//                count++;
		//            }
		//            end = match.Index + match.Length;
		//        }
		//        ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
		//        match = _h3.Match(sb.ToString(), end);
		//    }

		//    match = _h2.Match(sb.ToString());
		//    while (match.Success)
		//    {
		//        if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
		//        {
		//            h = match.Value.Substring(3, match.Value.Length - 6 - (match.Value.EndsWith("\n") ? 1 : 0));
		//            bool found = false;
		//            for (int i = 0; i < hPos.Count; i++)
		//            {
		//                if (match.Index == hPos[i].Index)
		//                {
		//                    found = true;
		//                    break;
		//                }
		//            }
		//            if (!found)
		//            {
		//                hPos.Add(new HPosition(match.Index, h, 2, count));
		//                count++;
		//            }
		//            end = match.Index + match.Length;
		//        }
		//        ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
		//        match = _h2.Match(sb.ToString(), end);
		//    }

		//    match = _h1.Match(sb.ToString());
		//    while (match.Success)
		//    {
		//        if (!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
		//        {
		//            h = match.Value.Substring(2, match.Value.Length - 4 - (match.Value.EndsWith("\n") ? 1 : 0));
		//            bool found = false;
		//            for (int i = 0; i < hPos.Count; i++)
		//            {
		//                // A special treatment is needed in this case
		//                // because =====xxx===== matches also 2 H1 headers (=='='==)
		//                if (match.Index >= hPos[i].Index && match.Index <= hPos[i].Index + hPos[i].Text.Length + 5)
		//                {
		//                    found = true;
		//                    break;
		//                }
		//            }
		//            if (!found)
		//            {
		//                hPos.Add(new HPosition(match.Index, h, 1, count));
		//                count++;
		//            }
		//            end = match.Index + match.Length;
		//        }
		//        ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
		//        match = _h1.Match(sb.ToString(), end);
		//    }

		//    return hPos;
		//}

		//private static string BuildEditSectionLink(int id, string page)
		//{
		//    StringBuilder sb = new StringBuilder();
		//    sb.Append(@"<a href=""Edit.aspx?Page=");
		//    sb.Append(HttpUtility.UrlEncode(page));
		//    sb.Append("&amp;Section=");
		//    sb.Append(id.ToString());
		//    sb.Append(@""" class=""editsectionlink"">");
		//    sb.Append(editSectionPlaceHolder);
		//    sb.Append("</a>");
		//    return sb.ToString();
		//}

		private static string GenerateList(string[] lines, int line, int level, ref int currLine)
		{
			var sb = new StringBuilder();
			if (lines[currLine][level] == '*') sb.Append("<ul>");
			else if (lines[currLine][level] == '#') sb.Append("<ol>");
			while (currLine <= lines.Length - 1 && CountBullets(lines[currLine]) >= level + 1)
			{
				if (CountBullets(lines[currLine]) == level + 1)
				{
					sb.Append("<li>");
					sb.Append(lines[currLine].Substring(CountBullets(lines[currLine])).Trim());
					sb.Append("</li>");
					currLine++;
				}
				else
				{
					sb.Remove(sb.Length - 5, 5);
					sb.Append(GenerateList(lines, currLine, level + 1, ref currLine));
					sb.Append("</li>");
				}
			}
			if (lines[line][level] == '*') sb.Append("</ul>");
			else if (lines[line][level] == '#') sb.Append("</ol>");
			return sb.ToString();
		}

		private static int CountBullets(string line)
		{
			int res = 0, count = 0;
			while (line[count] == '*' || line[count] == '#')
			{
				res++;
				count++;
			}
			return res;
		}

		//private static string ExtractBullets(string value)
		//{
		//    string res = "";
		//    for (int i = 0; i < value.Length; i++)
		//    {
		//        if (value[i] == '*' || value[i] == '#') res += value[i];
		//        else break;
		//    }
		//    return res;
		//}

		//private static string BuildToc(List<HPosition> hPos)
		//{
		//    StringBuilder sb = new StringBuilder();

		//    hPos.Sort(new HPositionComparer());

		//    // Table only used to workaround IE idiosyncrasies - use TocCointainer for styling
		//    sb.Append(@"<table id=""TocContainerTable""><tr><td>");
		//    sb.Append(@"<div id=""TocContainer"">");
		//    sb.Append(@"<p class=""small"">");
		//    sb.Append(tocTitlePlaceHolder);
		//    sb.Append("</p>");

		//    sb.Append(@"<div id=""Toc"">");
		//    sb.Append("<p><br />");
		//    for (int i = 0; i < hPos.Count; i++)
		//    {
		//        //Debug.WriteLine(i.ToString() + " " + hPos[i].Index.ToString() + ": " + hPos[i].Level);
		//        switch (hPos[i].Level)
		//        {
		//            case 1:
		//                break;
		//            case 2:
		//                sb.Append("&nbsp;&nbsp;&nbsp;");
		//                break;
		//            case 3:
		//                sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
		//                break;
		//            case 4:
		//                sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
		//                break;
		//        }
		//        if (hPos[i].Level == 1) sb.Append("<b>");
		//        if (hPos[i].Level == 4) sb.Append("<small>");
		//        sb.Append(@"<a href=""#");
		//        sb.Append(BuildHAnchor(hPos[i].Text, hPos[i].ID.ToString()));
		//        sb.Append(@""">");
		//        sb.Append(StripWikiMarkup(StripHtml(hPos[i].Text)));
		//        sb.Append("</a>");
		//        if (hPos[i].Level == 4) sb.Append("</small>");
		//        if (hPos[i].Level == 1) sb.Append("</b>");
		//        sb.Append("<br />");
		//    }
		//    sb.Append("</p>");
		//    sb.Append("</div>");

		//    sb.Append("</div>");
		//    sb.Append("</td></tr></table>");

		//    return sb.ToString();
		//}

		/// <summary>
		/// Builds a valid anchor name from a string.
		/// </summary>
		/// <param name="h">The string, usually a header (Hx).</param>
		/// <returns>The anchor ID.</returns>
		public static string BuildHAnchor(string h)
		{
			var sb = new StringBuilder(StripWikiMarkup(h));
			sb.Replace(" ", "_");
			sb.Replace(".", "");
			sb.Replace(",", "");
			sb.Replace(";", "");
			sb.Replace("\"", "");
			sb.Replace("/", "");
			sb.Replace("\\", "");
			sb.Replace("'", "");
			sb.Replace("(", "");
			sb.Replace(")", "");
			sb.Replace("[", "");
			sb.Replace("]", "");
			sb.Replace("{", "");
			sb.Replace("}", "");
			sb.Replace("<", "");
			sb.Replace(">", "");
			sb.Replace("#", "");
			sb.Replace("\n", "");
			sb.Replace("?", "");
			sb.Replace("&", "");
			sb.Replace("0", "A");
			sb.Replace("1", "B");
			sb.Replace("2", "C");
			sb.Replace("3", "D");
			sb.Replace("4", "E");
			sb.Replace("5", "F");
			sb.Replace("6", "G");
			sb.Replace("7", "H");
			sb.Replace("8", "I");
			sb.Replace("9", "J");
			return sb.ToString();
		}

		/// <summary>
		/// Builds a valid and unique anchor name from a string.
		/// </summary>
		/// <param name="h">The string, usually a header (Hx).</param>
		/// <param name="uid">The unique ID.</param>
		/// <returns>The anchor ID.</returns>
		public static string BuildHAnchor(string h, string uid)
		{
			return BuildHAnchor(h) + "_" + uid;
		}

		/// <summary>
		/// Escapes ampersands in a URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The escaped URL.</returns>
		private static string EscapeUrl(string url)
		{
			return url.Replace("&", "&amp;");
		}

		/// <summary>
		/// Builds a HTML table from WikiMarkup.
		/// </summary>
		/// <param name="table">The WikiMarkup.</param>
		/// <returns>The HTML.</returns>
		private static string BuildTable(string table)
		{
			// Proceed line-by-line, ignoring the first and last one
			string[] lines = table.Split(new [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length < 3)
			{
				return "<b>FORMATTER ERROR (Malformed Table)</b>";
			}
			var sb = new StringBuilder();
			sb.Append("<table");
			if (lines[0].Length > 2)
			{
				sb.Append(" ");
				sb.Append(lines[0].Substring(3));
			}
			sb.Append(">");
			int count = 1;
			if (lines[1].Length >= 3 && lines[1].Trim().StartsWith("|+"))
			{
				// Table caption
				sb.Append("<caption>");
				sb.Append(lines[1].Substring(3));
				sb.Append("</caption>");
				count++;
			}
			if (!lines[count].StartsWith("|-")) sb.Append("<tr>");
			string item;
			for (int i = count; i < lines.Length - 1; i++)
			{
				if (lines[i].Trim().StartsWith("|-"))
				{
					// New line
					if (i != count) sb.Append("</tr>");
					sb.Append("<tr");
					if (lines[i].Length > 2)
					{
						sb.Append(" ");
						sb.Append(lines[i].Substring(3));
					}
					sb.Append(">");
				}
				else if (lines[i].Trim().StartsWith("|"))
				{
					// Cell
					if (lines[i].Length < 3) continue;
					item = lines[i].Substring(2);
					if (item.IndexOf(" || ") != -1)
					{
						sb.Append("<td>");
						sb.Append(item.Replace(" || ", "</td><td>"));
						sb.Append("</td>");
					}
					else if (item.IndexOf(" | ") != -1)
					{
						sb.Append("<td ");
						sb.Append(item.Substring(0, item.IndexOf(" | ")));
						sb.Append(">");
						sb.Append(item.Substring(item.IndexOf(" | ") + 3));
						sb.Append("</td>");
					}
					else
					{
						sb.Append("<td>");
						sb.Append(item);
						sb.Append("</td>");
					}
				}
				else if (lines[i].Trim().StartsWith("!"))
				{
					// Header
					if (lines[i].Length < 3) continue;
					item = lines[i].Substring(2);
					if (item.IndexOf(" !! ") != -1)
					{
						sb.Append("<th>");
						sb.Append(item.Replace(" !! ", "</th><th>"));
						sb.Append("</th>");
					}
					else if (item.IndexOf(" ! ") != -1)
					{
						sb.Append("<th ");
						sb.Append(item.Substring(0, item.IndexOf(" ! ")));
						sb.Append(">");
						sb.Append(item.Substring(item.IndexOf(" ! ") + 3));
						sb.Append("</th>");
					}
					else
					{
						sb.Append("<th>");
						sb.Append(item);
						sb.Append("</th>");
					}
				}
			}
			if (sb.ToString().EndsWith("<tr>"))
			{
				sb.Remove(sb.Length - 4 - 1, 4);
				sb.Append("</table>");
			}
			else
			{
				sb.Append("</tr></table>");
			}
			sb.Replace("<tr></tr>", "");

			return sb.ToString();
		}

		private static string BuildIndent(string indent)
		{
			int colons = 0;
			indent = indent.Trim();
			while (colons < indent.Length && indent[colons] == ':') colons++;
			indent = indent.Substring(colons).Trim();
			return @"<div style=""margin: 0px; padding: 0px; padding-left: " + (colons * 15) + @"px"">" + indent + "</div>";
		}

		//private static string BuildCloud()
		//{
		//    StringBuilder sb = new StringBuilder(500);
		//    // Total categorized Pages (uncategorized Pages don't count)
		//    int tot = Pages.Instance.AllPages.Count - Pages.Instance.GetUncategorizedPages().Length;
		//    for (int i = 0; i < Pages.Instance.AllCategories.Count; i++)
		//    {
		//        if (Pages.Instance.AllCategories[i].Pages.Length > 0)
		//        {
		//            sb.Append(@"<a href=""AllPages.aspx?Cat=");
		//            sb.Append(Tools.UrlEncode(Pages.Instance.AllCategories[i].Name));
		//            sb.Append(@""" style=""font-size: ");
		//            sb.Append(ComputeSize((float)Pages.Instance.AllCategories[i].Pages.Length / (float)tot * 100F).ToString());
		//            sb.Append(@"px;"">");
		//            sb.Append(Pages.Instance.AllCategories[i].Name);
		//            sb.Append("</a>");
		//        }
		//        if (i != Pages.Instance.AllCategories.Count - 1) sb.Append(" ");
		//    }
		//    return sb.ToString();
		//}

		//private static int ComputeSize(float percentage)
		//{
		//    // Interpolates min and max size on a line, so that if:
		//    // - percentage = 0   -> size = minSize
		//    // - percentage = 100 -> size = maxSize
		//    // - intermediate values are calculated
		//    const float minSize = 8;
		//    const float maxSize = 26;
		//    //return (int)((maxSize - minSize) / 100F * (float)percentage + minSize); // Linear interpolation
		//    return (int)(maxSize - (maxSize - minSize) * Math.Exp(-percentage / 25)); // Exponential interpolation
		//}

		private static void ComputeNoWiki(string text, ICollection<int> noWikiBegin, List<int> noWikiEnd)
		{
			noWikiBegin.Clear();
			noWikiEnd.Clear();

			var match = _noWiki.Match(text);
			while (match.Success)
			{
				noWikiBegin.Add(match.Index);
				noWikiEnd.Add(match.Index + match.Length);
				match = _noWiki.Match(text, match.Index + match.Length);
			}
		}

		private static bool IsNoWikied(int index, IList<int> noWikiBegin, IList<int> noWikiEnd, out int end)
		{
			for (int i = 0; i < noWikiBegin.Count; i++)
			{
				if (index > noWikiBegin[i] && index < noWikiEnd[i])
				{
					end = noWikiEnd[i];
					return true;
				}
			}
			end = 0;
			return false;
		}

		///// <summary>
		///// Performs the internal Phase 3 of the Formatting pipeline.
		///// </summary>
		///// <param name="raw">The raw data.</param>
		///// <param name="current">The current PageInfo, if any.</param>
		///// <returns>The formatted content.</returns>
		//public static string FormatPhase3(string raw, PageInfo current)
		//{
		//    StringBuilder sb = new StringBuilder();
		//    StringBuilder dummy;
		//    sb.Append(raw);

		//    Match match;

		//    // Format {CLOUD} tag (it has to be in Phase3 because on page rebind the cache is not cleared)
		//    match = cloud.Match(sb.ToString());
		//    string cloudMarkup = BuildCloud();
		//    while (match.Success)
		//    {
		//        sb.Remove(match.Index, match.Length);
		//        sb.Insert(match.Index, cloudMarkup);
		//        match = cloud.Match(sb.ToString(), match.Index + cloudMarkup.Length);
		//    }

		//    match = null;

		//    dummy = new StringBuilder("<b>");
		//    dummy.Append(Exchanger.ResourceExchanger.GetResource("TableOfContents"));
		//    dummy.Append(@"</b><span id=""ExpandTocSpan""> [<a href=""#"" onclick=""javascript:if(document.getElementById('Toc').style['display']=='none') document.getElementById('Toc').style['display']=''; else document.getElementById('Toc').style['display']='none'; return false;"">");
		//    dummy.Append(Exchanger.ResourceExchanger.GetResource("HideShow"));
		//    dummy.Append("</a>]</span>");
		//    sb.Replace(tocTitlePlaceHolder, dummy.ToString());

		//    sb.Replace(editSectionPlaceHolder, Exchanger.ResourceExchanger.GetResource("Edit"));

		//    string shift = "";
		//    if (HttpContext.Current.Request.Cookies[Settings.CultureCookieName] != null) shift = HttpContext.Current.Request.Cookies[Settings.CultureCookieName]["T"];

		//    match = sign.Match(sb.ToString());
		//    while (match.Success)
		//    {
		//        sb.Remove(match.Index, match.Length);
		//        string txt = match.Value.Substring(3, match.Length - 6);
		//        int idx = txt.LastIndexOf(",");
		//        string[] fields = new string[] { txt.Substring(0, idx), txt.Substring(idx + 1) };
		//        dummy = new StringBuilder();
		//        dummy.Append(@"<span class=""signature""><a href=""Message.aspx?Username=");
		//        dummy.Append(Tools.UrlEncode(fields[0]));
		//        dummy.Append(@""">");
		//        dummy.Append(fields[0]);
		//        dummy.Append("</a>, ");
		//        dummy.Append(Tools.AlignWithPreferences(DateTime.Parse(fields[1]), shift).ToString(Settings.DateTimeFormat));
		//        dummy.Append("</span>");
		//        sb.Insert(match.Index, dummy.ToString());
		//        match = sign.Match(sb.ToString());
		//    }

		//    match = username.Match(sb.ToString());
		//    while (match.Success)
		//    {
		//        sb.Remove(match.Index, match.Length);
		//        if (SessionFacade.LoginKey != null) sb.Insert(match.Index, SessionFacade.Username);
		//        match = username.Match(sb.ToString());
		//    }

		//    return sb.ToString();
		//}

	}

	///// <summary>
	///// Represents a Header.
	///// </summary>
	//public class HPosition
	//{

	//    private int index;
	//    private string text;
	//    private int level;
	//    private int id;

	//    /// <summary>
	//    /// Initializes a new instance of the <b>HPosition</b> class.
	//    /// </summary>
	//    /// <param name="index">The Index.</param>
	//    /// <param name="text">The Text.</param>
	//    /// <param name="level">The Header level.</param>
	//    /// <param name="id">The Unique ID of the Header (0-based counter).</param>
	//    public HPosition(int index, string text, int level, int id)
	//    {
	//        this.index = index;
	//        this.text = text;
	//        this.level = level;
	//        this.id = id;
	//    }

	//    /// <summary>
	//    /// Gets or sets the Index.
	//    /// </summary>
	//    public int Index
	//    {
	//        get { return index; }
	//        set { index = value; }
	//    }

	//    /// <summary>
	//    /// Gets or sets the Text.
	//    /// </summary>
	//    public string Text
	//    {
	//        get { return text; }
	//        set { text = value; }
	//    }

	//    /// <summary>
	//    /// Gets or sets the Level.
	//    /// </summary>
	//    public int Level
	//    {
	//        get { return level; }
	//        set { level = value; }
	//    }

	//    /// <summary>
	//    /// Gets or sets the ID (0-based counter).
	//    /// </summary>
	//    public int ID
	//    {
	//        get { return id; }
	//        set { id = value; }
	//    }

	//}

	///// <summary>
	///// Compares HPosition objects.
	///// </summary>
	//public class HPositionComparer : IComparer<HPosition>
	//{
	//    /// <summary>
	//    /// Performs the comparison.
	//    /// </summary>
	//    /// <param name="x">The first object.</param>
	//    /// <param name="y">The second object.</param>
	//    /// <returns>The comparison result.</returns>
	//    public int Compare(HPosition x, HPosition y)
	//    {
	//        return x.Index.CompareTo(y.Index);
	//    }
	//}
}
