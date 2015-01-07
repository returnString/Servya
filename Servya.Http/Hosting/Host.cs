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
			InitHttp();
		}

		protected virtual void PreServiceInit()
		{
		}

		protected virtual WebInterfaceConfig CreateInterfaceConfig()
		{
			return WebInterfaceConfig.Default;
		}

		private void InitHttp()
		{
			var httpConfig = Config.Http;

			var listener = new AsyncHttpListener(Router, port: httpConfig.Port, securePort: httpConfig.SecurePort);
			var autoRouter = new AutoRouter(Router, new Parser(), Resolver, Config.Debug);
			autoRouter.Discover();
			autoRouter.CreateWebInterface(CreateInterfaceConfig());
			listener.Start(() => new EventLoopContext(), httpConfig.EventLoops, httpConfig.MaxDelay);
		}
	}
}

