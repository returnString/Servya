using System.Threading.Tasks;
using Newtonsoft.Json;
using Servya;
using StackExchange.Redis;

namespace AccountBackend
{
	class Program
	{
		static void Main()
		{
			MainImpl().Wait();
		}

		static async Task MainImpl()
		{
			var configFile = await FileIO.OpenRead("config.json").ReadAllAsync();
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
			var multiplexer = ConnectionMultiplexer.Connect(Config.RedisServer);
			Resolver.Add(multiplexer.GetDatabase());
		}
	}

	public class AccountBackendConfig : Config
	{
		public string RedisServer { get; set; }
	}
}
