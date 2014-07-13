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
		public async Task<ProfileInfo> My(string user)
		{
			var hashes = await m_db.HashGetAllAsync(Keys.User(user));
			var data = hashes.ToStringDictionary();

			return new ProfileInfo { Name = user, JoinDate = long.Parse(data["joindate"]) };
		}
	}
}

