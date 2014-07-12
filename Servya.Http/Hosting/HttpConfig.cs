using System;

namespace Servya
{
	public class HttpConfig
	{
		public int Port { get; set; }
		public int SecurePort { get; set; }
		public int EventLoops { get; set; }
		public int MaxDelay { get; set; }

		public static HttpConfig DevDefault
		{
			get
			{
				return new HttpConfig { Port = 1337, EventLoops = Environment.ProcessorCount };
			}
		}
	}
}

