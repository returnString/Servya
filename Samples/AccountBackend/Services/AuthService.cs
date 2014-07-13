using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Servya;
using StackExchange.Redis;

namespace AccountBackend
{
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

		[UnprotectedRoute(Verb = HttpVerb.Post)]
		public async Task<Response> Register(string name, string password)
		{
			var key = Keys.User(name);
			var transaction = m_db.CreateTransaction();
			var nameNotTaken = transaction.AddCondition(Condition.KeyNotExists(key));
			transaction.HashSetAsync(key, "password", PasswordHash(password)).Forget();
			transaction.HashSetAsync(key, "joindate", DateTime.UtcNow.GetUnixTime()).Forget();

			if (await transaction.ExecuteAsync())
				return Status.Ok;

			if (!nameNotTaken.WasSatisfied)
				return Status.NameTaken;

			return Status.InternalError;
		}

		[UnprotectedRoute(Verb = HttpVerb.Post)]
		public async Task<Response<string>> Login(string name, string password)
		{
			var key = Keys.User(name);
			var storedPassword = await m_db.HashGetAsync(key, "password");

			if (storedPassword == PasswordHash(password))
			{
				var token = Guid.NewGuid().ToString();
				await m_db.StringSetAsync(Keys.Token(token), name, m_tokenTTL);
				return token;
			}

			return Status.InvalidCredentials;
		}

		[UnprotectedRoute]
		public async Task<Response> Verify(string token)
		{
			if (await m_db.KeyExpireAsync(Keys.Token(token), m_tokenTTL))
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
	}
}

