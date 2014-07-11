using System;
using System.IO;

namespace Servya
{
	public class StreamLogger : ILogger
	{
		protected TextWriter Writer { get; set; }

		public StreamLogger()
		{
		}

		public StreamLogger(TextWriter writer)
		{
			Writer = writer;
		}

		public virtual void Write(LogLevel level, string message)
		{
			if (Writer == null)
				return;

			Writer.WriteLine(message);
			Writer.Flush();
		}
	}

	public class FileLogger : StreamLogger
	{
		private readonly string m_name;
		private readonly CategoryLogger m_logger;

		private DateTime m_nextRotate;
		private Stream m_underlyingStream;
		private bool m_initialised;
		private TimeSpan m_rotateInterval;
		private long m_maxBytes;

		public FileLogger(string name)
		{
			m_logger = new CategoryLogger(this);
			m_name = name;
			Rotate();
		}

		public void Init(TimeSpan interval, FileSize maxSize)
		{
			m_rotateInterval = interval;
			m_nextRotate += interval;
			m_maxBytes = maxSize.Bytes;
			m_initialised = true;
		}

		public void Close()
		{
			if (Writer != null)
				Writer.Close();
		}

		public override void Write(LogLevel level, string message)
		{
			base.Write(level, message);

			if (m_initialised && (m_nextRotate < DateTime.Now || (m_maxBytes > 0 && m_underlyingStream.Length > m_maxBytes)))
			{
				m_logger.Info("Performing log rotation");
				Rotate();
			}
		}

		public void Rotate()
		{
			Close();

			var newName = m_name + "_" + DateTime.UtcNow.ToString("dd-MM-yyyy_HH-mm-ss") + ".log";
			m_underlyingStream = File.OpenWrite(newName);
			Writer = new StreamWriter(m_underlyingStream);

			m_nextRotate = DateTime.Now + m_rotateInterval;
		}
	}
}
