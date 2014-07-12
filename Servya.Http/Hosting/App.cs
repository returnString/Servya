using System;

namespace Servya
{
	public static class App
	{
		public static void Run()
		{
			App.Run(new Host<Config>(), Config.DevDefault);
		}

		public static void Run<THost, TConfig>(THost host, TConfig config)
			where THost : Host<TConfig>
			where TConfig : Config
		{
			host.Run(config);

			while (true)
				Console.ReadLine();
		}
	}
}

