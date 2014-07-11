using Servya;
using System.Security.Cryptography;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Text;
using System;

namespace AccountBackend
{
	public enum RegistrationStatus
	{
		Ok,
		UnknownError,
		NameTaken,
	}

	[Service]
	public class AuthService
	{
		private readonly IDatabase m_db;
		private readonly TimeSpan m_tokenTTL;

		public AuthService(IDatabase db)
		{
			m_db = db;
			m_tokenTTL = TimeSpan.FromMinutes(10);
		}

		[Route]
		public async Task<RegistrationStatus> Register(string name, string password)
		{
			var key = UserKey(name);

			var transaction = m_db.CreateTransaction();
			var nameNotTaken = transaction.AddCondition(Condition.KeyNotExists(key));
			transaction.HashSetAsync(key, "password", PasswordHash(password)).Forget();

			if (await transaction.ExecuteAsync())
				return RegistrationStatus.Ok;

			if (!nameNotTaken.WasSatisfied)
				return RegistrationStatus.NameTaken;

			return RegistrationStatus.UnknownError;
		}

		[Route]
		public async Task<string> Login(string name, string password)
		{
			var key = UserKey(name);

			var storedPassword = await m_db.HashGetAsync(key, "password");

			if (storedPassword == PasswordHash(password))
			{
				var token = Guid.NewGuid().ToString();
				await m_db.StringSetAsync(TokenKey(token), name, m_tokenTTL);
				return token;
			}

			return "fail";
		}

		[Route]
		public async Task<bool> Verify(string token)
		{
			return await m_db.KeyExistsAsync(TokenKey(token));
		}

		private byte[] PasswordHash(string password)
		{
			using (var alg = new SHA512Managed())
			{
				var bytes = Encoding.UTF8.GetBytes(password);
				return alg.ComputeHash(bytes);
			}
		}

		private string UserKey(string name)
		{
			return "user:" + name;
		}

		private string TokenKey(string token)
		{
			return "token:" + token;
		}
	}
}

