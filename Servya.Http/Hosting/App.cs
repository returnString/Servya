using System;

namespace Servya
{
	public static class App
	{
		private static CategoryLogger m_logger;

		static App()
		{
			m_logger = new CategoryLogger("App");
		}

		public static void Run()
		{
			App.Run(new Host<HostConfig>(), HostConfig.DevDefault);
		}

		public static void Run<THost, TConfig>(THost host, TConfig config)
			where THost : Host<TConfig>
			where TConfig : HostConfig
		{
			try
			{
				host.Run(config);
			}
			catch (Exception ex)
			{
				m_logger.Error(ex);
			}
			
			while (true)
				Console.ReadLine();
		}
	}
}

