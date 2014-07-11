using Servya;
using StackExchange.Redis;

namespace AccountBackend
{
	class Program
	{
		static void Main()
		{
			var host = new AccountBackendHost();
			var config = new AccountBackendConfig
			{
				Http = HttpConfig.DevDefault,
				RedisServer = "127.0.0.1"
			};

			App.Run(host, config);
		}
	}

	public class AccountBackendHost : Host<AccountBackendConfig> 
	{
		// Add our Redis connection to the dependency resolver
		// This way, services can get it from their constructor
		protected override void PreServiceInit()
		{
			var multiplexer = ConnectionMultiplexer.Connect(Config.RedisServer);
			Resolver.Add(multiplexer.GetDatabase());
		}
	}

	public class AccountBackendConfig : Config
	{
		public string RedisServer { get; set; }
	}
}
