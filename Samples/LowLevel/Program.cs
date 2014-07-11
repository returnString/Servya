using System;
using Servya;
using System.Threading;

// These are bad examples for production usage
// It's usually best to disable HTTP chunking and know the response length upfront
// Raw routes get no web interface entries either
// Unless you have specific streaming needs, avoid this altogether and use services
// Services are built on top of the routing system

namespace LowLevel
{
	class Program
	{
		static void Main()
		{
			// Routers control the raw map from endpoints to actions
			var router = new Router();

			var random = new ThreadLocal<Random>(() => new Random());

			// Simple example of writing a value back to the response
			router.AddRoute(HttpVerb.Get, "/random", async (context, args) =>
			{
				using (var writer = context.GetWriter())
				{
					await writer.WriteLineAsync("Random value: " + random.Value.Next());
				}
			});

			// You can access request properties
			router.AddRoute(HttpVerb.Get, "/info", async (context, args) =>
			{
				var request = context.Request;

				using (var writer = context.GetWriter())
				{
					await writer.WriteLineAsync("Caller: " + request.Client);
					await writer.WriteLineAsync("Url: " + request.Url);
					await writer.WriteLineAsync("Keep alive: " + request.KeepAlive);
					await writer.WriteLineAsync("Encoding: " + request.Encoding.EncodingName);
				}
			});

			// Routing args allow for catch-all URL components
			// Should be called like http://host/anything/somethingelse
			router.AddRoute(HttpVerb.Get, "/{first}/{second}", async (context, args) =>
			{
				using (var writer = context.GetWriter())
				{
					await writer.WriteLineAsync("Path segments, first = {0}, second = {1}", args["first"], args["second"]);
				}
			});

			// Listeners process incoming HTTP requests
			var listener = new AsyncHttpListener(router, port: 1337, securePort: 0);

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
