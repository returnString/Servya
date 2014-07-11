using System;

namespace Servya
{
	public class ConsoleLogger : StreamLogger
	{
		public ConsoleLogger()
			: base(Console.Out)
		{
		}

		public override void Write(LogLevel level, string message)
		{
			var oldColour = Console.ForegroundColor;
			Console.ForegroundColor = GetColour(level);
			base.Write(level, message);
			Console.ForegroundColor = oldColour;
		}

		private static ConsoleColor GetColour(LogLevel level)
		{
			switch (level)
			{
				case LogLevel.Error:
					return ConsoleColor.Red;

				case LogLevel.Warning:
					return ConsoleColor.Yellow;

				case LogLevel.Debug:
					return ConsoleColor.DarkGreen;

				default:
					return ConsoleColor.Gray;
			}
		}
	}
}
