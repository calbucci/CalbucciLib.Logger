using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalbucciLib
{
	public class PerfLogger : IDisposable
	{
		private Stopwatch _Stopwatch;
		public static Logger DefaultLogger { get; set; }

		public TimeSpan MaxThreshold { get; set; }

		public string Message { get; set; }

		static PerfLogger()
		{
			DefaultLogger = Logger.Default;
		}

		public PerfLogger(TimeSpan alertThreshold, string message = null, bool autoStart = true)
		{
			MaxThreshold = alertThreshold;
			Message = message;
			if (autoStart)
			{
				_Stopwatch = new Stopwatch();
				_Stopwatch.Start();
			}
		}

		public PerfLogger(int alertThresholdSeconds, string message = null, bool autoStart = true)
		{
			Message = message;
			MaxThreshold = TimeSpan.FromSeconds(alertThresholdSeconds);
			if (autoStart)
			{
				_Stopwatch = new Stopwatch();
				_Stopwatch.Start();
			}
		}

		public void Start()
		{
			if(_Stopwatch != null)
				throw new Exception("PerfLogger can only be started once.");
			_Stopwatch = new Stopwatch();
			_Stopwatch.Start();
		}

		public void Stop()
		{
			if(_Stopwatch != null)
			{
				_Stopwatch.Stop();
				if (_Stopwatch.Elapsed > MaxThreshold)
				{
				    string message = Message ?? "PerfLog > " + MaxThreshold.TotalSeconds.ToString("F") + " secs";
					DefaultLogger.PerfIssue(logEvent =>
					{
						logEvent.Add("Perf", "MaxThreshold", MaxThreshold.TotalSeconds.ToString("F"));
						logEvent.Add("Perf", "Elapsed", _Stopwatch.Elapsed.TotalSeconds.ToString("F"));
					}, message);
				}
				_Stopwatch = null;
			}
		}

		public void Dispose()
		{
			Stop();
			
		}
	}
}
