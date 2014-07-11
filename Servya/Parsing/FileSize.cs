using System;

namespace Servya
{
	public struct FileSize
	{
		public long Bytes { get; private set; }

		public static bool TryParse(string str, out FileSize size)
		{
			var numberStr = string.Empty;
			foreach (var c in str)
			{
				if (char.IsDigit(c) || c == '.')
					numberStr += c;
			}

			double number;
			if (!double.TryParse(numberStr, out number))
			{
				size = default(FileSize);
				return false;
			}

			var bytes = ParseSize(str.ToLower(), number);
			size = new FileSize { Bytes = bytes };
			return true;
		}

		private static long ParseSize(string str, double number)
		{
			Func<int, long> size = p => (long)(number * Math.Pow(1024, p));

			if (str.EndsWith("kb"))
				return size(1);
			else if (str.EndsWith("mb"))
				return size(2);
			else if (str.EndsWith("gb"))
				return size(3);

			return size(0);
		}
	}
}
