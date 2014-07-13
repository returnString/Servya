using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace AccountBackend
{
	public static class RedisExtensions
	{
		public static Task HashSetAsync(this IDatabaseAsync db, RedisKey key, params RedisValue[] args)
		{
			if (args.Length % 2 != 0)
				throw new ArgumentException("Expected arguments in pairs", "args");

			var hashes = new HashEntry[args.Length / 2];

			for (var i = 0; i < args.Length; i += 2)
			{
				hashes[i / 2] = new HashEntry(args[i], args[i + 1]);
			}

			return db.HashSetAsync(key, hashes);
		}
	}
}

