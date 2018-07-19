using Ecng.Net.SocketIO.Engine.Modules;
using Ecng.Net.SocketIO.Engine.Parser;
using System;
using System.Net;
using System.Collections.Generic;
using System.Security.Authentication;
//using WebSocket4Net;
//using SuperSocket.ClientEngine.Proxy;

namespace Ecng.Net.SocketIO.Engine.Client.Transports
{
	using System.Globalization;
	using System.Linq;
	using System.Net.WebSockets;
	using System.Text;
	using System.Threading;

	using Ecng.Common;

	public class WebSocket : Transport
    {
        public static readonly string NAME = "websocket";

	    private const bool _isNative = true;

	    private ClientWebSocket _ws;
	    private CancellationTokenSource _source = new CancellationTokenSource();
	    private bool _connected;

        //private WebSocket4Net.WebSocket ws;
        private List<KeyValuePair<string, string>> Cookies;
        private List<KeyValuePair<string, string>> MyExtraHeaders;

        public WebSocket(Options opts)
            : base(opts)
        {
            Name = NAME;
            Cookies = new List<KeyValuePair<string, string>>();
            foreach (var cookie in opts.Cookies)
            {
                Cookies.Add(new KeyValuePair<string, string>(cookie.Key, cookie.Value));
            }
            MyExtraHeaders = new List<KeyValuePair<string, string>>();
            foreach (var header in opts.ExtraHeaders)
            {
                MyExtraHeaders.Add(new KeyValuePair<string, string>(header.Key, header.Value));
            }
        }

        protected override void DoOpen()
        {
            var log = LogManager.GetLogger(Global.CallerName());
            log.Info("DoOpen uri =" + this.Uri());

	        if (_isNative)
	        {
		        _ws = new ClientWebSocket();
		        
		        var destUrl = new UriBuilder(this.Uri());
		        if (this.Secure)
			        destUrl.Scheme = "wss";
		        else
			        destUrl.Scheme = "ws";
		        var useProxy = !WebRequest.DefaultWebProxy.IsBypassed(destUrl.Uri);
		        if (useProxy)
		        {
			        var proxyUrl = WebRequest.DefaultWebProxy.GetProxy(destUrl.Uri);
			        var proxy = new WebProxy(proxyUrl.Host, proxyUrl.Port);
			        _ws.Options.Proxy = proxy;
		        }
		        _ws.ConnectAsync(destUrl.Uri, _source.Token).Wait();
		        OnOpen();

		        _connected = true;

		        ThreadingHelper.Thread(() => CultureInfo.InvariantCulture.DoInCulture(OnReceive)).Launch();
	        }
	        else
	        {
		        //ws = new WebSocket4Net.WebSocket(this.Uri(), String.Empty, Cookies, MyExtraHeaders, sslProtocols: SslProtocols)
		        //{
			       // EnableAutoSendPing = false
		        //};
		        //if (ServerCertificate.Ignore)
		        //{
			       // var security = ws.Security;

			       // if (security != null)
			       // {
				      //  security.AllowUnstrustedCertificate = true;
				      //  security.AllowNameMismatchCertificate = true;
			       // }
		        //}
		        //ws.Opened += ws_Opened;
		        //ws.Closed += ws_Closed;
		        //ws.MessageReceived += ws_MessageReceived;
		        //ws.DataReceived += ws_DataReceived;
		        //ws.Error += ws_Error;

		        //var destUrl = new UriBuilder(this.Uri());
		        //if (this.Secure)
			       // destUrl.Scheme = "https";
		        //else
			       // destUrl.Scheme = "http";
		        //var useProxy = !WebRequest.DefaultWebProxy.IsBypassed(destUrl.Uri);
		        //if (useProxy)
		        //{
			       // var proxyUrl = WebRequest.DefaultWebProxy.GetProxy(destUrl.Uri);
			       // var proxy = new HttpConnectProxy(new DnsEndPoint(proxyUrl.Host, proxyUrl.Port), destUrl.Host);
			       // ws.Proxy = proxy;
		        //}
		        //ws.Open();
	        }
        }

	    private void OnReceive()
	    {
		    try
		    {
			    var buf = new byte[1024 * 1024];
			    var pos = 0;

			    var errorCount = 0;
			    const int maxErrorCount = 10;

			    while (_connected)
			    {
				    string recv = null;

				    try
				    {
					    var task = _ws.ReceiveAsync(new ArraySegment<byte>(buf, pos, buf.Length - pos), _source.Token);
					    task.Wait();

					    var result = task.Result;

					    if (result.CloseStatus != null)
					    {
						    if (task.Exception != null && _connected)
							    this.OnError("websocket error", task.Exception);

						    break;
					    }

					    pos += result.Count;

					    if (!result.EndOfMessage)
						    continue;

					    recv = Encoding.UTF8.GetString(buf, 0, pos);

					    pos = 0;

					    OnData(recv);

					    errorCount = 0;
				    }
				    catch (AggregateException ex)
				    {
					    if (_connected)
						    this.OnError("websocket error", ex);

					    if (ex.InnerExceptions.FirstOrDefault() is WebSocketException)
						    break;

					    if (++errorCount >= maxErrorCount)
					    {
						    //this.AddErrorLog("Max error {0} limit reached.", maxErrorCount);
						    break;
					    }
				    }
				    catch (Exception ex)
				    {
					    this.OnError("websocket error", new InvalidOperationException(recv, ex));
				    }
			    }

			    try
			    {
				    _ws.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, _source.Token).Wait();
			    }
			    catch (Exception ex)
			    {
				    if (_connected)
					    this.OnError("websocket error", ex);
			    }

			    _ws.Dispose();

			    OnClose();
		    }
		    catch (Exception ex)
		    {
			    this.OnError("websocket error", ex);
		    }
	    }

