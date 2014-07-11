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

		static void Main()
		{
			App.Run();
		}
	}
}
