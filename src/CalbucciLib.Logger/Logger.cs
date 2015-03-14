﻿	using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
	using System.Net.Mime;
	using System.Reflection;
	using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CalbucciLib
{
	public class Logger
	{
		private static readonly string[] _SensitiveInfo = new[]
		{
			"pwd",
			"pass",
			"auth",
			"ccnum",
			"ccno",
			"credit",
			"token",
			"card",
			"ssn",
			"socialsec",
			"ssnum",
			"secnumber"
		};

		public static Logger Default { get; set; }

		/// <summary>
		/// Truncate threshold for FORMs values (default 8K)
		/// </summary>
		public int MaxHttpFormValueLength { get; set; }
		/// <summary>
		/// Truncate threshold for outputting the body content for text HTTP POST requests (default 32K)
		/// </summary>
		public int MaxHttpBodyLength { get; set; }
		/// <summary>
		/// Include File Names in the CallStack category (doesn't affect Exception logging) (defaults to false)
		/// </summary>
		public bool IncludeFileNamesInCallStack { get; set; }
		/// <summary>
		/// Include the Session Items objects (defaults to false)
		/// </summary>
		public bool IncludeSessionObjects { get; set; }
		/// <summary>
		/// Enable callback for the log events so they can sent to DB, APIs, file, etc
		/// </summary>
		public List<Action<LogEvent>> LogExtensions { get; set; }
		/// <summary>
		/// Optional function to test if we should log or not that content
		/// </summary>
		public Func<LogEvent, bool> ShouldLogCallback { get; set; }

		/// <summary>
		/// If the logging system itself throws an exception, it calls this function
		/// </summary>
		public Action<Exception> InternalCrashCallback { get; set; }


		private MailAddress _DefaultEmailAddress { get; set; }
		private MailAddress _FatalEmailAddress { get; set; }

		public MailAddress EmailFrom { get; set; }
		/// <summary>
		/// Set an email address to all log information to
		/// </summary>
		public string SendToEmailAddress
		{
			get { return _DefaultEmailAddress != null ? _DefaultEmailAddress.Address : null; }
			set
			{
				_DefaultEmailAddress = value == null ? null : new MailAddress(value);
				if (_DefaultEmailAddress != null && this.SmtpClient == null)
				{
					SmtpClient = new SmtpClient();
				}
			}
		}

		public string SendToEmailAddressFatal
		{
			get { return _FatalEmailAddress != null ? _FatalEmailAddress.Address : null; }
			set
			{
				_FatalEmailAddress = value == null ? null : new MailAddress(value);
				if (_FatalEmailAddress != null && this.SmtpClient == null)
				{
					SmtpClient = new SmtpClient();
				}
			}
		}

		/// <summary>
		/// Subject Line Prefix 
		/// </summary>
		public string SubjectLinePrefix { get; set; }

		public SmtpClient SmtpClient { get; set; }

		// ============================================================
		//
		// CONSTRUCTORS
		//
		// ============================================================

		static Logger()
		{
			Default = new Logger();
		}

		public Logger()
		{
			MaxHttpFormValueLength = 8192;
			MaxHttpBodyLength = 32678;
			IncludeFileNamesInCallStack = false;
			IncludeSessionObjects = false;
			SubjectLinePrefix = "[Log] ";
			EmailFrom = new MailAddress("nobody@test.com");
		}

		// ============================================================
		//
		// PUBLIC CONFIG
		//
		// ============================================================


		public void AddExtension(Action<LogEvent> extensionLog)
		{
			if (LogExtensions == null)
			{
				LogExtensions = new List<Action<LogEvent>>();
				LogExtensions.Add(extensionLog);
			}

		}



		// ============================================================
		//
		// PUBLIC LOG
		//
		// ============================================================

		public void Error(string format, params string[] args)
		{
			Error(null, format, args);
		}

		public void Error(Action<LogEvent> appendData, string format, params string[] args)
		{
			Log(appendData, "Error", null, format, args);
		}

		public void Info(string format, params string[] args)
		{
			Info(null, format, args);
		}

		public void Info(Action<LogEvent> appendData, string format, params string[] args)
		{
			Log(appendData, "Info", null, format, args);
		}

		public void Warning(string format, params string[] args)
		{
			Warning(null, format, args);
		}

		public void Warning(Action<LogEvent> appendData, string format, params string[] args)
		{
			Log(appendData, "Warning", null, format, args);
		}

		public void Fatal(string format, params string[] args)
		{
			Fatal(null, format, args);
		}

		public void Fatal(Action<LogEvent> appendData, string format, params string[] args)
		{
			Log(appendData, "Fatal", null, format, args);
		}

		public void Exception(Exception ex, string format, params string[] args)
		{
			Exception(null, ex, format, args);
		}

		public void Exception(Action<LogEvent> appendData, Exception ex, string format, params string[] args)
		{
			Log(appendData, "Exception", ex, format, args);
		}

		public void PerfIssue(string format, params string[] args)
		{
			PerfIssue(null, format, args);
		}

		public void PerfIssue(Action<LogEvent> appendData, string format, params string[] args)
		{
			Log(appendData, "PerfIssue", null, format, args);
		}

		public void InvalidCodePath(string format, params string[] args)
		{
			InvalidCodePath(null, format, args);	
		}

		public void InvalidCodePath(Action<LogEvent> appendData, string format, params string[] args)
		{
			Log(appendData, "InvalidCodePath", null, format, args);
		}




	// ============================================================
		//
		// PRIVATE
		//
		// ============================================================
		private void Log(Action<LogEvent> appendData, string type, Exception ex, string format, params string[] args)
	    {
			LogEvent logEvent = new LogEvent(type);
		    if (format == null)
			    format = ex.Message;

			if (format.IndexOf('{') >= 0)
			{
				logEvent.Message = string.Format(format, args);
			}
			else
			{
				logEvent.Message = format;
				if (args != null && args.Length > 0)
				{
					for (int i = 0; i < args.Length; i++)
					{
						logEvent.Add("Args", i.ToString(), args[i]);
					}
				}
			}

			var ctx = HttpContext.Current;
			if (ctx != null)
			{
				AppendHttpContextUserInfo(logEvent, ctx);
				AppendHttpRequestInfo(logEvent, ctx);
				AppendHttpResponseInfo(logEvent, ctx);
				AddHttpSessionInfo(logEvent, ctx);
			}

		    logEvent.StackSignature = AppendCallStackInfo(logEvent.GetOrCreateCategory("CallStack"));
			AppendThreadInfo(logEvent);
			AppendProcessInfo(logEvent);
			AppendComputerInfo(logEvent);
		    AppendException(logEvent, ex);

		    if (appendData != null)
		    {
			    try
			    {
				    appendData(logEvent);
			    }
			    catch (Exception ex2)
			    {
				    InternalCrash(ex2);
			    }
		    }
		    if (ShouldLogCallback != null)
		    {
			    if (!ShouldLogCallback(logEvent))
				    return;
		    }

		    if (_DefaultEmailAddress != null || _FatalEmailAddress != null)
		    {
			    // Send email
			    if (SmtpClient != null)
			    {
				    try
				    {
						MailMessage mm = new MailMessage();
						if (_DefaultEmailAddress != null)
						{
							mm.To.Add(_DefaultEmailAddress);
						}

						if (type == "Fatal" && _FatalEmailAddress != null)
						{
							mm.To.Add(_FatalEmailAddress);
						}

					    if (mm.To.Count > 0)
					    {
						    string messageTruncated = logEvent.Message;
						    if (messageTruncated != null && messageTruncated.Length > 50)
							    messageTruncated = messageTruncated.Substring(0, 50) + "...";
						    mm.Subject = SubjectLinePrefix + type + ": " + messageTruncated + " (" + logEvent.StackSignature + ")";
						    mm.From = EmailFrom;
						    mm.Body = logEvent.Htmlize();
						    mm.IsBodyHtml = true;

						    SmtpClient.SendAsync(mm, null);
					    }
				    }
				    catch (Exception ex2)
				    {
					    InternalCrash(ex2);
				    }
				    
			    }
		    }

		    if (LogExtensions != null)
		    {
			    foreach (var extension in LogExtensions)
			    {
				    try
				    {
					    extension(logEvent);
				    }
				    catch (Exception ex2)
				    {
					    InternalCrash(ex2);
				    }
			    }
		    }
	    }

		private void AppendException(LogEvent logEvent, Exception ex)
		{
			AppendException(logEvent.GetOrCreateCategory("Exception"), ex);
		}

	    private void AppendException(Dictionary<string, object> info, Exception ex)
	    {
		    info["Message"] = ex.Message;
			if(ex.Data.Count > 0)
				info["Data"] = ex.Data;
		    info["HResult"] = ex.HResult;
		    info["Source"] = ex.Source;
		    info["StackTrace"] = ex.StackTrace;

		    if (ex.InnerException != null)
		    {
			    var innerInfo = new Dictionary<string, object>();
			    info["InnerException"] = innerInfo;
				AppendException(innerInfo, ex.InnerException);
		    }
	    }



		private void AppendHttpRequestInfo(LogEvent logEvent, HttpContext ctx)
		{
			var req = ctx.Request;

			var cat = logEvent.GetOrCreateCategory("HttpRequest");
			cat["ContentLength"] = req.ContentLength;
			cat["ContentType"] = req.ContentType;
			cat["HttpMethod"] = req.HttpMethod;
			cat["IsAuthenticated"] = req.IsAuthenticated;
			cat["Path"] = req.Path;
			cat["PathInfo"] = req.PathInfo;
			cat["Referrer"] = req.UrlReferrer;
			cat["RequestType"] = req.RequestType;
			cat["RawUrl"] = req.RawUrl;
			cat["TotalBytes"] = req.TotalBytes;
			cat["UserHostAddress"] = req.UserHostAddress;
			cat["Url"] = req.Url;

			var cookies = new Dictionary<string, string>();
			foreach (var cookieName in req.Cookies.AllKeys)
			{
				var cookie = req.Cookies[cookieName];
				if (cookie == null)
					continue;
				cookies[cookieName] = cookie.Value;
			}
			cat["Cookies"] = cookies;

			var headers = new Dictionary<string, string>();
			for (int i = 0; i < req.Headers.Keys.Count; i++)
			{
				string key = req.Headers.GetKey(i);
				string[] vals = req.Headers.GetValues(i);
				if (vals == null || vals.Length == 0)
				{
					headers[key] = "";
				}
				else if (vals.Length == 1)
				{
					headers[key] = vals[0];
				}
				else
				{

					for (int t = 0; t < vals.Length; t++)
					{
						headers[key + "(" + t + ")"] = vals[t];
					}
				}
			}

			var form = req.Form;
			if (form.Keys.Count > 0)
			{
				var formInfo = new Dictionary<string, string>(form.Keys.Count);
				cat["Form"] = formInfo;

				for (int i = 0; i < form.Keys.Count; i++)
				{
					string key = form.GetKey(i);
					string keyName = "Form:" + key;
					string[] vals = form.GetValues(i);
					if (vals != null && vals.Length != 0 && !string.IsNullOrWhiteSpace(vals[0]) && IsSensitiveItem(key))
					{
						if (vals.Length == 1)
						{
							formInfo[keyName] = string.Format("[removed for security] Length: {0}", vals[0].Length);
						}
						else
						{
							for (int j = 0; j < vals.Length; j++)
							{
								formInfo[keyName + ":" + j] = string.Format("[removed for security] Length: {0}",
									vals[j] != null ? vals[j].Length : 0);
							}
						}
						continue;
					}
					if (vals == null || vals.Length == 0)
					{
						formInfo[keyName] = "";
					}
					else if (vals.Length == 1)
					{
						formInfo[keyName] = vals[0] != null
							? (vals[0].Length > MaxHttpFormValueLength ? vals[0].Substring(0, Int32.MaxValue) + "..." : vals[0])
							: "";

					}
					else
					{
						for (int t = 0; t < vals.Length; t++)
						{
							formInfo[keyName + ":" + t] = vals[t] != null
								? (vals[t].Length > MaxHttpFormValueLength ? vals[t].Substring(0, Int32.MaxValue) + "..." : vals[t])
								: "";
						}
					}
				}
			}

			var files = req.Files;
			if (files.Count > 0)
			{
				var fileInfo = new Dictionary<string, object>(files.Count);
				cat["Files"] = fileInfo;
				for (int i = 0; i < files.Count; i++)
				{
					var pf = req.Files[i];
					fileInfo["File:" + i + ":FileName"] = pf.FileName;
					fileInfo["File:" + i + ":ContentType"] = pf.ContentType;
					fileInfo["File:" + i + ":ContentLength"] = pf.ContentLength;
				}
			}

			string contentType = req.ContentType.ToLower();
			
			if (contentType.Contains("application/json")
				|| contentType.Contains("text/")
				|| contentType.Contains("html/")
				|| contentType.Contains("application/xml")
				|| contentType.Contains("+xml"))
			{
				try
				{
					using (var sr = new StreamReader(req.InputStream))
					{
						if (req.InputStream.CanSeek)
							req.InputStream.Position = 0;

						string bodyContent = sr.ReadToEnd();
						if (bodyContent.Length > MaxHttpBodyLength)
							bodyContent = bodyContent.Substring(0, MaxHttpBodyLength) + "...";

						cat["Body"] = bodyContent;
					}
				}
				catch (Exception ex)
				{
					InternalCrash(ex);
				}
			}
		}

		private void AppendHttpResponseInfo(LogEvent logEvent, HttpContext ctx)
		{
			HttpResponse resp = ctx.Response;

			var cat = logEvent.GetOrCreateCategory("HttpResponse");

			cat["Buffer"] = resp.Buffer;
			cat["BufferOutput"] = resp.BufferOutput;
			cat["CacheControl"] = resp.CacheControl;
			cat["Charset"] = resp.Charset;
			cat["ContentEncoding"] = resp.ContentEncoding.EncodingName;
			cat["ContentType"] = resp.ContentType;
			cat["Expires"] = resp.Expires;
			cat["ExpiresAbsolute"] = resp.ExpiresAbsolute;
			cat["IsClientConnected"] = resp.IsClientConnected;
			cat["RedirectLocation"] = resp.RedirectLocation;
			cat["Status"] = resp.Status;
			cat["StatusCode"] = resp.StatusCode;
			cat["StatusDescription"] = resp.StatusDescription;
			cat["SupressContent"] = resp.SuppressContent;
		}

	    private void AddHttpSessionInfo(LogEvent logEvent, HttpContext ctx)
	    {
		    var cat = logEvent.GetOrCreateCategory("HttpSession");

		    var session = ctx.Session;

		    cat["SessionID"] = session.SessionID;
		    cat["IsNewSession"] = session.IsNewSession;

		    if (IncludeSessionObjects)
		    {
			    for(int i = 0; i < session.Count; i++)
			    {
				    string key = session.Keys[i];
				    object value = session[key];

				    if (IsSensitiveItem(key))
					    value = "[removed for security] Length: " + (value != null ? (value.ToString().Length) : 0);
				    cat[key] = value;
			    }
		    }
	    }

		private void AppendHttpContextUserInfo(LogEvent logEvent, HttpContext ctx)
		{
			var user = ctx.User;

			if (!user.Identity.IsAuthenticated)
				return;

			var cat = logEvent.GetOrCreateCategory("HttpUser");
			cat["IsAuthenticated"] = user.Identity.IsAuthenticated;
			cat["Name"] = user.Identity.Name;
			cat["AuthenticationType"] = user.Identity.AuthenticationType;
		}


		private string AppendCallStackInfo(Dictionary<string, object> info, StackTrace stack = null)
		{
			if (stack == null)
			{
				stack = new StackTrace(2, true);
			}

			var frames = stack.GetFrames();
			if (frames == null)
				return null;

			bool skipLoggerClass = true;
			int count = 0;

			// The signatureHash is a hash for the first 4 stack frames that are not System.* or Microsoft.*
			int sigCount = 0;
			int signatureHash = 0;
			foreach(var frame in frames)
			{
				var method = frame.GetMethod();
				var methodType = method.DeclaringType ?? method.ReflectedType;
				if (skipLoggerClass)
				{
					if (methodType == typeof (CalbucciLib.Logger) || methodType == typeof (CalbucciLib.PerfLogger))
					{
						continue;
					}
					skipLoggerClass = false;
				}
				string methodName = methodType != null ? methodType.FullName : "?";
				int lineNumber = frame.GetFileLineNumber();
				if (sigCount < 4 && !methodName.StartsWith("System.", StringComparison.CurrentCultureIgnoreCase)
				    && !methodName.StartsWith("Microsoft.", StringComparison.CurrentCultureIgnoreCase))
				{
					signatureHash ^= (methodName + lineNumber).GetHashCode();
					sigCount++;
				}

				string frameLine;
				if (IncludeFileNamesInCallStack)
				{
					string fileName = frame.GetFileName();
					frameLine = string.Format("{0} #{1} @ {2}", methodName, lineNumber,
						fileName);
				}
				else
				{
					frameLine = string.Format("{0} #{1}", methodName, lineNumber);
				}

				info[count.ToString()] = frameLine;
				count++;
			}

			return signatureHash.ToString("x");
		}

		private void AppendThreadInfo(LogEvent logEvent)
		{
			var thread = Thread.CurrentThread;
			logEvent.Add("Thread", "ThreadId", thread.ManagedThreadId);
		}

		private void AppendProcessInfo(LogEvent logEvent)
		{
			var pi = ProcessModelInfo.GetCurrentProcessInfo();
			var assembly = Assembly.GetExecutingAssembly();

			logEvent.Add("Process", "AssemblyVersion", assembly.GetName().Version.ToString());
			logEvent.Add("Process", "PeakMemoryUsed", pi.PeakMemoryUsed);
			logEvent.Add("Process", "ProcessId", pi.ProcessID);
			logEvent.Add("Process", "StartTime", pi.StartTime);
		}

		private void AppendComputerInfo(LogEvent logEvent)
		{
			logEvent.Add("Computer", "Name", System.Environment.MachineName);

		}

	    static private bool IsSensitiveItem(string itemName)
	    {
		    if (string.IsNullOrWhiteSpace(itemName))
			    return false;

		    return _SensitiveInfo.Any(si => itemName.IndexOf(si, StringComparison.CurrentCultureIgnoreCase) >= 0);
	    }

		private void InternalCrash(Exception ex)
		{
			if(Debugger.IsAttached)
				Debugger.Break();

			if (InternalCrashCallback != null)
			{
				try
				{
					InternalCrashCallback(ex);
				}
				catch (Exception)
				{
					// Oh well
				}
			}
		}



		// ============================================================
		//
		// STATIC shortcuts
		//
		// ============================================================

		static public void LogError(Action<LogEvent> appendData, string format, params string[] args)
		{
			Default.Error(appendData, format, args);
		}

		static public void LogError(string format, params string[] args)
		{
			LogError(null, format, args);
		}

		static public void LogWarning(Action<LogEvent> appendData, string format, params string[] args)
		{
			Default.Warning(appendData, format, args);
		}

		static public void LogWarning(string format, params string[] args)
		{
			LogWarning(null, format, args);
		}

		static public void LogInfo(Action<LogEvent> appendData, string format, params string[] args)
		{
			Default.Info(appendData, format, args);
		}

		static public void LogInfo(string format, params string[] args)
		{
			LogInfo(null, format, args);
		}

		static public void LogFatal(Action<LogEvent> appendData, string format, params string[] args)
		{
			Default.Fatal(appendData, format, args);
		}

		static public void LogFatal(string format, params string[] args)
		{
			LogFatal(null, format, args);
		}

		static public void LogExeption(Action<LogEvent> appendData, Exception ex, string format, params string[] args)
		{
			Default.Exception(appendData, ex, format, args);
		}

		static public void LogException(Exception ex, string format, params string[] args)
		{
			Default.Exception(ex, format, args);
		}

		static public void LogPerfIssue(Action<LogEvent> appendData, string format, params string[] args)
		{
			Default.PerfIssue(appendData, format, args);
		}

		static public void LogPerfIssue(string format, params string[] args)
		{
			Default.PerfIssue(format, args);
		}

		static public void LogInvalidCodePath(Action<LogEvent> appendData, string format, params string[] args)
		{
			Default.InvalidCodePath(appendData, format, args);
		}

		static public void LogInvalidCodePath(string format, params string[] args)
		{
			Default.InvalidCodePath(format, args);
		}


    }
}