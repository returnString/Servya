using System;
using System.Text;

namespace Servya
{
	public static class StringExtensions
	{
		public static StringBuilder AppendLine(this StringBuilder builder, string format, params object[] args)
		{
			return builder.AppendFormat(format, args).AppendLine();
		}

		public static string[] SplitRemoveEmpty(this string str, params char[] chars)
		{
			return str.Split(chars, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
