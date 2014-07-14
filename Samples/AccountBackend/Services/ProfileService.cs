using System.Threading.Tasks;
using Servya;
using StackExchange.Redis;
using System;

namespace AccountBackend
{
	[Service]
	public class ProfileService
	{
		private readonly IDatabase m_db;

		public ProfileService(IDatabase db)
		{
			m_db = db;
		}

		[TokenRoute]
		public Task<Response<ProfileInfo>> My(string user)
		{
			return Find(user);
		}

		[TokenRoute]
		public async Task<Response<ProfileInfo>> Find(string name)
		{
			var hashes = await m_db.HashGetAllAsync(Keys.User(name));
			if (hashes.Length == 0)
				return Status.NotFound;

			var data = hashes.ToStringDictionary();

			return new ProfileInfo { Name = name, JoinDate = long.Parse(data["joindate"]), Country = data["country"] };
		}
	}
}

