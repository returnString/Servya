using System;

namespace Servya
{
	public class HttpConfig
	{
		public int Port { get; set; }
		public int SslPort { get; set; }
		public int EventLoops { get; set; }

		public static HttpConfig DevDefault
		{
			get
			{
				return new HttpConfig { Port = 1337, SslPort = 0, EventLoops = Environment.ProcessorCount };
			}
		}
	}
}

