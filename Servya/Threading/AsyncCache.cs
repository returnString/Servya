using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Servya
{
	public class AsyncCache<TKey, TValue>
	{
		private class CacheEntry
		{
			public DateTime Expiration { get; private set; }
			public TValue Value { get; private set; }

			public CacheEntry(TValue value, DateTime expiration)
			{
				Value = value;
				Expiration = expiration;
			}
		}

		private readonly ConcurrentDictionary<TKey, CacheEntry> m_dict;
		private readonly Predicate<TValue> m_valuePredicate;
		private readonly string m_name;
		private readonly CategoryLogger m_logger;

		public AsyncCache(string name, Predicate<TValue> valuePredicate = null)
		{
			m_logger = new CategoryLogger(this);
			m_logger.Info("Creating cache '{0}' for {1} => {2}", name, typeof(TKey).GetFriendlyName(), typeof(TValue).GetFriendlyName());

			m_name = name;
			m_valuePredicate = valuePredicate;
			m_dict = new ConcurrentDictionary<TKey, CacheEntry>();
		}

		public async Task<TValue> GetOrAdd(TKey key, Func<Task<TValue>> adder, TimeSpan? ttl = null)
		{
			CacheEntry entry;
			if (m_dict.TryGetValue(key, out entry) && entry.Expiration > DateTime.UtcNow)
				return entry.Value;

			var expiration = DateTime.MaxValue;

			if (ttl.HasValue)
				expiration = DateTime.UtcNow + ttl.Value;

			var newEntry = new CacheEntry(await adder(), expiration);

			if (m_valuePredicate == null || m_valuePredicate(newEntry.Value))
			{
				m_dict[key] = newEntry;
			}
			else
			{
				m_logger.Warning("Cache predicate failed for '{0}': {1} = {2}", m_name, key, newEntry.Value);
			}

			return newEntry.Value;
		}
	}
}
