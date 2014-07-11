using System;

namespace Servya
{
	public static class App
	{
		public static void DevRun()
		{
			App.Run(new Host(), Config.DevDefault);
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

	public class Host<TConfig>
		where TConfig : Config
	{
		protected DependencyResolver Resolver { get; private set; }
		protected Router Router { get; private set; }

		public void Run(TConfig config)
		{
			Resolver = new DependencyResolver();
			Router = new Router();

			InitHttp(config.Http);
		}

		private void InitHttp(HttpConfig config)
		{
			var listener = new AsyncHttpListener(Router, port: config.Port, securePort: config.SslPort);
			var autoRouter = new AutoRouter(Router, new Parser(), Resolver);
			autoRouter.Discover();
			autoRouter.CreateWebInterface();
			listener.Start(() => new EventLoopContext(), Environment.ProcessorCount);
		}
	}

	public class Host : Host<Config>
	{
	}
}

