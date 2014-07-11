using Servya;
using System;
using System.Threading.Tasks;

namespace HelloWorld
{
	[Service]
	class ProgramService
	{
		// Available as: http://host/program/hello?name=whatever
		[Route]
		public string Hello(string name)
		{
			return "Hello there, " + name;
		}

		// Available as: http://host/program/belatedhello?name=whatever&delay=2
		// The response will take <delay> seconds to come through, but this won't block server resources
		[Route]
		public async Task<string> BelatedHello(string name, int delay)
		{
			await Task.Delay(TimeSpan.FromSeconds(delay));
			return "Apologies for keeping you waiting, " + name;
		}

		// This is the really long explanation that introduces various Servya components
		static void Main()
		{
			// Routers control the raw map from endpoints to actions
			var router = new Router();

			// Listeners process incoming HTTP requests
			var listener = new AsyncHttpListener(router, port: 1337, securePort: 0);

			// AutoRouters turn service classes, like this, into nicely mapped endpoints
			var autoRouter = new AutoRouter(router, new Parser(), new DependencyResolver());

			// Load all the service classes, like this ProgramService
			// Find any methods tagged as [Route]
			autoRouter.Discover();

			// Create a dev API interface available at http://host/
			autoRouter.CreateWebInterface();

			// Now that we're all set up, start our HTTP listener
			// The event loop context is similar to how Node.js works
			// When using this, don't block any threads! Use async all the way down
			// Unlike Node.js, Servya provides built-in support for multiple event loops
			// A nice default is the CPU count for the system
			listener.Start(() => new EventLoopContext(), Environment.ProcessorCount);

			while (true)
				Console.ReadLine();
		}
	}
}
