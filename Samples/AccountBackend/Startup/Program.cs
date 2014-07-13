using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Servya;
using StackExchange.Redis;

namespace AccountBackend
{
	class Program
	{
		static void Main()
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

			var configFile = File.ReadAllText("config.json");
			var config = JsonConvert.DeserializeObject<AccountBackendConfig>(configFile);

			var host = new AccountBackendHost();
			App.Run(host, config);
		}
	}

	public class AccountBackendHost : Host<AccountBackendConfig> 
	{
		// Add our Redis connection to the dependency resolver
		// This way, services can get it from their constructor
		protected override void PreServiceInit()
		{
			var multiplexer = ConnectionMultiplexer.Connect(Config.Redis.Host);
			Resolver.Add(multiplexer.GetDatabase());
		}

		// Make sure our token-protected methods show up properly in the web interface
		protected override WebInterfaceConfig CreateInterfaceConfig()
		{
			var tokenParam = new WebInterfaceParam(typeof(string), "token");

			return new WebInterfaceConfig
			{
				Swap = new Dictionary<string, WebInterfaceParam>
				{
					{ "user", tokenParam }
				},
				ExtraParamCreator = m =>
				{
					if (m.HasAttribute<TokenRouteAttribute>())
						return new[] { tokenParam };

					return null;
				}
			};
		}
	}
}
