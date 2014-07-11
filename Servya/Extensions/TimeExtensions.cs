using System;

namespace Servya
{
	public static class TimeExtensions
	{
		private static readonly DateTime m_epoch = new DateTime(1970, 1, 1);

		public static long GetUnixTime(this DateTime time)
		{
			return (long)time.Subtract(m_epoch).TotalSeconds;
		}
	}
}
