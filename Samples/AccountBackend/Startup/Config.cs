using Servya;

namespace AccountBackend
{
	public class AccountBackendConfig : Config
	{
		public RedisConfig Redis { get; set; }
	}

	public class RedisConfig
	{
		public string Host { get; set; }
	}
}

