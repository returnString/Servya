using System;

namespace Servya
{
	public class Host<TConfig>
		where TConfig : Config
	{
		protected DependencyResolver Resolver { get; private set; }
		protected Router Router { get; private set; }
		protected TConfig Config { get; private set; }

		public void Run(TConfig config)
		{
			Config = config;
			Resolver = new DependencyResolver();
			Router = new Router();

			PreServiceInit();
			InitHttp(config.Http);
		}

		protected virtual void PreServiceInit()
		{
		}

		protected virtual WebInterfaceConfig CreateInterfaceConfig()
		{
			return WebInterfaceConfig.Default;
		}

		private void InitHttp(HttpConfig config)
		{
			var listener = new AsyncHttpListener(Router, port: config.Port, securePort: config.SecurePort);
			var autoRouter = new AutoRouter(Router, new Parser(), Resolver);
			autoRouter.Discover();
			autoRouter.CreateWebInterface(CreateInterfaceConfig());
			listener.Start(() => new EventLoopContext(), Environment.ProcessorCount, config.MaxDelay);
		}
	}
}

