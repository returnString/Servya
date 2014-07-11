using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Servya;
using StackExchange.Redis;

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

		[JsonRoute]
		public async Task<Response> Register(string name, string password)
		{
			var key = UserKey(name);

			var transaction = m_db.CreateTransaction();
			var nameNotTaken = transaction.AddCondition(Condition.KeyNotExists(key));
			transaction.HashSetAsync(key, "password", PasswordHash(password)).Forget();

			if (await transaction.ExecuteAsync())
				return Status.Ok;

			if (!nameNotTaken.WasSatisfied)
				return Status.NameTaken;

			return Status.UnknownError;
		}

		[JsonRoute]
		public async Task<Response<string>> Login(string name, string password)
		{
			var key = UserKey(name);

			var storedPassword = await m_db.HashGetAsync(key, "password");

			if (storedPassword == PasswordHash(password))
			{
				var token = Guid.NewGuid().ToString();
				await m_db.StringSetAsync(TokenKey(token), name, m_tokenTTL);
				return token;
			}

			return Status.InvalidCredentials;
		}

		[JsonRoute]
		public async Task<Response> Verify(string token)
		{
			if (await m_db.KeyExistsAsync(TokenKey(token)))
				return Status.Ok;

			return Status.TokenExpired;
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

