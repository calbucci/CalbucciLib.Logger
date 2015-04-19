using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

		public string UniqueId { get; set; }
		public DateTime EventDate { get; set; }
		public DateTime EventDateUtc { get; set; }
        
		public Dictionary<string, Dictionary<string, object>> Information { get; set; }

        // ============================================================
        //
        // CONSTRUCTORS
        //
        // ============================================================

		public LogEvent(string type)
		{
			Information = new Dictionary<string, Dictionary<string, object>>();

			Type = type;
			UniqueId = Guid.NewGuid().ToString("n");
			EventDate = DateTime.Now;
			EventDateUtc = DateTime.UtcNow;
		}

        // ============================================================
        //
        // SET / GET Information
        //
        // ============================================================

		public void Add(string collectionName, string name, object value)
		{
		    if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(name))
		        return;

			var collection = GetOrCreateCollection(collectionName);
			collection[name] = value;
		}


	    public object Get(string collectionName, string name)
	    {
	        if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(name))
	            return null;

	        var collection = GetCollection(collectionName);
	        if (collection == null)
	            return null;

	        object ret = null;
	        collection.TryGetValue(name, out ret);
	        return ret;
	    }

	    public void AddUserData(string name, object value)
	    {
	        Add("User", name, value);
	    }

	    public object GetUserData(string name)
	    {
	        return Get("User", name);
	    }

        // ============================================================
        //
        // COLLECTIONS
        //
        // ============================================================

		public Dictionary<string, object> GetOrCreateCollection(string collectionName)
		{
		    if (string.IsNullOrWhiteSpace(collectionName))
		        return null;

			Dictionary<string, object> collection;
			if (!Information.TryGetValue(collectionName, out collection))
			{
				collection = new Dictionary<string, object>();
				Information[collectionName] = collection;

			}
			return collection;
		}

        public Dictionary<string, object> GetCollection(string collectionName)
        {
            Dictionary<string, object> collection = null;
            if (!string.IsNullOrWhiteSpace(collectionName))
            {
                Information.TryGetValue(collectionName, out collection);
            }
            return collection;
        }

        // ============================================================
        //
        // CONVERSION
        //
        // ============================================================

		public string ToJson(bool indent = false)
		{
			return JsonConvert.SerializeObject(this, indent ? Formatting.Indented : Formatting.None);
		}

		public string Htmlify()
		{
			StringBuilder sb = new StringBuilder(4192);

			sb.Append(@"<div style=""font-family: arial;max-width: 72em;"">");

			sb.AppendFormat(@"<p style=""font-size:160%;margin-bottom:0;"">{0}: {1}</p>", Type, HttpUtility.HtmlEncode(Message));
		    sb.Append(@"<div style=""color:#888;font-size:80%;"">");
			sb.AppendFormat(@"<div>Local Time: {0} | UTC: {1}</div>", HttpUtility.HtmlEncode(EventDate.ToString()), HttpUtility.HtmlEncode(EventDateUtc.ToString()));
            sb.AppendFormat(@"<div>UID: {0} | StackSignature: {1}</div>", UniqueId, StackSignature);
		    sb.Append("</div>");

			foreach (var de in Information)
			{
				sb.AppendFormat(@"<h3 style=""margin-bottom: 0px;background:#ddd;padding:5px;"">{0}</h3>", HttpUtility.HtmlEncode(de.Key));
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
                sb.AppendFormat(@"<div><i style=""display:inline-block;min-width:6em"">{0}</i> ", HttpUtility.HtmlEncode(de.Key));
				if (de.Value == null)
				{
					sb.Append(@": (null)");
				}
				else if (de.Value is IDictionary)
				{
                    var dic = ((IDictionary)de.Value);
                    if (dic.Count == 0)
                        continue;
                    sb.Append(@"(dictionary):</div>");
					sb.Append(@"<div style=""padding-left:2em;"">");
					foreach (var k in dic.Keys)
					{
						var value = dic[k];
						sb.AppendFormat(@"<div><i>{0}</i>: {1}</div>", HttpUtility.HtmlEncode(k),
							value != null ? HttpUtility.HtmlEncode(value) : "(null)");
					}
					
				}
				else if (de.Value is IList)
				{
                    sb.Append(@"(list):</div>");
					sb.Append(@"<div style=""padding-left:2em;"">");
				    var list = de.Value as IList;
                    if(list.Count == 0)
                        continue;
				        
					for(int i = 0; i < list.Count; i++)
					{
					    var val = list[i];
						sb.AppendFormat(@"<div><i>{0}</i>: {1}</div>", i, 
							val != null ? HttpUtility.HtmlEncode(val) : "(null)");
					}
				}
                else if (de.Value is Int32 || de.Value is Int64)
                {
                    sb.AppendFormat(": {0:N0} ({0:X})", de.Value);
                }
				else
				{
				    sb.Append(": ");
                    sb.Append(HttpUtility.HtmlEncode(de.Value));
				}
			    sb.Append("</div>");
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
