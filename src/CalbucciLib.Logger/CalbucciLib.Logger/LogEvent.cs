using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace CalbucciLib
{
	public class LogEvent
	{
		public string Message { get; set; }
		public string Type { get; set; }
		public string StackSignature { get; set; }

		public string UID { get; set; }
		public DateTime EventDate { get; set; }
		public DateTime EventDateUtc { get; set; }

		Dictionary<string, Dictionary<string, object>> Information { get; set; }

		public LogEvent(string type)
		{
			Information = new Dictionary<string, Dictionary<string, object>>();

			Type = type;
			UID = Guid.NewGuid().ToString("n");
			EventDate = DateTime.Now;
			EventDate = DateTime.UtcNow;
		}

		public void Add(string categoryName, string name, object value)
		{
			var category = GetOrCreateCategory(categoryName);
			category[name] = value;
		}

		public Dictionary<string, object> GetOrCreateCategory(string categoryName)
		{
			Dictionary<string, object> category;
			if (!Information.TryGetValue(categoryName, out category))
			{
				category = new Dictionary<string, object>();
				Information[categoryName] = category;

			}
			return category;
		}

		public string ToJson(bool indent = false)
		{
			return JsonConvert.SerializeObject(this, indent ? Formatting.Indented : Formatting.None);
		}

		public string Htmlize()
		{
			StringBuilder sb = new StringBuilder(4192);

			sb.Append("<div>");

			sb.AppendFormat(@"<p style=""font-size:160%"">{0}: {1}</p>", Type, HttpUtility.HtmlEncode(Message));
			sb.AppendFormat(@"<div>Local Time: {0} | UTC: {1}</div>", HttpUtility.HtmlEncode(EventDate.ToString()), HttpUtility.HtmlEncode(EventDateUtc.ToString()));
			sb.AppendFormat(@"<div>UID: {0} | StackSignature: {1}</div>", UID, StackSignature);

			foreach (var de in Information)
			{
				sb.AppendFormat(@"<br><h3>{0}</h3>", HttpUtility.HtmlEncode(de.Key));
				Htmlize(sb, de.Value);
			}

			sb.Append("</div>");

			return sb.ToString();
		}

		private void Htmlize(StringBuilder sb, Dictionary<string, object> info)
		{
			if (info == null)
				return;

			foreach (var de in info)
			{
				if (de.Value == null)
				{
					sb.AppendFormat(@"<div><i>{0}</i>: (null)</div>", HttpUtility.HtmlEncode(de.Key));
				}
				else if (de.Value is Dictionary<string, object>)
				{
					sb.AppendFormat(@"<div><i>{0}</i></div>", HttpUtility.HtmlEncode(de.Key));
					sb.Append(@"<div style=""padding-left:3em;"">");
					Htmlize(sb, de.Value as Dictionary<string, object>);
					sb.Append("</div>");
				}
				else if (de.Value is IDictionary)
				{
					sb.AppendFormat(@"<div><i>{0}</i></div>", HttpUtility.HtmlEncode(de.Key));
					sb.Append(@"<div style=""padding-left:3em;"">");
					var dic = ((IDictionary) de.Value);
					foreach (var k in dic.Keys)
					{
						var value = dic[k];
						sb.AppendFormat(@"<div><i>{0}</i>: {1}</div>", HttpUtility.HtmlEncode(k),
							value != null ? HttpUtility.HtmlEncode(value) : "(null)");
					}
					sb.Append("</div>");
					
				}
				else if (de.Value is ICollection)
				{
					sb.AppendFormat(@"<div><i>{0}</i></div>", HttpUtility.HtmlEncode(de.Key));
					sb.Append(@"<div style=""padding-left:3em;"">");
					var collection = ((ICollection) de.Value).Cast<object>().Select((value, index) => new {value, index});
					foreach(var item in collection)
					{
						sb.AppendFormat(@"<div><i>{0}</i>: {1}</div>", item.index,
							item.value != null ? HttpUtility.HtmlEncode(item.value) : "(null)");
					}
					sb.Append("</div>");
				}
				else
				{
					sb.AppendFormat(@"<div><i>{0}</i>: {1}</div>", HttpUtility.HtmlEncode(de.Key), de.Value != null ? HttpUtility.HtmlEncode(de.Value) : "(null)");
				}
			}
		}

		static public LogEvent FromJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				return null;

			var le = JsonConvert.DeserializeObject<LogEvent>(json);
			return le;
		}

		public override string ToString()
		{
			return ToJson(true);
		}
	}
}
