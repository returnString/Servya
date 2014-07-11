using System;
using System.Diagnostics;
using System.Threading;

namespace Servya
{
	public class CategoryLogger
	{
		private readonly string m_name;

		public CategoryLogger(object container)
			: this(container.GetType().GetFriendlyName())
		{
		}

		public CategoryLogger(string name)
		{
			m_name = name;
		}

		[Conditional("DEBUG")]
		public void Debug(string format, params object[] args)
		{
			Write(LogLevel.Debug, format, args);
		}

		public void Info(string format, params object[] args)
		{
			Write(LogLevel.Info, format, args);
		}

		public void Warning(string format, params object[] args)
		{
			Write(LogLevel.Warning, format, args);
		}

		public void Error(string format, params object[] args)
		{
			Write(LogLevel.Error, format, args);
		}

		public void Error(Exception ex)
		{
			Error(ex.ToString());
		}

		private static ILogger s_logger = new ConsoleLogger();

		public static void SetLogger(ILogger logger)
		{
			s_logger = logger;
		}

		private void Write(LogLevel level, string format, params object[] args)
		{
			s_logger.Write(level, Prepare(level, format, args));
		}

		private string Prepare(LogLevel level, string format, params object[] args)
		{
			var msg = args.Length == 0 ? format : string.Format(format, args);
			var thread = Thread.CurrentThread;

			string desc;
			if (thread.IsThreadPoolThread || string.IsNullOrEmpty(thread.Name))
				desc = thread.ManagedThreadId.ToString();
			else
				desc = thread.Name;

			return string.Format("[{0}] [{1}] [{2}] [{3}] {4}", DateTime.Now.ToString("o"), desc, level, m_name, msg);
		}
	}
}
