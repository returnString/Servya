using System;

namespace Servya
{
	public class QueueLogger : ILogger
	{
		private readonly ILogger[] m_loggers;
		private readonly QueueProcessor m_processor;

		public QueueLogger(params ILogger[] loggers)
		{
			m_loggers = loggers;
			m_processor = new QueueProcessor("Log");
		}

		public void Write(LogLevel level, string message)
		{
			m_processor.Add(() =>
			{
				foreach (var logger in m_loggers)
					logger.Write(level, message);
			});
		}
	}
}

