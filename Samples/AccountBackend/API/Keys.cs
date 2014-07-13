using System;

namespace AccountBackend
{
	public static class Keys
	{
		public static string User(string name)
		{
			return Format("user", name);
		}

		public static string Token(string token)
		{
			return Format("token", token);
		}

		private static string Format(params string[] args)
		{
			return string.Join(":", args);
		}
	}
}