        //void ws_DataReceived(object sender, DataReceivedEventArgs e)
        //{
        //    var log = LogManager.GetLogger(Global.CallerName());
        //    log.Info("ws_DataReceived " + e.Data);
        //    this.OnData(e.Data);
        //}

        //private void ws_Opened(object sender, EventArgs e)
        //{
        //    var log = LogManager.GetLogger(Global.CallerName());
        //    log.Info("ws_Opened " + ws.SupportBinary);
        //    this.OnOpen();
        //}

        //void ws_Closed(object sender, EventArgs e)
        //{
        //    var log = LogManager.GetLogger(Global.CallerName());
        //    log.Info("ws_Closed");
        //    ws.Opened -= ws_Opened;
        //    ws.Closed -= ws_Closed;
        //    ws.MessageReceived -= ws_MessageReceived;
        //    ws.DataReceived -= ws_DataReceived;
        //    ws.Error -= ws_Error;
        //    this.OnClose();
        //}

        //void ws_MessageReceived(object sender, MessageReceivedEventArgs e)
        //{
        //    var log = LogManager.GetLogger(Global.CallerName());
        //    log.Info("ws_MessageReceived e.Message= " + e.Message);
        //    this.OnData(e.Message);
        //}

        //void ws_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        //{
        //    this.OnError("websocket error", e.Exception);
        //}

        protected override void Write(System.Collections.Immutable.ImmutableList<Parser.Packet> packets)
        {
            Writable = false;
            foreach (var packet in packets)
            {
                Parser.Parser.EncodePacket(packet, new WriteEncodeCallback(this));
            }

            // fake drain
            // defer to next tick to allow Socket to clear writeBuffer
            //EasyTimer.SetTimeout(() =>
            //{
            Writable = true;
            Emit(EVENT_DRAIN);
            //}, 1);
        }

        public class WriteEncodeCallback : IEncodeCallback
        {
            private WebSocket webSocket;

            public WriteEncodeCallback(WebSocket webSocket)
            {
                this.webSocket = webSocket;
            }

            public void Call(object data)
            {
                //var log = LogManager.GetLogger(Global.CallerName());

	            //if (webSocket.ws != null)
	            //{
		           // if (data is string)
		           // {                    
			          //  webSocket.ws.Send((string)data);
		           // }
		           // else if (data is byte[])
		           // {
			          //  var d = (byte[])data;

			          //  //try
			          //  //{
			          //  //    var dataString = BitConverter.ToString(d);
			          //  //    //log.Info(string.Format("WriteEncodeCallback byte[] data {0}", dataString));
			          //  //}
			          //  //catch (Exception e)
			          //  //{
			          //  //    log.Error(e);
			          //  //}

			          //  webSocket.ws.Send(d, 0, d.Length);
		           // }
	            //}
	            //else
	            {
		            if (data is string)
		            {      
			            var sendBuf = Encoding.UTF8.GetBytes((string)data);
			            webSocket._ws.SendAsync(new ArraySegment<byte>(sendBuf), WebSocketMessageType.Text, true, webSocket._source.Token).Wait();
		            }
		            else if (data is byte[])
		            {
			            var d = (byte[])data;

			            //try
			            //{
			            //    var dataString = BitConverter.ToString(d);
			            //    //log.Info(string.Format("WriteEncodeCallback byte[] data {0}", dataString));
			            //}
			            //catch (Exception e)
			            //{
			            //    log.Error(e);
			            //}

			            webSocket._ws.SendAsync(new ArraySegment<byte>(d), WebSocketMessageType.Text, true, webSocket._source.Token).Wait();
		            }
	            }
            }
        }



        protected override void DoClose()
        {
			//if (ws != null)
   //         {
          
   //             try
   //             {
   //                 ws.Close();
   //             }
   //             catch (Exception e)
   //             {
   //                 var log = LogManager.GetLogger(Global.CallerName());
   //                 log.Info("DoClose ws.Close() Exception= " + e.Message);
   //             }
   //         }

	        if (_ws != null)
	        {
          
		        try
		        {
			        //ws.Close();
			        _connected = false;
			        _source.Cancel();
			        _source = new CancellationTokenSource();
		        }
		        catch (Exception e)
		        {
			        var log = LogManager.GetLogger(Global.CallerName());
			        log.Info("DoClose ws.Close() Exception= " + e.Message);
		        }
	        }
        }



        public string Uri()
        {
            Dictionary<string, string> query = null;
            query = this.Query == null ? new Dictionary<string, string>() : new Dictionary<string, string>(this.Query);
            var schema = this.Secure ? "wss" : "ws";
            var portString = "";

            if (this.TimestampRequests)
            {
                query.Add(this.TimestampParam, DateTime.Now.Ticks.ToString() + "-" + Transport.Timestamps++);
            }

            var _query = ParseQS.Encode(query);

            if (this.Port > 0 && (("wss" == schema && this.Port != 443)
                    || ("ws" == schema && this.Port != 80)))
            {
                portString = ":" + this.Port;
            }

            if (_query.Length > 0)
            {
                _query = "?" + _query;
            }

            return schema + "://" + this.Hostname + portString + this.Path + _query;
        }
    }
}
