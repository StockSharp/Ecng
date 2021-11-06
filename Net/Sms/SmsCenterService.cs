namespace Ecng.Net.Sms
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Net.Http;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Net;

	public enum SmsFormats
	{
		[Display(Name = "")]
		Simple,

		[Display(Name = "flash=1")]
		Flash,

		[Display(Name = "push=1")]
		WapPush,

		[Display(Name = "hlr=1")]
		Hlr,

		[Display(Name = "bin=1")]
		Bin,

		[Display(Name = "bin=2")]
		BinHex,

		[Display(Name = "ping=1")]
		Ping,
	}

	public class SmsCenterService : ISmsService
	{
		private readonly string _login;
		private readonly string _password;

		// Константы с параметрами отправки 
		//const string SMSC_LOGIN = "";        // логин клиента 
		//const string SMSC_PASSWORD = "";    // пароль или MD5-хеш пароля в нижнем регистре 
		//const bool SMSC_POST = false;                // использовать метод POST 
		//const bool SMSC_HTTPS = false;                // использовать HTTPS протокол 
		//const string SMSC_CHARSET = "utf-8";        // кодировка сообщения (windows-1251 или koi8-r), по умолчанию используется utf-8 
		//const bool SMSC_DEBUG = false;                // флаг отладки 

		// Константы для отправки SMS по SMTP 
		//const string SMTP_FROM = "api@smsc.ru";        // e-mail адрес отправителя 
		//const string SMTP_SERVER = "send.smsc.ru";    // адрес smtp сервера 
		//const string SMTP_LOGIN = "";                // логин для smtp сервера 
		//const string SMTP_PASSWORD = "";            // пароль для smtp сервера 

		public SmsCenterService(string login, string password)
		{
			_login = login;
			_password = password;
		}

		public string Sender { get; set; }
		public bool UseTranslit { get; set; }
		public bool UseHttps { get; set; }
		public bool UsePost { get; set; }

		private Encoding _encoding = Encoding.UTF8;

		public Encoding Encoding
		{
			get => _encoding;
			set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
		}

		// Метод отправки SMS 
		// 
		// обязательные параметры: 
		// 
		// phones - список телефонов через запятую или точку с запятой 
		// message - отправляемое сообщение 
		// 
		// необязательные параметры: 
		// 
		// translit - переводить или нет в транслит 
		// time - необходимое время доставки в виде строки (DDMMYYhhmm, h1-h2, 0ts, +m) 
		// id - идентификатор сообщения. Представляет собой 32-битное число в диапазоне от 1 до 2147483647. 
		// format - формат сообщения (0 - обычное sms, 1 - flash-sms, 2 - wap-push, 3 - hlr, 4 - bin, 5 - bin-hex, 6 - ping-sms) 
		// sender - имя отправителя (Sender ID). Для отключения Sender ID по умолчанию необходимо в качестве имени 
		// передать пустую строку или точку. 
		// query - строка дополнительных параметров, добавляемая в URL-запрос ("valid=01:00&maxsms=3") 
		// 
		// возвращает массив строк (<id>, <количество sms>, <стоимость>, <баланс>) в случае успешной отправки 
		// либо массив строк (<id>, -<код ошибки>) в случае ошибки 

		public string[] SendSms(IEnumerable<string> phones, string message, DateTime? time = null, int id = 0, SmsFormats format = SmsFormats.Simple, string query = "")
		{
			var m = InternalSendSmsAsync("send", "cost=3&charset=" + Encoding.WebName + "&phones=" + phones.JoinDotComma().EncodeUrl()
						+ "&mes=" + message.EncodeUrl() + "&id=" + id.To<string>() + "&translit=" + (UseTranslit ? "1" : "0")
							+ (format != SmsFormats.Simple ? "&" + format.GetDisplayName() : string.Empty) + (!Sender.IsEmpty() ? "&sender=" + Sender.EncodeUrl() : string.Empty)
							+ (time != null ? "&time=" + time.Value.ToString("DDMMYYhhmm").EncodeUrl() : string.Empty) + (!query.IsEmpty() ? "&" + query : string.Empty)).Result;

			// (id, cnt, cost, balance) или (id, -error) 

			return m;
		}

		// SMTP версия метода отправки SMS 

		//public void send_sms_mail(string phones, string message, int translit = 0, string time = "", int id = 0, int format = 0, string sender = "")
		//{
		//	MailMessage mail = new MailMessage();

		//	mail.To.Add("send@send.smsc.ru");
		//	mail.From = new MailAddress(SMTP_FROM, "");

		//	mail.Body = SMSC_LOGIN + ":" + SMSC_PASSWORD + ":" + id.ToString() + ":" + time + ":"
		//				+ translit.ToString() + "," + format.ToString() + "," + sender
		//				+ ":" + phones + ":" + message;

		//	mail.BodyEncoding = Encoding.GetEncoding(SMSC_CHARSET);
		//	mail.IsBodyHtml = false;

		//	SmtpClient client = new SmtpClient(SMTP_SERVER, 25);
		//	client.DeliveryMethod = SmtpDeliveryMethod.Network;
		//	client.EnableSsl = false;
		//	client.UseDefaultCredentials = false;

		//	if (SMTP_LOGIN != "")
		//		client.Credentials = new NetworkCredential(SMTP_LOGIN, SMTP_PASSWORD);

		//	client.Send(mail);
		//}

		// Метод получения стоимости SMS 
		// 
		// обязательные параметры: 
		// 
		// phones - список телефонов через запятую или точку с запятой 
		// message - отправляемое сообщение 
		// 
		// необязательные параметры: 
		// 
		// translit - переводить или нет в транслит 
		// format - формат сообщения (0 - обычное sms, 1 - flash-sms, 2 - wap-push, 3 - hlr, 4 - bin, 5 - bin-hex, 6 - ping-sms) 
		// sender - имя отправителя (Sender ID) 
		// query - строка дополнительных параметров, добавляемая в URL-запрос ("list=79999999999:Ваш пароль: 123\n78888888888:Ваш пароль: 456") 
		// 
		// возвращает массив (<стоимость>, <количество sms>) либо массив (0, -<код ошибки>) в случае ошибки 

		public string[] GetSmsCost(string phones, string message, SmsFormats format = SmsFormats.Simple, string query = "")
		{
			var m = InternalSendSmsAsync("send", "cost=1&charset=" + Encoding.WebName + "&phones=" + phones.EncodeUrl()
							+ "&mes=" + message.EncodeUrl() + (UseTranslit ? "1" : "0") + (format != SmsFormats.Simple ? "&" + format.GetDisplayName() : string.Empty)
							+ (!Sender.IsEmpty() ? "&sender=" + Sender.EncodeUrl() : string.Empty) + (query != string.Empty ? "&query" : string.Empty)).Result;

			// (cost, cnt) или (0, -error) 

			return m;
		}

		// Метод проверки статуса отправленного SMS или HLR-запроса 
		// 
		// id - ID cообщения 
		// phone - номер телефона 
		// 
		// возвращает массив: 
		// для отправленного SMS (<статус>, <время изменения>, <код ошибки sms>) 
		// для HLR-запроса (<статус>, <время изменения>, <код ошибки sms>, <код IMSI SIM-карты>, <номер сервис-центра>, <код страны регистрации>, 
		// <код оператора абонента>, <название страны регистрации>, <название оператора абонента>, <название роуминговой страны>, 
		// <название роумингового оператора>) 
		// 
		// При all = 1 дополнительно возвращаются элементы в конце массива: 
		// (<время отправки>, <номер телефона>, <стоимость>, <sender id>, <название статуса>, <текст сообщения>) 
		// 
		// либо массив (0, -<код ошибки>) в случае ошибки 

		public string[] GetStatus(int id, string phone, bool all = false)
		{
			var m = InternalSendSmsAsync("status", "phone=" + phone.EncodeUrl() + "&id=" + id.To<string>() + "&all=" + (all ? "1" : "0")).Result;

			// (status, time, err, ...) или (0, -error) 

			if (all && m.Length > 9 && (m.Length < 14 || m[14] != "HLR"))
				m = m.JoinComma().Split(",".ToCharArray(), 9);

			return m;
		}

		// Метод получения баланса 
		// 
		// без параметров 
		// 
		// возвращает баланс в виде строки или пустую строку в случае ошибки 

		public string GetBalance()
		{
			var m = InternalSendSmsAsync("balance", string.Empty).Result;

			// (balance) или (0, -error) 

			return m.Length == 1 ? m[0] : string.Empty;
		}

		// ПРИВАТНЫЕ МЕТОДЫ 

		// Метод вызова запроса. Формирует URL и делает 3 попытки чтения 

		private async Task<string[]> InternalSendSmsAsync(string cmd, string arg, CancellationToken cancellationToken = default)
		{
			arg = "login=" + _login.EncodeUrl() + "&psw=" + _password.EncodeUrl() + "&fmt=1&" + arg;

			var url = (UseHttps ? "https" : "http") + "://smsc.ru/sys/" + cmd + ".php" + (UsePost ? string.Empty : "?" + arg);

			var request = new HttpClient();

			HttpResponseMessage response;

			if (UsePost)
				response = await request.PostAsync(url, new StringContent(arg), cancellationToken);
			else
				response = await request.GetAsync(url, cancellationToken);

			try
			{
				response.EnsureSuccessStatusCode();

				var ret = await response.Content.ReadAsAsync<string>(cancellationToken);

				if (ret == string.Empty)
					ret = ","; // фиктивный ответ 

				var retVal = ret.Split(',');

				if (int.TryParse(retVal[1], out var errCode) && errCode < 0)
					throw new InvalidOperationException($"СМС шлюз вернул ошибку {ret}.");

				return retVal;
			}
			finally
			{
				response.Dispose();
			}
		}

		public SmsFormats Format { get; set; }

		async Task<string> ISmsService.SendAsync(string phone, string message, CancellationToken cancellationToken)
		{
			var id = 1;
			DateTime? time = null;
			string query = null;

			var m = await InternalSendSmsAsync("send", "cost=3&charset=" + Encoding.WebName + "&phones=" + phone.EncodeUrl()
						+ "&mes=" + message.EncodeUrl() + "&id=" + id.To<string>() + "&translit=" + (UseTranslit ? "1" : "0")
							+ (Format != SmsFormats.Simple ? "&" + Format.GetDisplayName() : string.Empty) + (!Sender.IsEmpty() ? "&sender=" + Sender.EncodeUrl() : string.Empty)
							+ (time != null ? "&time=" + time.Value.ToString("DDMMYYhhmm").EncodeUrl() : string.Empty) + (!query.IsEmpty() ? "&" + query : string.Empty), cancellationToken);

			// (id, cnt, cost, balance) или (id, -error) 

			return m[0];
		}
	} 
}